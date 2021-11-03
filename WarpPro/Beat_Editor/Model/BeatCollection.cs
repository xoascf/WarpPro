using System;
using System.Collections.Generic;

namespace Beat_Editor.Model
{
	public class BeatCollection
	{
		private class AdjustedTimeBeatComparer : IComparer<Beat>
		{
			public double dt;

			public AdjustedTimeBeatComparer(double dt)
			{
				this.dt = dt;
			}

			public int Compare(Beat x, Beat y)
			{
				double num = x.adjustedTime - y.adjustedTime;
				if (!(num < 0.0 - dt))
				{
					if (!(num > dt))
					{
						return 0;
					}
					return 1;
				}
				return -1;
			}
		}

		private class OrigTimeBeatComparer : IComparer<Beat>
		{
			public int Compare(Beat x, Beat y)
			{
				double num = x.OrigTime - y.OrigTime;
				if (!(num < 0.0))
				{
					if (!(num > 0.0))
					{
						return 0;
					}
					return 1;
				}
				return -1;
			}
		}

		public Beat[] beats;

		public double bpm;

		public double targetBPM;

		private bool isDirty;

		private AdjustedTimeBeatComparer comparer = new AdjustedTimeBeatComparer(0.0);

		private Beat compareBeat = new Beat(0.0);

		private OrigTimeBeatComparer origComparer = new OrigTimeBeatComparer();

		public double FirstBeatOFfset
		{
			get
			{
				if (beats.Length == 0)
				{
					return 0.0;
				}
				return beats[0].adjustedTime;
			}
			set
			{
				if (value < 0.0)
				{
					value = 0.0;
				}
				if (beats.Length != 0)
				{
					beats[0].adjustedTime = value;
				}
			}
		}

		public bool Dirty
		{
			get
			{
				return isDirty;
			}
			set
			{
				isDirty = value;
			}
		}

		public int Length
		{
			get
			{
				return beats.Length;
			}
		}

		public BeatCollection(BeatCollection other)
		{
			beats = DeepCloneBeats(other.beats);
			bpm = other.bpm;
			targetBPM = other.targetBPM;
		}

		public BeatCollection()
		{
			Clear();
		}

		public void Clear()
		{
			beats = new Beat[0];
			bpm = 0.0;
			targetBPM = 0.0;
			isDirty = false;
		}

		private static Beat[] DeepCloneBeats(Beat[] beats)
		{
			Beat[] array = new Beat[beats.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = new Beat(beats[i]);
			}
			return array;
		}

		public double[] GetBeatChangeArray(double tempoChange)
		{
			int num = beats.Length;
			double[] array = new double[2 * num + 2];
			array[0] = 0.0;
			array[1] = 0.0;
			for (int i = 0; i < num; i++)
			{
				array[2 * i + 2] = beats[i].adjustedTime;
				array[2 * i + 3] = beats[i].OrigTime / tempoChange;
			}
			return array;
		}

		public void Init(double durationSec, double firstOffset, bool reset)
		{
			double num = 60.0 / bpm;
			int i = (int)(durationSec / num) + 1;
			if (!reset && beats.Length > 1)
			{
				for (; i < beats.Length && !(beats[i].adjustedTime <= beats[i - 1].adjustedTime) && !(beats[i].adjustedTime >= durationSec + firstOffset); i++)
				{
				}
				int num2 = Math.Min(i, beats.Length) - 1;
				double adjustedTime = beats[num2].adjustedTime;
				int num3 = (int)Math.Ceiling((durationSec - adjustedTime) / num);
				if (num3 > 0)
				{
					i += num3;
				}
			}
			if (beats.Length != i)
			{
				Array.Resize(ref beats, i);
			}
			double num4 = firstOffset;
			for (int j = 0; j < i; j++)
			{
				if (beats[j] == null)
				{
					double t = num4;
					if (j > 0)
					{
						t = beats[j - 1].adjustedTime + num;
					}
					beats[j] = new Beat(t);
					beats[j].OrigTime = num4;
				}
				else if (reset)
				{
					beats[j].Reset(num4);
				}
				else
				{
					beats[j].OrigTime = num4;
				}
				num4 += num;
			}
			if (reset)
			{
				isDirty = false;
			}
		}

		public int FindNearestBeat(double time, double dt)
		{
			compareBeat.adjustedTime = time;
			comparer.dt = dt;
			return Array.BinarySearch(beats, compareBeat, comparer);
		}

		public double getOrigTimeOfNearestAdjustedTime(double adjustedTime)
		{
			if (adjustedTime < FirstBeatOFfset)
			{
				return adjustedTime;
			}
			int num = FindNearestBeat(adjustedTime, 0.0);
			if (num < 0)
			{
				num = ~num;
			}
			if (num > beats.Length - 1)
			{
				num = beats.Length - 1;
			}
			return beats[num].OrigTime;
		}

		public double getAdjustedTimeOfNearestOrigTime(double origTime)
		{
			if (!(origTime < FirstBeatOFfset) && beats.Length != 0)
			{
				compareBeat.OrigTime = origTime;
				int num = Array.BinarySearch(beats, compareBeat, origComparer);
				if (num < 0)
				{
					num = ~num;
				}
				if (num > beats.Length - 1)
				{
					num = beats.Length - 1;
				}
				return beats[num].adjustedTime;
			}
			return origTime;
		}

		public void FixMoved()
		{
			Beat[] array = beats;
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].fixMoved())
				{
					isDirty = true;
				}
			}
		}

		public void MoveAllOffset(double firstBeatMoveOffset)
		{
			Beat[] array = beats;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].MoveOffset(firstBeatMoveOffset);
			}
		}

		public void setupAudioModification(WaveDspStream dsp)
		{
			double[] array = null;
			double num = 1.0;
			if (dsp != null)
			{
				num = targetBPM / bpm;
				array = GetBeatChangeArray(num);
				dsp.SetTimeRefArray(array);
				dsp.TempoChange = num;
			}
		}
	}
}
