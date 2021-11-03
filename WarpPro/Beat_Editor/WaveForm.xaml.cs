using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Beat_Editor.Model;
using Sample_NAudio;

namespace Beat_Editor
{
	public partial class WaveForm : UserControl, IComponentConnector
	{
		public WaveformDataProvider data;

		protected double viewBeginTime;

		protected double viewEndTime;

		public double zoomedBeginTime;

		public double zoomedEndTime;

		public BeatCollection beats = new BeatCollection();

		protected Polygon[] beatPolys = new Polygon[0];

		protected double firstBeatMoveOffset;

		private DispatcherTimer playProgressTimer;

		private DispatcherTimer lazyRenderTimer = new DispatcherTimer();

		private bool isCurrentlyZooming;

		private bool _isCurrentlyMoving;

		public static readonly Brush WaveformLine = new SolidColorBrush(Color.FromArgb(byte.MaxValue, 196, 230, byte.MaxValue));

		public static readonly Brush brushBar = new SolidColorBrush(Color.FromArgb(byte.MaxValue, 67, 97, 137));

		public static readonly Brush brushInt = new SolidColorBrush(Color.FromArgb(byte.MaxValue, 63, 90, 129));

		private Polyline polymin;

		private Polyline polymax;

		private Line scaleline;

		protected double pixelsPerSecond;

		private float[] waveMin;

		private float[] waveMax;

		protected double yOffset;

		protected double yMargin = 10.0;

		public Action<double, double, bool> event_zoomChanged;

		public Action<double> event_CursorPosition;

		public Action<double> event_PlayPosition;

		protected Polyline progressLine;

		private double progressPos;

		private bool isZoomFollowEnabled;

		private static readonly double streamTimeBias = 0.0;

		protected Line firstBeatLine = new Line
		{
			Stroke = Brushes.Blue,
			StrokeThickness = 2.5
		};

		protected PointCollection progressPointCollection = new PointCollection();

		private bool isProgressFrozen;

		protected bool IsCurrentlyMoving
		{
			get
			{
				return _isCurrentlyMoving;
			}
			set
			{
				bool num = !value && IsFastRendering;
				_isCurrentlyMoving = value;
				if (num)
				{
					DrawWaveform();
				}
			}
		}

		protected virtual bool IsFastRendering
		{
			get
			{
				return isCurrentlyZooming;
			}
		}

		public bool ZoomFollowPlayback
		{
			get
			{
				return isZoomFollowEnabled;
			}
			set
			{
				isZoomFollowEnabled = value;
			}
		}

		public bool FreezeProgress
		{
			set
			{
				isProgressFrozen = value;
			}
		}

		public double Bpm
		{
			get
			{
				return beats.bpm;
			}
		}

		public double FirstBeatOffset
		{
			get
			{
				return beats.FirstBeatOFfset;
			}
			set
			{
				beats.FirstBeatOFfset = value;
			}
		}

		public WaveForm(WaveformDataProvider dataProvider)
		{
			InitializeComponent();
			init(dataProvider);
		}

		public WaveForm()
		{
			InitializeComponent();
			init(new WaveformDataProvider());
		}

		public void setDataProvider(WaveformDataProvider waveData)
		{
			if (data != null)
			{
				WaveformDataProvider waveformDataProvider = data;
				waveformDataProvider.event_DataChanged = (Action)Delegate.Remove(waveformDataProvider.event_DataChanged, new Action(Handler_DataChangeInvoker));
			}
			data = waveData;
			if (data != null)
			{
				WaveformDataProvider waveformDataProvider = data;
				waveformDataProvider.event_DataChanged = (Action)Delegate.Combine(waveformDataProvider.event_DataChanged, new Action(Handler_DataChangeInvoker));
			}
			Update();
		}

		private void init(WaveformDataProvider dataProvider)
		{
			WaveformLine.Freeze();
			brushBar.Freeze();
			brushInt.Freeze();
			data = dataProvider;
			WaveformDataProvider waveformDataProvider = data;
			waveformDataProvider.event_DataChanged = (Action)Delegate.Combine(waveformDataProvider.event_DataChanged, new Action(DataChanged));
			viewBeginTime = 0.0;
			viewEndTime = data.durationSec;
			playProgressTimer = new DispatcherTimer();
			playProgressTimer.Interval = new TimeSpan(0, 0, 0, 0, 40);
			playProgressTimer.Tick += timer_Tick;
			playProgressTimer.Start();
			lazyRenderTimer.Tick += LazyRenderTimerTick;
			lazyRenderTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
			IsCurrentlyMoving = false;
			progressLine = new Polyline
			{
				Stroke = Brushes.Green,
				StrokeThickness = 3.0,
				SnapsToDevicePixels = true
			};
			polymin = new Polyline
			{
				Stroke = WaveformLine,
				StrokeThickness = 1.0,
				SnapsToDevicePixels = true
			};
			polymax = new Polyline
			{
				Stroke = WaveformLine,
				StrokeThickness = 1.0,
				SnapsToDevicePixels = true
			};
			scaleline = new Line
			{
				Stroke = Brushes.Black,
				StrokeThickness = 1.0,
				SnapsToDevicePixels = true
			};
			waveformCanvas.Children.Add(scaleline);
			waveformCanvas.Children.Add(polymin);
			waveformCanvas.Children.Add(polymax);
			base.AllowDrop = true;
		}

		private void timer_Tick(object sender, EventArgs e)
		{
			UpdateProgress();
		}

		public void ZoomChange_EventHandler(double begin_t, double end_t, bool isCurrentlyMoving)
		{
			IsCurrentlyMoving = isCurrentlyMoving;
			SetZoomRange(begin_t, end_t, false);
		}

		public void Pair(WaveForm other)
		{
			event_zoomChanged = (Action<double, double, bool>)Delegate.Combine(event_zoomChanged, new Action<double, double, bool>(other.ZoomChange_EventHandler));
			other.event_zoomChanged = (Action<double, double, bool>)Delegate.Combine(other.event_zoomChanged, new Action<double, double, bool>(ZoomChange_EventHandler));
		}

		public virtual void SetZoomRange(double beginTime, double endTime, bool notifyOther = true)
		{
			if (beginTime < 0.0)
			{
				beginTime = 0.0;
			}
			if (endTime > data.durationSec)
			{
				endTime = data.durationSec;
			}
			zoomedBeginTime = beginTime;
			zoomedEndTime = endTime;
			if (notifyOther && event_zoomChanged != null)
			{
				event_zoomChanged(beginTime, endTime, IsCurrentlyMoving);
			}
		}

		private void Handler_DataChangeInvoker()
		{
#if NET40
			Dispatcher.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate
#else
			base.Dispatcher.Invoke(delegate
#endif
			{
				DataChanged();
			});
		}

		protected virtual void DataChanged()
		{
			viewBeginTime = 0.0;
			viewEndTime = data.durationSec;
			SetZoomRange(0.0, 10.0, false);
			IsCurrentlyMoving = false;
			isCurrentlyZooming = false;
			Update();
		}

		public void setVisibleRange(double begin, double end)
		{
			viewBeginTime = begin;
			viewEndTime = end;
		}

		protected double CalcTimeToXCoord(double t)
		{
			return (t - viewBeginTime) * pixelsPerSecond;
		}

		protected double CalcXCoordToTime(double x)
		{
			double num = x / pixelsPerSecond + viewBeginTime;
			if (num < 0.0)
			{
				num = 0.0;
			}
			else if (num > data.durationSec)
			{
				num = data.durationSec;
			}
			return num;
		}

		protected virtual void drawBeat(int beatIndex, bool cleanDraw = false)
		{
			if (beatIndex >= beats.Length - 1 || beatIndex % 2 != 0)
			{
				return;
			}
			Beat beat = beats.beats[beatIndex];
			Beat beat2 = beats.beats[beatIndex + 1];
			if (!(beat2.adjustedTime < viewBeginTime) || !(beat2.OrigTime < viewBeginTime))
			{
				double x = CalcTimeToXCoord(beat.adjustedTime);
				double x2 = CalcTimeToXCoord(beat2.adjustedTime);
				Polygon polygon = new Polygon
				{
					Fill = ((beatIndex % 4 == 0) ? brushBar : brushInt),
					SnapsToDevicePixels = true
				};
				polygon.Points.Add(new Point
				{
					X = x,
					Y = 0.0
				});
				polygon.Points.Add(new Point
				{
					X = x,
					Y = scaleCanvas.ActualHeight
				});
				polygon.Points.Add(new Point
				{
					X = x2,
					Y = scaleCanvas.ActualHeight
				});
				polygon.Points.Add(new Point
				{
					X = x2,
					Y = 0.0
				});
				scaleCanvas.Children.Add(polygon);
				if (beatIndex == 0)
				{
					double num3 = (firstBeatLine.X1 = (firstBeatLine.X2 = CalcTimeToXCoord(beat.adjustedTime + firstBeatMoveOffset)));
					firstBeatLine.Y1 = 0.0;
					firstBeatLine.Y2 = scaleCanvas.ActualHeight;
					scaleCanvas.Children.Remove(firstBeatLine);
					scaleCanvas.Children.Add(firstBeatLine);
				}
			}
		}

		public virtual void DrawScale()
		{
			if (data != null && scaleCanvas.ActualWidth != 0.0 && beats.Length != 0)
			{
				scaleCanvas.Children.Clear();
				for (int i = 0; i < beats.Length - 1 && (!(beats.beats[i].adjustedTime > viewEndTime) || !(beats.beats[i].OrigTime > viewEndTime)); i++)
				{
					drawBeat(i, true);
				}
			}
		}

		protected void DrawWaveform()
		{
			if (data.durationSec != 0.0)
			{
				int num = (int)waveformCanvas.ActualWidth;
				double num2 = (waveformCanvas.ActualHeight - yOffset - yMargin) / 2.0;
				if (waveMin == null || waveMin.Length != num)
				{
					waveMin = new float[num];
					waveMax = new float[num];
				}
				data.GetDatapoints(waveMin, waveMax, viewBeginTime, viewEndTime);
				PointCollection pointCollection = new PointCollection(num);
				PointCollection pointCollection2 = new PointCollection(num);
				double num3 = yOffset + yMargin / 2.0;
				Point value = default(Point);
				for (int i = 0; i < num; i++)
				{
					value.X = i;
					value.Y = num2 * (double)(1f + waveMax[i]) + num3;
					pointCollection2.Add(value);
					value.X = i;
					value.Y = num2 * (double)(1f + waveMin[i]) + num3;
					pointCollection.Add(value);
				}
				EdgeMode edgeMode = (IsFastRendering ? EdgeMode.Aliased : EdgeMode.Unspecified);
				polymin.SetValue(RenderOptions.EdgeModeProperty, edgeMode);
				polymax.SetValue(RenderOptions.EdgeModeProperty, edgeMode);
				polymin.Points = pointCollection2;
				polymax.Points = pointCollection;
				scaleline.X1 = 0.0;
				scaleline.Y1 = num2 + num3;
				scaleline.X2 = waveformCanvas.ActualWidth;
				scaleline.Y2 = num2 + num3;
			}
		}

		public virtual void Update()
		{
			if (waveformCanvas.ActualWidth == 0.0 || waveformCanvas.ActualHeight == 0.0)
			{
				return;
			}
			if (data == null)
			{
				progressCanvas.Children.Clear();
				polymin.Points.Clear();
				polymax.Points.Clear();
				scaleCanvas.Children.Clear();
				selectionCanvas.Children.Clear();
				return;
			}
			if (viewEndTime > data.durationSec)
			{
				viewEndTime = data.durationSec;
			}
			double num = viewEndTime - viewBeginTime;
			if (!(num < 1E-09))
			{
				pixelsPerSecond = scaleCanvas.ActualWidth / num;
				DrawScale();
				DrawWaveform();
				progressCanvas.Children.Clear();
				progressCanvas.Children.Add(progressLine);
				drawProgressIndicator();
			}
		}

		private void waveformCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			Update();
		}

		private void LazyRenderTimerTick(object sender, EventArgs e)
		{
			((DispatcherTimer)sender).Stop();
			isCurrentlyZooming = false;
			if (!IsFastRendering)
			{
				DrawWaveform();
			}
		}

		public void StartLazyRenderTimer()
		{
			isCurrentlyZooming = true;
			lazyRenderTimer.Start();
		}

		public void ZoomStep(bool zoomIn, int origo = -1)
		{
			double num = zoomedEndTime - zoomedBeginTime;
			if (zoomIn)
			{
				num *= Math.Sqrt(0.5);
				if (num * (double)data.sampleRate < waveformCanvas.ActualWidth)
				{
					num = waveformCanvas.ActualWidth / (double)data.sampleRate;
				}
			}
			else
			{
				num *= Math.Sqrt(2.0);
			}
			double num2 = waveformCanvas.ActualWidth / num;
			double num3 = ((origo < 0) ? (waveformCanvas.ActualWidth / 2.0) : ((double)origo));
			double num4 = CalcXCoordToTime(num3) - num3 / num2;
			double num5 = num4 + num;
			if (num5 > data.durationSec)
			{
				num5 = data.durationSec;
				num4 = num5 - num;
			}
			if (num4 < 0.0)
			{
				num4 = 0.0;
			}
			StartLazyRenderTimer();
			SetZoomRange(num4, num5);
			Mouse.OverrideCursor = Cursors.Arrow;
		}

		protected override void OnMouseWheel(MouseWheelEventArgs e)
		{
			int origo = (int)e.GetPosition(this).X;
			if (e.Delta > 0)
			{
				ZoomStep(true, origo);
			}
			else if (e.Delta < 0)
			{
				ZoomStep(false, origo);
			}
		}

		protected double getCurrentPlayerPos()
		{
			if (NAudioControl.Instance.dspStream == null)
			{
				return 0.0;
			}
			return NAudioControl.Instance.getCurrentRealtimeStreamPos() * NAudioControl.Instance.dspStream.TempoChange;
		}

		protected double getCurrentAdjustedBeatPlayerPos()
		{
			if (NAudioControl.Instance.dspStream == null)
			{
				return 0.0;
			}
			return NAudioControl.Instance.dspStream.CalcInputPos(NAudioControl.Instance.getCurrentRealtimeStreamPos());
		}

		protected virtual void drawProgressIndicator()
		{
			double currentAdjustedBeatPlayerPos = getCurrentAdjustedBeatPlayerPos();
			double x = CalcTimeToXCoord(currentAdjustedBeatPlayerPos);
			progressPointCollection.Clear();
			Point point = default(Point);
			point.X = x;
			point.Y = 0.0;
			Point value = point;
			point = default(Point);
			point.X = x;
			point.Y = progressCanvas.ActualHeight;
			Point value2 = point;
			progressPointCollection.Add(value);
			progressPointCollection.Add(value2);
			progressLine.Points = progressPointCollection;
		}

		protected void UpdateProgress()
		{
			if (progressCanvas == null)
			{
				return;
			}
			double num = getCurrentPlayerPos() - streamTimeBias;
			if (num < 0.0)
			{
				num = 0.0;
			}
			if (Math.Abs(num - progressPos) < 0.001)
			{
				return;
			}
			progressPos = num;
			if (isZoomFollowEnabled && !isProgressFrozen)
			{
				num = beats.getAdjustedTimeOfNearestOrigTime(num);
				if (num >= zoomedEndTime - 0.5 || num < zoomedBeginTime)
				{
					ReleaseMouseCapture();
					double num2 = zoomedEndTime - zoomedBeginTime;
					if (num2 < 6.0)
					{
						num2 = 6.0;
					}
					zoomedBeginTime = num - 1.5;
					if (zoomedBeginTime < 0.0)
					{
						zoomedBeginTime = 0.0;
					}
					zoomedEndTime = zoomedBeginTime + num2;
					if (zoomedEndTime > data.durationSec)
					{
						zoomedEndTime = data.durationSec;
						zoomedBeginTime = zoomedEndTime - num2;
					}
					isCurrentlyZooming = true;
					SetZoomRange(zoomedBeginTime, zoomedEndTime);
				}
			}
			drawProgressIndicator();
			if (event_PlayPosition != null)
			{
				event_PlayPosition(num);
			}
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (event_CursorPosition != null)
			{
				double obj = CalcXCoordToTime(e.GetPosition(this).X);
				event_CursorPosition(obj);
			}
		}
	}
}
