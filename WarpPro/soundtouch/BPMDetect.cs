using System;
using System.Runtime.InteropServices;

namespace soundtouch
{
	public class BPMDetect : IDisposable
	{
		private class NativeMethods
		{
			[DllImport("SoundTouch.dll", CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr bpm_createInstance(int numChannels, int aSampleRate);

			[DllImport("SoundTouch.dll", CallingConvention = CallingConvention.Cdecl)]
			internal static extern void bpm_destroyInstance(IntPtr h);

			[DllImport("SoundTouch.dll", CallingConvention = CallingConvention.Cdecl)]
			internal static extern void bpm_inputSamples(IntPtr h, float[] samples, int numSamples);

			[DllImport("SoundTouch.dll", CallingConvention = CallingConvention.Cdecl)]
			internal static extern void bpm_inputSamplesI16(IntPtr h, short[] samples, int numSamples);

			[DllImport("SoundTouch.dll", CallingConvention = CallingConvention.Cdecl)]
			internal static extern float bpm_getBpm(IntPtr h);

			[DllImport("SoundTouch.dll", CallingConvention = CallingConvention.Cdecl)]
			internal static extern int bpm_getBeats(IntPtr h, float[] pos, float[] values);
		}

		private IntPtr handle;

		private int channels;

		private bool IsDisposed;

		public BPMDetect(int numChannels, int aSampleRate)
		{
			handle = NativeMethods.bpm_createInstance(numChannels, aSampleRate);
			channels = numChannels;
		}

		~BPMDetect()
		{
			Dispose(false);
		}

		public void InputSamples(float[] samples, int len = -1)
		{
			if (len < 0)
			{
				len = samples.Length;
			}
			NativeMethods.bpm_inputSamples(handle, samples, len / channels);
		}

		public void InputSamples(short[] samples, int len = -1)
		{
			if (len < 0)
			{
				len = samples.Length;
			}
			NativeMethods.bpm_inputSamplesI16(handle, samples, len / channels);
		}

		public float GetBPM()
		{
			return NativeMethods.bpm_getBpm(handle);
		}

		public int GetBeats(out float[] pos, out float[] values)
		{
			int num = NativeMethods.bpm_getBeats(handle, null, null);
			pos = new float[num];
			values = new float[num];
			return NativeMethods.bpm_getBeats(handle, pos, values);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool alsoManaged)
		{
			if (!IsDisposed)
			{
				if (handle != IntPtr.Zero)
				{
					NativeMethods.bpm_destroyInstance(handle);
					handle = IntPtr.Zero;
				}
				IsDisposed = true;
			}
		}
	}
}
