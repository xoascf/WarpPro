using System;

namespace Beat_Editor.Controller
{
	internal class BlockConvertI32 : IBlockConvert
	{
		private int[] buff = new int[0];

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
				buff = new int[num];
			}
			Buffer.BlockCopy(raw, 0, buff, 0, numBytes);
			for (int i = 0; i < num; i++)
			{
				dest[i] = (short)(buff[i] >> 16);
			}
			Buffer.BlockCopy(dest, 0, raw, 0, num * 2);
			return num;
		}
	}
}
