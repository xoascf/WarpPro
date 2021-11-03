using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using Beat_Editor;
using Beat_Editor.Controller;
using NAudio.Wave;

namespace Sample_NAudio
{
	internal class NAudioControl : INotifyPropertyChanged, IDisposable
	{
		private static NAudioControl instance;

		private bool disposed;

		private bool canPlay;

		private bool canPause;

		private bool canStop;

		private bool isPlaying;

		public WaveOut waveOutDevice;

		private WaveStream activeStream;

		public WaveChannel32 inputStream;

		public WaveDspStream dspStream;

		public MetronomeStream metronome;

		private const int waveformCompressedPointCount = 2000;

		private const int repeatThreshold = 200;

		public bool isBassViewFilter;

		private double prevRealtimeTime;

		private int antiStutterCount;

		private volatile int playerLastAliveTick;

		private string filePath;

		private Thread playerThread;

		private volatile bool playerThreadStarted;

		private static string _outDeviceName;

		private int BytesPerSecond
		{
			get
			{
				return inputStream.WaveFormat.SampleRate * inputStream.WaveFormat.BitsPerSample * inputStream.WaveFormat.Channels / 8;
			}
		}

		private int BytesPerSample
		{
			get
			{
				return inputStream.WaveFormat.BitsPerSample * inputStream.WaveFormat.Channels / 8;
			}
		}

		public double Position
		{
			get
			{
				return (double)inputStream.Position / (double)BytesPerSecond;
			}
			set
			{
				metronome.Position = (int)(value * (double)inputStream.WaveFormat.SampleRate) * BytesPerSample;
			}
		}

		public static NAudioControl Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new NAudioControl();
				}
				return instance;
			}
		}

		public static string[] DeviceNames
		{
			get
			{
				List<string> list = new List<string>();
				int deviceCount = WaveOut.DeviceCount;
				for (int i = 0; i < deviceCount; i++)
				{
					list.Add(WaveOut.GetCapabilities(i).ProductName);
				}
				return list.ToArray();
			}
		}

		private int CurrentOutDeviceNum
		{
			get
			{
				int deviceCount = WaveOut.DeviceCount;
				string outDeviceName = OutDeviceName;
				int num = 0;
				while (true)
				{
					if (num < deviceCount)
					{
						if (WaveOut.GetCapabilities(num).ProductName == outDeviceName)
						{
							break;
						}
						num++;
						continue;
					}
					return 0;
				}
				return num;
			}
		}

		public WaveStream ActiveStream
		{
			get
			{
				return activeStream;
			}
			protected set
			{
				WaveStream waveStream = activeStream;
				activeStream = value;
				if (waveStream != activeStream)
				{
					NotifyPropertyChanged("ActiveStream");
				}
			}
		}

		public bool CanPlay
		{
			get
			{
				return canPlay;
			}
			protected set
			{
				bool num = canPlay;
				canPlay = value;
				if (num != canPlay)
				{
					NotifyPropertyChanged("CanPlay");
				}
			}
		}

		public bool CanPause
		{
			get
			{
				return canPause;
			}
			protected set
			{
				bool num = canPause;
				canPause = value;
				if (num != canPause)
				{
					NotifyPropertyChanged("CanPause");
				}
			}
		}

		public bool CanStop
		{
			get
			{
				return canStop;
			}
			protected set
			{
				bool num = canStop;
				canStop = value;
				if (num != canStop)
				{
					NotifyPropertyChanged("CanStop");
				}
			}
		}

		public bool IsPlaying
		{
			get
			{
				return isPlaying;
			}
			protected set
			{
				bool num = isPlaying;
				isPlaying = value;
				if (num != isPlaying)
				{
					NotifyPropertyChanged("IsPlaying");
				}
			}
		}

		internal static string OutDeviceName
		{
			get
			{
				if (_outDeviceName == null || _outDeviceName.Length == 0)
				{
					string text = (string)LicenseController.Instance.GetSettingValue("playbackdevice");
					if (text != null)
					{
						_outDeviceName = text;
					}
				}
				return _outDeviceName;
			}
			set
			{
				_outDeviceName = value;
				LicenseController.Instance.SetSettingValue("playbackdevice", _outDeviceName);
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public event Action PlaybackStoppedAction;

		private NAudioControl()
		{
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposed)
			{
				return;
			}
			if (disposing)
			{
				Close();
				if (dspStream != null)
				{
					dspStream.Dispose();
				}
				if (metronome != null)
				{
					metronome.Dispose();
				}
			}
			disposed = true;
		}

		private void NotifyPropertyChanged(string info)
		{
			if (this.PropertyChanged != null)
			{
				this.PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}

		public void Close()
		{
			if (canStop)
			{
				Stop();
			}
			if (waveOutDevice != null)
			{
				waveOutDevice.Stop();
				waveOutDevice = null;
			}
			if (inputStream != null)
			{
				inputStream.Close();
				inputStream = null;
			}
			if (activeStream != null)
			{
				ActiveStream.Close();
				ActiveStream = null;
			}
			if (waveOutDevice != null)
			{
				waveOutDevice.Dispose();
				waveOutDevice = null;
			}
			if (playerThread != null)
			{
				playerThread.Abort();
				playerThread = null;
			}
		}

		public void Stop()
		{
			if (!canStop)
			{
				return;
			}
			if (!CheckIfPlayerThreadAlive())
			{
				AbortPlayerThread();
				return;
			}
			if (waveOutDevice != null)
			{
				waveOutDevice.Stop();
			}
			IsPlaying = false;
			CanStop = false;
			CanPlay = true;
			CanPause = false;
		}

		public void Pause()
		{
			if (IsPlaying && CanPause && CheckIfPlayerThreadAlive())
			{
				waveOutDevice.Pause();
				IsPlaying = false;
				CanPlay = true;
				CanPause = false;
			}
		}

		public void Play()
		{
			if (playerThread == null)
			{
				OpenFile(filePath);
			}
			if (CanPlay)
			{
				waveOutDevice.DeviceNumber = CurrentOutDeviceNum;
				waveOutDevice.Init(metronome);
				waveOutDevice.Play();
				IsPlaying = true;
				CanPause = true;
				CanPlay = false;
				CanStop = true;
			}
		}

		public double getCurrentRealtimeStreamPos()
		{
			double num = instance.dspStream.CurrentTime.TotalSeconds;
			if (isPlaying)
			{
				num += 0.001 * (double)dspStream.TimeSinceLatestDataReadMsec / instance.dspStream.TempoChange - 0.2;
				if (num < prevRealtimeTime && antiStutterCount < 5)
				{
					num = prevRealtimeTime;
					antiStutterCount++;
				}
				else
				{
					prevRealtimeTime = num;
					antiStutterCount = 0;
				}
			}
			return num;
		}

		private void PlayerThreadMain()
		{
			try
			{
				waveOutDevice = new WaveOut
				{
					DesiredLatency = 250,
					DeviceNumber = CurrentOutDeviceNum
				};
				ActiveStream = FileProcessor.OpenAudioReaderStream(filePath);
				inputStream = new WaveChannel32(ActiveStream);
				inputStream.PadWithZeroes = false;
				dspStream = new WaveDspStream(inputStream, LicenseController.Instance.IsTrialExpired);
				metronome = new MetronomeStream(dspStream);
				waveOutDevice.Init(metronome);
				CanPlay = true;
				disposed = false;
				waveOutDevice.PlaybackStopped += playbackStopped_event;
			}
			catch (Exception value)
			{
				Console.WriteLine(value);
				ActiveStream = null;
				dspStream = null;
				CanPlay = false;
			}
			playerThreadStarted = true;
			int num = 0;
			long num2 = 0L;
			try
			{
				while (true)
				{
					if (isPlaying)
					{
						long position = metronome.Position;
						if (position == num2)
						{
							num++;
							if (num >= 10)
							{
								Console.WriteLine("Detexted ending of music");
								waveOutDevice.Stop();
							}
						}
						else
						{
							num = 0;
							num2 = position;
						}
					}
					playerLastAliveTick = (int)DateTime.Now.Ticks;
					Thread.Sleep(100);
				}
			}
			catch (ThreadAbortException)
			{
			}
			catch (Exception ex2)
			{
				Console.WriteLine("play thread exception: " + ex2);
				if (waveOutDevice != null)
				{
					waveOutDevice.Stop();
				}
			}
		}

		private bool CheckIfPlayerThreadAlive()
		{
			int num = (int)DateTime.Now.Ticks - playerLastAliveTick;
			int num2;
			if (isPlaying)
			{
				num2 = ((num < 5000000L) ? 1 : 0);
				if (num2 == 0)
				{
					Console.WriteLine("Playback thread died");
					isPlaying = false;
					canPause = false;
					canStop = false;
					playerThread.Abort();
					waveOutDevice = null;
					inputStream = null;
					activeStream = null;
					Close();
					if (this.PlaybackStoppedAction != null)
					{
						this.PlaybackStoppedAction();
					}
				}
			}
			else
			{
				num2 = 1;
			}
			return (byte)num2 != 0;
		}

		private void StartPlayerThread(string path)
		{
			filePath = path;
			playerThreadStarted = false;
			if (playerThread != null)
			{
				playerThread.Abort();
			}
			playerThread = new Thread(PlayerThreadMain);
			playerThread.Priority = ThreadPriority.AboveNormal;
			playerThread.Start();
			for (int i = 0; i < 50; i++)
			{
				if (playerThreadStarted)
				{
					break;
				}
				Thread.Sleep(50);
			}
		}

		private void AbortPlayerThread()
		{
			if (playerThread != null)
			{
				playerThread.Abort();
				playerThread = null;
			}
		}

		public void OpenFile(string path)
		{
			Close();
			if (File.Exists(path))
			{
				StartPlayerThread(path);
			}
		}

		private void playbackStopped_event(object sender, StoppedEventArgs e)
		{
			Stop();
			if (this.PlaybackStoppedAction != null)
			{
				this.PlaybackStoppedAction();
			}
		}
	}
}
