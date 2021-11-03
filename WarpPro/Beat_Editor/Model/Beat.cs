using System.Windows;

namespace Beat_Editor.Model
{
	public class Beat
	{
		public double adjustedTime;

		private double idealScaleTime;

		private double prevTime;

		public Point[] points = new Point[8];

		public double OrigTime
		{
			get
			{
				return idealScaleTime;
			}
			set
			{
				idealScaleTime = value;
				prevTime = adjustedTime;
			}
		}

		public double MoveDelta
		{
			get
			{
				return adjustedTime - prevTime;
			}
			set
			{
				adjustedTime = prevTime + value;
			}
		}

		public void MoveOffset(double offset)
		{
			adjustedTime += offset;
			prevTime = adjustedTime;
			idealScaleTime += offset;
		}

		public bool fixMoved()
		{
			bool result = prevTime != adjustedTime;
			prevTime = adjustedTime;
			return result;
		}

		private void Init(double t)
		{
			Reset(t);
			for (int i = 0; i < points.Length; i++)
			{
				points[i] = default(Point);
			}
		}

		public void Reset(double t)
		{
			adjustedTime = t;
			prevTime = t;
			idealScaleTime = t;
		}

		public Beat(double t)
		{
			Init(t);
		}

		public Beat(Beat other)
		{
			Init(other.adjustedTime);
			idealScaleTime = other.idealScaleTime;
		}
	}
}
