using System;
using NAudio.Wave;
using soundtouch;

namespace Beat_Editor.Controller
{
	internal class DetectBpmStream : WaveStream
	{
		private WaveStream input;

		private WaveFormat format;

		private BPMDetect bpmd;

		private short[] sbuffer = new short[0];

		private float[] fbuffer = new float[0];

		private IBlockConvert sampleConverter;

		public override long Length
		{
			get
			{
				return input.Length * 2L / sampleConverter.elemSize;
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
				return format;
			}
		}

		public DetectBpmStream(WaveStream input)
		{
			this.input = input;
			format = input.WaveFormat;
			bpmd = new BPMDetect(input.WaveFormat.Channels, input.WaveFormat.SampleRate);
			if (input.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
			{
				sampleConverter = new BlockConvertF32();
				format = WaveFormat.CreateCustomFormat(WaveFormatEncoding.Pcm, format.SampleRate, format.Channels, format.AverageBytesPerSecond, format.Channels * 2, 16);
				return;
			}
			if (input.WaveFormat.Encoding == WaveFormatEncoding.Pcm && input.WaveFormat.BitsPerSample == 16)
			{
				sampleConverter = new BlockConvertI16();
				return;
			}
			if (input.WaveFormat.Encoding != WaveFormatEncoding.Pcm || input.WaveFormat.BitsPerSample != 32)
			{
				throw new NotImplementedException();
			}
			sampleConverter = new BlockConvertI32();
		}

		public double BPM()
		{
			return bpmd.GetBPM();
		}

		public void Beats(out float[] pos, out float[] values)
		{
			bpmd.GetBeats(out pos, out values);
		}

		public override int Read(byte[] sampleBuffer, int offset, int numBytes)
		{
			int num = input.Read(sampleBuffer, offset, numBytes) / sampleConverter.elemSize;
			if (sbuffer.Length < num)
			{
				sbuffer = new short[num];
			}
			num = sampleConverter.convert(sbuffer, sampleBuffer, numBytes);
			bpmd.InputSamples(sbuffer, num);
			return num * 2;
		}
	}
}
