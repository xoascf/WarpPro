using System;

namespace Beat_Editor.Controller
{
	internal class BlockConvertF32 : IBlockConvert
	{
		private float[] buff = new float[0];

		public int elemSize
		{
			get
			{
				return 4;
			}
		}

		public int convert(short[] dest, Array raw, int numBytes)
		{
			int num = numBytes / elemSize;
			if (buff.Length < num)
			{
				buff = new float[num];
			}
			Buffer.BlockCopy(raw, 0, buff, 0, numBytes);
			for (int i = 0; i < num; i++)
			{
				int num2 = (int)(32768.0 * (double)buff[i]);
				num2 = ((num2 < -32768) ? (-32768) : ((num2 > 32767) ? 32767 : num2));
				dest[i] = (short)num2;
			}
			Buffer.BlockCopy(dest, 0, raw, 0, num * 2);
			return num;
		}
	}
}
