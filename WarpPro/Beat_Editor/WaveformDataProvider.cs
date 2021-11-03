using System;
using System.Collections.Generic;
using NAudio.Wave;

namespace Beat_Editor
{
	public class WaveformDataProvider
	{
		private abstract class SampleData
		{
			public int decimateFactor = 1;

			public double durationSec;

			public int absMaxValue;

			public abstract void GetDatapoints(float[] min, float[] max, double beginTime, double endTime);
		}

		private class SampleDataRaw : SampleData
		{
			public sbyte[] samples;

			public event Action<int> ReadDataProgress;

			public SampleDataRaw()
			{
				samples = new sbyte[0];
			}

			public void readData(WaveStream input)
			{
				byte[] array = new byte[BLOCKSIZE * 2];
				short[] array2 = new short[BLOCKSIZE];
				int num = 0;
				int num2 = -1;
				int channels = input.WaveFormat.Channels;
				samples = new sbyte[input.Length * 8L / (input.WaveFormat.Channels * input.WaveFormat.BitsPerSample)];
				int num3 = 0;
				durationSec = (double)input.Length * 8.0 / (double)(input.WaveFormat.BitsPerSample * input.WaveFormat.Channels * input.WaveFormat.SampleRate);
				int num5;
				do
				{
					int num4 = input.Read(array, 0, BLOCKSIZE * 2);
					num5 = ((num + num4 / (2 * channels) >= samples.Length) ? (samples.Length - num) : (num4 / 2));
					if (num5 <= 0)
					{
						continue;
					}
					Buffer.BlockCopy(array, 0, array2, 0, num4);
					if (channels == 1)
					{
						for (int i = 0; i < num5; i++)
						{
							sbyte b = (sbyte)(array2[i] / 256);
							samples[num] = b;
							if (num3 < b || num3 < -b)
							{
								num3 = Math.Abs(b);
							}
							num++;
						}
					}
					else
					{
						if (channels != 2)
						{
							throw new NotImplementedException();
						}
						for (int j = 0; j < num5; j += 2)
						{
							sbyte b2 = (sbyte)((array2[j] + array2[j + 1]) / 512);
							samples[num] = b2;
							if (num3 < b2 || num3 < -b2)
							{
								num3 = Math.Abs(b2);
							}
							num++;
						}
					}
					int num6 = (int)(100L * num / samples.Length);
					if (this.ReadDataProgress != null && num2 != num6)
					{
						this.ReadDataProgress(num6);
						num2 = num6;
					}
				}
				while (num5 > 0);
				absMaxValue = num3;
			}

			public override void GetDatapoints(float[] min, float[] max, double beginTime, double endTime)
			{
				double num = endTime - beginTime;
				int num2 = min.Length;
				for (int i = 0; i < num2; i++)
				{
					double num3 = beginTime + (double)i * num / (double)num2;
					int num4 = (int)(num3 * (double)samples.Length / durationSec);
					int num5 = (int)((num3 + num / (double)num2) * (double)samples.Length / durationSec);
					sbyte b = samples[num4];
					sbyte b2 = samples[num4];
					for (int j = num4 + 1; j < num5; j++)
					{
						b = ((samples[j] < b) ? samples[j] : b);
						b2 = ((samples[j] > b2) ? samples[j] : b2);
					}
					min[i] = (float)b / (float)absMaxValue;
					max[i] = (float)b2 / (float)absMaxValue;
				}
			}
		}

		private class SampleDataDecimated : SampleData
		{
			private sbyte[] samplesMin;

			private sbyte[] samplesMax;

			public SampleDataDecimated(SampleDataRaw input, int decimateBy)
			{
				sbyte[] samples = input.samples;
				durationSec = input.durationSec;
				absMaxValue = input.absMaxValue;
				int num = 0;
				decimateFactor = decimateBy;
				samplesMin = new sbyte[samples.Length / decimateBy + 1];
				samplesMax = new sbyte[samples.Length / decimateBy + 1];
				for (int i = 0; i < samples.Length; i += decimateBy)
				{
					sbyte b = samples[i];
					sbyte b2 = samples[i];
					for (int j = 1; j < decimateBy && i + j < samples.Length; j++)
					{
						b = ((samples[i + j] < b) ? samples[i + j] : b);
						b2 = ((samples[i + j] > b2) ? samples[i + j] : b2);
					}
					samplesMin[num] = b;
					samplesMax[num] = b2;
					num++;
				}
			}

			public override void GetDatapoints(float[] min, float[] max, double beginTime, double endTime)
			{
				double num = endTime - beginTime;
				int num2 = min.Length;
				for (int i = 0; i < num2; i++)
				{
					double num3 = beginTime + (double)i * num / (double)num2;
					int num4 = (int)(num3 * (double)samplesMin.Length / durationSec);
					int num5 = (int)((num3 + num / (double)num2) * (double)samplesMin.Length / durationSec);
					sbyte b = samplesMin[num4];
					sbyte b2 = samplesMax[num4];
					for (int j = num4 + 1; j < num5; j++)
					{
						b = ((samplesMin[j] < b) ? samplesMin[j] : b);
						b2 = ((samplesMax[j] > b2) ? samplesMax[j] : b2);
					}
					min[i] = (float)b / (float)absMaxValue;
					max[i] = (float)b2 / (float)absMaxValue;
				}
			}
		}

		public bool loaded;

		public Action<int> ReadDataProgress;

		private static readonly int BLOCKSIZE = 8192;

		public float absmaxval;

		public int sampleRate;

		public Action event_DataChanged;

		private SampleDataRaw raw = new SampleDataRaw();

		private List<SampleDataDecimated> decimated = new List<SampleDataDecimated>();

		public double durationSec
		{
			get
			{
				if (raw == null)
				{
					return 0.0;
				}
				return raw.durationSec;
			}
		}

		public WaveformDataProvider()
		{
			raw.durationSec = 0.0;
			sampleRate = 44100;
		}

		public void GetDatapoints(float[] min, float[] max, double beginTime, double endTime)
		{
			if (beginTime < 0.0)
			{
				beginTime = 0.0;
			}
			if (endTime > durationSec)
			{
				endTime = durationSec;
			}
			if (min.Length != max.Length)
			{
				throw new Exception("min/max length mismatch");
			}
			double num = endTime - beginTime;
			if (num < 1E-09)
			{
				for (int i = 0; i < min.Length; i++)
				{
					min[i] = 0f;
					max[i] = 0f;
				}
				return;
			}
			int num2 = (int)((double)sampleRate * num / (double)min.Length);
			SampleData sampleData = raw;
			foreach (SampleDataDecimated item in decimated)
			{
				if (item.decimateFactor < num2)
				{
					sampleData = item;
					continue;
				}
				break;
			}
			sampleData.GetDatapoints(min, max, beginTime, endTime);
		}

		private void progressHandler(int percent)
		{
			if (ReadDataProgress != null)
			{
				ReadDataProgress(75 * percent / 100);
			}
		}

		public virtual void readData(WaveStream input)
		{
			sampleRate = input.WaveFormat.SampleRate;
			raw.ReadDataProgress += progressHandler;
			raw.readData(input);
			raw.ReadDataProgress -= progressHandler;
			int num = (int)(input.Length * 8L / (input.WaveFormat.Channels * input.WaveFormat.BitsPerSample) / 1920L);
			int num2 = 75;
			decimated.Clear();
			for (int num3 = 8; num3 < num; num3 *= 8)
			{
				SampleDataDecimated item = new SampleDataDecimated(raw, num3);
				decimated.Add(item);
				num2 += 5;
				if (ReadDataProgress != null)
				{
					ReadDataProgress((num2 < 100) ? num2 : 99);
				}
			}
			if (ReadDataProgress != null)
			{
				ReadDataProgress(101);
			}
			if (event_DataChanged != null)
			{
				event_DataChanged();
			}
			loaded = true;
		}
	}
}
