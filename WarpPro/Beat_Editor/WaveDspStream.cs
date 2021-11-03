using System;
using NAudio.Dsp;
using NAudio.Wave;
using soundtouch;

namespace Beat_Editor
{
	public class WaveDspStream : WaveStream
	{
		private WaveChannel32 inputStr;

		private byte[] bytebuffer = new byte[4096];

		private float[] floatbuffer = new float[1024];

		private double[] timeRefArray = new double[0];

		private SoundTouch st = new SoundTouch();

		private BiQuadFilter filter;

		private double pos;

		private bool doMono;

		private int _prevOutSeekIx;

		private bool isProcessingEnabled = true;

		private double _tempoChange = 1.0;

		private int n_in;

		private int n_out;

		private bool onetime = true;

		private long latestReadTicks;

		public override long Length
		{
			get
			{
				return inputStr.Length;
			}
		}

		private int BytesPerSample
		{
			get
			{
				return inputStr.WaveFormat.Channels * inputStr.WaveFormat.BitsPerSample / 8;
			}
		}

		public override long Position
		{
			get
			{
				return (long)pos;
			}
			set
			{
				lock (st)
				{
					double num = inputStr.WaveFormat.SampleRate * BytesPerSample;
					double num2 = CalcInputPos((double)value / (num * _tempoChange));
					inputStr.Position = (int)(num2 * (double)inputStr.WaveFormat.SampleRate) * BytesPerSample;
					SeekPos(num2);
					onetime = true;
				}
			}
		}

		public override WaveFormat WaveFormat
		{
			get
			{
				return inputStr.WaveFormat;
			}
		}

		public bool ProcessingEnabled
		{
			get
			{
				return isProcessingEnabled;
			}
			set
			{
				if (isProcessingEnabled != value)
				{
					lock (st)
					{
						isProcessingEnabled = value;
						st.Clear();
					}
				}
			}
		}

		public double TempoChange
		{
			get
			{
				return _tempoChange;
			}
			set
			{
				_tempoChange = value;
			}
		}

		public long TimeSinceLatestDataReadMsec
		{
			get
			{
				if (latestReadTicks == 0L)
				{
					return 0L;
				}
				return (DateTime.Now.Ticks - latestReadTicks) / 10000L;
			}
		}

		public WaveDspStream(WaveChannel32 input, bool mono)
		{
			inputStr = input;
			doMono = mono;
			st.SetChannels((uint)input.WaveFormat.Channels);
			st.SetSampleRate((uint)input.WaveFormat.SampleRate);
			Position = 0L;
			pos = 0.0;
			filter = BiQuadFilter.HighPassFilter(input.WaveFormat.SampleRate, 150f, 1f);
		}

		public double CalcInputPos(double outputPosition)
		{
			int num = timeRefArray.Length - 3;
			if (num <= 0)
			{
				return outputPosition;
			}
			int i = ((_prevOutSeekIx < num) ? _prevOutSeekIx : 0);
			while (i > 0 && !(outputPosition >= timeRefArray[i + 1]))
			{
				i -= 2;
			}
			for (; i < num && !(outputPosition < timeRefArray[i + 3]); i += 2)
			{
			}
			if (i >= num)
			{
				return outputPosition * _tempoChange;
			}
			_prevOutSeekIx = i;
			double num2 = timeRefArray[i];
			double num3 = timeRefArray[i + 2];
			double num4 = timeRefArray[i + 1];
			double num5 = timeRefArray[i + 3];
			return num2 + (outputPosition - num4) * (num3 - num2) / (num5 - num4);
		}

		private void SeekPos(double position)
		{
			lock (st)
			{
				st.Clear();
				pos = st.SeekPos(position) * BytesPerSample;
				latestReadTicks = 0L;
			}
		}

		public void SetTimeRefArray(double[] timeRef)
		{
			lock (st)
			{
				timeRefArray = (double[])timeRef.Clone();
				st.SetTimeRefArray(timeRef);
			}
		}

		private void monoize(float[] buffer, uint frames)
		{
			uint channels = (uint)inputStr.WaveFormat.Channels;
			switch (channels)
			{
			case 1u:
			{
				for (uint num = 0u; num < frames; num++)
				{
					buffer[num] = filter.Transform(buffer[num]);
				}
				return;
			}
			case 2u:
			{
				for (uint num2 = 0u; num2 < 2 * frames; num2 += 2)
				{
					float inSample = 0.5f * (buffer[num2] + buffer[num2 + 1]);
					buffer[num2 + 1] = (buffer[num2] = filter.Transform(inSample));
				}
				return;
			}
			}
			for (uint num3 = 0u; num3 < channels * frames; num3 += channels)
			{
				float num4 = buffer[num3];
				for (uint num5 = 1u; num5 < channels; num5++)
				{
					num4 += buffer[num3 + num5];
				}
				num4 /= (float)channels;
				for (uint num6 = 0u; num6 < channels; num6++)
				{
					buffer[num3 + num6] = num4;
				}
			}
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			lock (st)
			{
				latestReadTicks = DateTime.Now.Ticks;
				if (!isProcessingEnabled)
				{
					return inputStr.Read(buffer, offset, count);
				}
				int num = Math.Max(count, bytebuffer.Length) / 4;
				if (floatbuffer.Length < num)
				{
					floatbuffer = new float[num];
				}
				while (st.NumSamples() * BytesPerSample < count)
				{
					int num2 = inputStr.Read(bytebuffer, 0, bytebuffer.Length);
					n_in += num2;
					if (num2 != 0)
					{
						Buffer.BlockCopy(bytebuffer, 0, floatbuffer, 0, num2);
						int channels = inputStr.WaveFormat.Channels;
						uint num3 = (uint)(num2 / (4 * channels));
						if (doMono)
						{
							monoize(floatbuffer, num3);
						}
						st.PutSamples(floatbuffer, num3);
						continue;
					}
					if (onetime)
					{
						onetime = false;
						st.Flush();
					}
					break;
				}
				int num4 = (int)st.ReceiveSamples(floatbuffer, (uint)(count / BytesPerSample));
				n_out += num4 * BytesPerSample;
				Buffer.BlockCopy(floatbuffer, 0, buffer, 0, num4 * BytesPerSample);
				pos += num4 * BytesPerSample;
				return num4 * BytesPerSample;
			}
		}
	}
}
