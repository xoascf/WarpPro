using System;

namespace Beat_Editor.Controller
{
	internal class BlockConvertI16 : IBlockConvert
	{
		public int elemSize
		{
			get
			{
				return 2;
			}
		}

		public int convert(short[] dest, Array raw, int numBytes)
		{
			Buffer.BlockCopy(raw, 0, dest, 0, numBytes);
			return numBytes / elemSize;
		}
	}
}
