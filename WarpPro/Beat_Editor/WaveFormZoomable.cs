using System.Windows.Input;

namespace Beat_Editor
{
	public class WaveFormZoomable : WaveForm
	{
		private double mouseRightDownTimePos;

		private double mouseRightDownTimeBegin;

		private double mouseRightDownTimeDuration;

		protected override bool IsFastRendering
		{
			get
			{
				if (!base.IsCurrentlyMoving)
				{
					return base.IsFastRendering;
				}
				return true;
			}
		}

		public WaveFormZoomable()
		{
			base.MouseRightButtonDown += OnMouseRightButtonDown;
			base.MouseUp += OnMouseUp;
		}

		public override void SetZoomRange(double beginTime, double endTime, bool notifyOther = true)
		{
			base.SetZoomRange(beginTime, endTime, notifyOther);
			viewBeginTime = zoomedBeginTime;
			viewEndTime = zoomedEndTime;
			Update();
		}

		protected virtual void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
		{
			mouseRightDownTimeDuration = viewEndTime - viewBeginTime;
			mouseRightDownTimePos = CalcXCoordToTime(e.GetPosition(this).X);
			mouseRightDownTimeBegin = viewBeginTime;
			CaptureMouse();
		}

		protected virtual void OnMouseUp(object sender, MouseButtonEventArgs e)
		{
			ReleaseMouseCapture();
			base.IsCurrentlyMoving = false;
			base.FreezeProgress = false;
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (e.RightButton == MouseButtonState.Pressed)
			{
				double num = mouseRightDownTimePos - e.GetPosition(this).X / pixelsPerSecond;
				if (num < 0.0)
				{
					num = 0.0;
				}
				double num2 = num + mouseRightDownTimeDuration;
				if (num2 > data.durationSec)
				{
					num2 = data.durationSec;
					num = num2 - mouseRightDownTimeDuration;
				}
				base.IsCurrentlyMoving = true;
				SetZoomRange(num, num2);
			}
			base.OnMouseMove(e);
		}
	}
}
