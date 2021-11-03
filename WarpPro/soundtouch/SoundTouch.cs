using System;
using System.Runtime.InteropServices;

namespace soundtouch
{
	public class SoundTouch : IDisposable
	{
		private class NativeMethods
		{
			[DllImport("SoundTouch.dll", CallingConvention = CallingConvention.Cdecl)]
			internal static extern int getVersionId();

			[DllImport("SoundTouch.dll", CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr soundtouch_createInstance();

			[DllImport("SoundTouch.dll", CallingConvention = CallingConvention.Cdecl)]
			internal static extern void soundtouch_destroyInstance(IntPtr h);

			[DllImport("SoundTouch.dll", CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr soundtouch_getVersionString();

			[DllImport("SoundTouch.dll", CallingConvention = CallingConvention.Cdecl)]
			internal static extern void soundtouch_setRate(IntPtr h, float newRate);

			[DllImport("SoundTouch.dll", CallingConvention = CallingConvention.Cdecl)]
			internal static extern void soundtouch_setTempo(IntPtr h, float newTempo);

			[DllImport("SoundTouch.dll", CallingConvention = CallingConvention.Cdecl)]
			internal static extern void soundtouch_setRateChange(IntPtr h, float newRate);

			[DllImport("SoundTouch.dll", CallingConvention = CallingConvention.Cdecl)]
			internal static extern void soundtouch_setTempoChange(IntPtr h, float newTempo);

			[DllImport("SoundTouch.dll", CallingConvention = CallingConvention.Cdecl)]
			internal static extern void soundtouch_setPitch(IntPtr h, float newPitch);

			[DllImport("SoundTouch.dll", CallingConvention = CallingConvention.Cdecl)]
			internal static extern void soundtouch_setPitchOctaves(IntPtr h, float newPitch);

			[DllImport("SoundTouch.dll", CallingConvention = CallingConvention.Cdecl)]
			internal static extern void soundtouch_setPitchSemiTones(IntPtr h, float newPitch);

			[DllImport("SoundTouch.dll", CallingConvention = CallingConvention.Cdecl)]
			internal static extern void soundtouch_setChannels(IntPtr h, uint numChannels);

			[DllImport("SoundTouch.dll", CallingConvention = CallingConvention.Cdecl)]
			internal static extern void soundtouch_setSampleRate(IntPtr h, uint srate);

			[DllImport("SoundTouch.dll", CallingConvention = CallingConvention.Cdecl)]
			internal static extern void soundtouch_flush(IntPtr h);

			[DllImport("SoundTouch.dll", CallingConvention = CallingConvention.Cdecl)]
			internal static extern void soundtouch_putSamples(IntPtr h, float[] samples, uint numSamples);

			[DllImport("SoundTouch.dll", CallingConvention = CallingConvention.Cdecl)]
			internal static extern void soundtouch_putSamples_i16(IntPtr h, short[] samples, uint numSamples);

			[DllImport("SoundTouch.dll", CallingConvention = CallingConvention.Cdecl)]
			internal static extern void soundtouch_clear(IntPtr h);

			[DllImport("SoundTouch.dll", CallingConvention = CallingConvention.Cdecl)]
			internal static extern int soundtouch_setSetting(IntPtr h, int settingId, int value);

			[DllImport("SoundTouch.dll", CallingConvention = CallingConvention.Cdecl)]
			internal static extern int soundtouch_getSetting(IntPtr h, int settingId);

			[DllImport("SoundTouch.dll", CallingConvention = CallingConvention.Cdecl)]
			internal static extern uint soundtouch_numUnprocessedSamples(IntPtr h);

			[DllImport("SoundTouch.dll", CallingConvention = CallingConvention.Cdecl)]
			internal static extern uint soundtouch_receiveSamples(IntPtr h, float[] outBuffer, uint maxSamples);

			[DllImport("SoundTouch.dll", CallingConvention = CallingConvention.Cdecl)]
			internal static extern uint soundtouch_receiveSamples_i16(IntPtr h, short[] outBuffer, uint maxSamples);

			[DllImport("SoundTouch.dll", CallingConvention = CallingConvention.Cdecl)]
			internal static extern uint soundtouch_numSamples(IntPtr h);

			[DllImport("SoundTouch.dll", CallingConvention = CallingConvention.Cdecl)]
			internal static extern int soundtouch_isEmpty(IntPtr h);

			[DllImport("SoundTouch.dll", CallingConvention = CallingConvention.Cdecl)]
			internal static extern void soundtouch_setTimeRefArray(IntPtr h, double[] timeRefArray, int len);

			[DllImport("SoundTouch.dll", CallingConvention = CallingConvention.Cdecl)]
			internal static extern uint soundtouch_seekPos(IntPtr h, double inputPos);
		}

		private IntPtr handle;

		private bool IsDisposed;

		public SoundTouch()
		{
			handle = NativeMethods.soundtouch_createInstance();
		}

		~SoundTouch()
		{
			Dispose(false);
		}

		public static string GetVersionString()
		{
			return Marshal.PtrToStringAnsi(NativeMethods.soundtouch_getVersionString());
		}

		public uint NumSamples()
		{
			return NativeMethods.soundtouch_numSamples(handle);
		}

		public void PutSamples(float[] samples, uint numSamples)
		{
			NativeMethods.soundtouch_putSamples(handle, samples, numSamples);
		}

		public void SetChannels(uint numChannels)
		{
			NativeMethods.soundtouch_setChannels(handle, numChannels);
		}

		public void SetSampleRate(uint srate)
		{
			NativeMethods.soundtouch_setSampleRate(handle, srate);
		}

		public uint ReceiveSamples(float[] outBuffer, uint maxSamples)
		{
			return NativeMethods.soundtouch_receiveSamples(handle, outBuffer, maxSamples);
		}

		public void Flush()
		{
			NativeMethods.soundtouch_flush(handle);
		}

		internal uint SeekPos(double position)
		{
			return NativeMethods.soundtouch_seekPos(handle, position);
		}

		public void Clear()
		{
			NativeMethods.soundtouch_clear(handle);
		}

		public void SetTempo(float newTempo)
		{
			NativeMethods.soundtouch_setTempo(handle, newTempo);
		}

		public void SetTempoChange(float newTempo)
		{
			NativeMethods.soundtouch_setTempoChange(handle, newTempo);
		}

		public void SetRate(float newRate)
		{
			NativeMethods.soundtouch_setTempo(handle, newRate);
		}

		public void SetRateChange(float newRate)
		{
			NativeMethods.soundtouch_setRateChange(handle, newRate);
		}

		public void SetPitch(float newPitch)
		{
			NativeMethods.soundtouch_setPitch(handle, newPitch);
		}

		public void SetPitchOctaves(float newPitch)
		{
			NativeMethods.soundtouch_setPitchOctaves(handle, newPitch);
		}

		public void SetPitchSemiTones(float newPitch)
		{
			NativeMethods.soundtouch_setPitchSemiTones(handle, newPitch);
		}

		public void PutSamples_i16(short[] samples, uint numSamples)
		{
			NativeMethods.soundtouch_putSamples_i16(handle, samples, numSamples);
		}

		public int SetSetting(int settingId, int value)
		{
			return NativeMethods.soundtouch_setSetting(handle, settingId, value);
		}

		public int getSetting(int settingId)
		{
			return NativeMethods.soundtouch_getSetting(handle, settingId);
		}

		public uint NumUnprocessedSamples()
		{
			return NativeMethods.soundtouch_numUnprocessedSamples(handle);
		}

		public uint receiveSamples_i16(short[] outBuffer, uint maxSamples)
		{
			return NativeMethods.soundtouch_receiveSamples_i16(handle, outBuffer, maxSamples);
		}

		public int IsEmpty()
		{
			return NativeMethods.soundtouch_isEmpty(handle);
		}

		public void SetTimeRefArray(double[] timeRefArray)
		{
			int len = ((timeRefArray != null) ? timeRefArray.Length : 0);
			NativeMethods.soundtouch_setTimeRefArray(handle, timeRefArray, len);
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
					NativeMethods.soundtouch_destroyInstance(handle);
					handle = IntPtr.Zero;
				}
				IsDisposed = true;
			}
		}
	}
}
