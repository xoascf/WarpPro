using System;

namespace Beat_Editor.Controller
{
	internal interface IBlockConvert
	{
		int elemSize { get; }

		int convert(short[] dest, Array raw, int numBytes);
	}
}
