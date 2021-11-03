using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Beat_Editor.Controller;
using NAudio.Wave;

namespace Beat_Editor
{
	public partial class OpenProgress : Window, IComponentConnector
	{
		private class ReadWorker : BackgroundWorker
		{
			private OpenProgress master;

			internal bool success;

			public ReadWorker(OpenProgress master)
			{
				this.master = master;
				base.WorkerReportsProgress = true;
				base.DoWork += Run;
			}

			private void Run(object sender, DoWorkEventArgs e)
			{
				try
				{
					WaveStream waveStream = FileProcessor.OpenAudioReaderStream(master.filename);
					if (waveStream == null)
					{
						return;
					}
					DetectBpmStream detectBpmStream = new DetectBpmStream(waveStream);
					WaveformDataProvider wd = master.wd;
					wd.ReadDataProgress = (Action<int>)Delegate.Combine(wd.ReadDataProgress, new Action<int>(LoadMp3Progress));
					master.wd.readData(detectBpmStream);
					wd = master.wd;
					wd.ReadDataProgress = (Action<int>)Delegate.Remove(wd.ReadDataProgress, new Action<int>(LoadMp3Progress));
					master.bpm = detectBpmStream.BPM();
					if (master.bpm > 170.0)
					{
						master.bpm *= 0.5;
					}
					detectBpmStream.Beats(out master.beatPos, out master.beatValues);
					waveStream.Close();
					success = true;
				}
				catch (Exception value)
				{
					Console.WriteLine(value);
				}
				ReportProgress(101);
			}

			private void LoadMp3Progress(int percent)
			{
				if (percent <= 100)
				{
					ReportProgress(percent);
				}
			}
		}

		private string filename;

		private WaveformDataProvider wd;

		private double bpm;

		internal float[] beatPos;

		internal float[] beatValues;

		public Action<OpenProgress, double> BPMReadyHandler;

		public OpenProgress(Window owner)
		{
			InitializeComponent();
			base.Owner = owner;
			base.WindowStartupLocation = WindowStartupLocation.CenterOwner;
			progressOpen.Value = 0.0;
		}

		public void Load(WaveformDataProvider wd, string name)
		{
			this.wd = wd;
			filename = name;
			base.Title = "Opening" + name;
			Show();
			ReadWorker readWorker = new ReadWorker(this);
			readWorker.ProgressChanged += ProgressUpdate;
			readWorker.RunWorkerAsync();
		}

		private void ProgressUpdate(object sender, ProgressChangedEventArgs e)
		{
			progressOpen.Value = e.ProgressPercentage;
			if (e.ProgressPercentage <= 100)
			{
				return;
			}
			Close();
			Application.Current.MainWindow.Focus();
			ReadWorker obj = (ReadWorker)sender;
			obj.ProgressChanged -= ProgressUpdate;
			wd = null;
			if (!obj.success)
			{
#if NET40
				Dispatcher.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate
#else
				base.Dispatcher.Invoke(delegate
#endif
				{
					MessageBox.Show("Can't open this file.", "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
				});
			}
			else if (BPMReadyHandler != null)
			{
				BPMReadyHandler(this, bpm);
			}
		}
	}
}
