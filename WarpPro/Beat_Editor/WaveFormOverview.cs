using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Beat_Editor.Model;

namespace Beat_Editor
{
	public class WaveFormOverview : WaveForm
	{
		private Point[] rectPoints = new Point[4];

		private Polygon poly;

		private double mouseRightDownTimePos;

		private double mouseRightDownTimeBegin;

		private double mouseRightDownTimeDuration;

		private bool beginSelection;

		private double newBegin;

		private Brush hilightBrush = new SolidColorBrush(Colors.DodgerBlue)
		{
			Opacity = 0.5
		};

		public WaveFormOverview()
		{
			init();
		}

		private void init()
		{
			base.MouseLeftButtonDown += OnMouseLeftButtonDown;
			base.MouseRightButtonDown += OnMouseRightButtonDown;
			base.MouseUp += OnMouseUp;
			poly = new Polygon();
			hilightBrush.Freeze();
			poly.Fill = hilightBrush;
		}

		public void ScaleChangeListener(BeatCollection beats)
		{
			base.beats = beats;
			DrawScale();
		}

		public override void SetZoomRange(double beginTime, double endTime, bool notifyOther = true)
		{
			base.SetZoomRange(beginTime, endTime, notifyOther);
			drawSelection();
		}

		protected void drawSelection()
		{
			if (data != null && data.durationSec != 0.0)
			{
				double x = CalcTimeToXCoord(zoomedBeginTime) - 1.0;
				double x2 = CalcTimeToXCoord(zoomedEndTime) + 1.0;
				rectPoints[0].X = x;
				rectPoints[0].Y = 0.0;
				rectPoints[1].X = x2;
				rectPoints[1].Y = 0.0;
				rectPoints[2].X = x2;
				rectPoints[2].Y = selectionCanvas.ActualHeight;
				rectPoints[3].X = x;
				rectPoints[3].Y = selectionCanvas.ActualHeight;
				poly.Points.Clear();
				poly.Points.Add(rectPoints[0]);
				poly.Points.Add(rectPoints[1]);
				poly.Points.Add(rectPoints[2]);
				poly.Points.Add(rectPoints[3]);
				selectionCanvas.Children.Clear();
				selectionCanvas.Children.Add(poly);
			}
		}

		public override void Update()
		{
			base.Update();
			drawSelection();
			UpdateProgress();
		}

		protected void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (beats.Length != 0)
			{
				if (e.ClickCount > 1)
				{
					SetZoomRange(0.0, data.durationSec);
					return;
				}
				newBegin = CalcXCoordToTime(e.GetPosition(this).X);
				beginSelection = true;
				CaptureMouse();
			}
		}

		protected void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (beats.Length != 0)
			{
				mouseRightDownTimeDuration = zoomedEndTime - zoomedBeginTime;
				mouseRightDownTimeBegin = zoomedBeginTime;
				mouseRightDownTimePos = CalcXCoordToTime(e.GetPosition(this).X);
				if (mouseRightDownTimePos < zoomedBeginTime || mouseRightDownTimePos > zoomedEndTime)
				{
					mouseRightDownTimePos = zoomedBeginTime + mouseRightDownTimeDuration / 2.0;
				}
				base.IsCurrentlyMoving = true;
				CaptureMouse();
			}
		}

		protected void OnMouseUp(object sender, MouseButtonEventArgs e)
		{
			if (beats.Length != 0)
			{
				ReleaseMouseCapture();
				base.IsCurrentlyMoving = false;
				if (zoomedEndTime < zoomedBeginTime)
				{
					double num = zoomedEndTime;
					zoomedEndTime = zoomedBeginTime;
					zoomedBeginTime = num;
				}
				SetZoomRange(zoomedBeginTime, zoomedEndTime);
			}
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (beats.Length == 0)
			{
				return;
			}
			double num = CalcXCoordToTime(e.GetPosition(this).X);
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				if (beginSelection)
				{
					if (Math.Abs(num - newBegin) > 1E-06)
					{
						beginSelection = false;
						zoomedBeginTime = newBegin;
						zoomedEndTime = num;
					}
				}
				else
				{
					zoomedEndTime = num;
					drawSelection();
				}
			}
			else if (e.RightButton == MouseButtonState.Pressed)
			{
				double num2 = mouseRightDownTimeBegin + (num - mouseRightDownTimePos);
				if (num2 < 0.0)
				{
					num2 = 0.0;
				}
				double num3 = num2 + mouseRightDownTimeDuration;
				if (num3 > data.durationSec)
				{
					num3 = data.durationSec;
					num2 = num3 - mouseRightDownTimeDuration;
				}
				SetZoomRange(num2, num3);
			}
			base.OnMouseMove(e);
		}
	}
}
