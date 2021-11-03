using System;
using NAudio.Dsp;
using NAudio.Wave;

namespace Beat_Editor
{
	internal class LPFWaveStream : WaveStream
	{
		private WaveStream input;

		private BiQuadFilter lpf;

		private short[] sbuffer = new short[0];

		public override long Length
		{
			get
			{
				return input.Length;
			}
		}

		public override long Position
		{
			get
			{
				return input.Position;
			}
			set
			{
				input.Position = value;
			}
		}

		public override WaveFormat WaveFormat
		{
			get
			{
				return input.WaveFormat;
			}
		}

		public LPFWaveStream(WaveStream input)
		{
			this.input = input;
			lpf = BiQuadFilter.LowPassFilter(input.WaveFormat.SampleRate, 250f, 1f);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			int channels = input.WaveFormat.Channels;
			if (sbuffer.Length < count / 2)
			{
				sbuffer = new short[count / 2];
			}
			count = input.Read(buffer, offset, count);
			int num = count / (channels * 2);
			if (num == 0)
			{
				return 0;
			}
			Buffer.BlockCopy(buffer, offset, sbuffer, 0, count);
			for (int i = 0; i < num; i++)
			{
				float num2 = sbuffer[channels * i];
				for (int j = 1; j < channels; j++)
				{
					num2 += (float)sbuffer[channels * i + j];
				}
				short num3 = (short)lpf.Transform(num2 / (float)channels);
				for (int k = 0; k < channels; k++)
				{
					sbuffer[channels * i + k] = num3;
				}
			}
			Buffer.BlockCopy(sbuffer, 0, buffer, offset, count);
			return count;
		}
	}
}
