using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Beat_Editor.Model;
using Sample_NAudio;

namespace Beat_Editor
{
	public class WaveFormBeatEditor : WaveFormZoomable, UndoHost
	{
		private double leftMouseDownPosTime;

		private double rightMouseDownPosTime;

		protected Action<BeatCollection> event_ScaleChanged;

		public Action<double> event_WarpChanged;

		public Action<double, double> event_BpmFirstBeatChanged;

		private Line[] beatTickLines = new Line[0];

		private float[] beatPos;

		private float[] beatValues;

		private Beat progressBeat = new Beat(0.0);

		private bool isMultiEdit;

		private int currentBeatIndex;

		private double curBeatMinTime;

		private double curBeatMaxTime;

		private bool undoStored;

		private bool editing;

		private bool adjustingBpm;

		private bool panningBpm;

		private bool EditingAllowed
		{
			get
			{
				return pixelsPerSecond * 60.0 >= 15.0 * base.Bpm;
			}
		}

		internal bool IsDirtyEditing
		{
			get
			{
				return beats.Dirty;
			}
		}

		public WaveFormBeatEditor()
		{
			base.MouseLeftButtonDown += OnMouseLeftButtonDown;
			yOffset = 30.0;
		}

		public void AttachScaleChangeListener(Action<BeatCollection> h)
		{
			event_ScaleChanged = (Action<BeatCollection>)Delegate.Combine(event_ScaleChanged, h);
			h(beats);
		}

		private void InitBeatPolys()
		{
			int length = beats.Length;
			if (beatPolys.Length == length)
			{
				return;
			}
			beatPolys = new Polygon[length];
			beatTickLines = new Line[length];
			for (int i = 0; i < beatPolys.Length; i++)
			{
				Beat beat = beats.beats[i];
				beatTickLines[i] = new Line
				{
					Stroke = Brushes.Blue,
					StrokeThickness = 2.0,
					SnapsToDevicePixels = true
				};
				if (i % 2 == 0)
				{
					Polygon polygon = new Polygon
					{
						Fill = ((i % 4 == 0) ? WaveForm.brushBar : WaveForm.brushInt),
						SnapsToDevicePixels = true
					};
					beatPolys[i] = polygon;
					for (int j = 0; j < beat.points.Length; j++)
					{
						polygon.Points.Add(beat.points[j]);
						polygon.Points.Add(beat.points[j]);
					}
				}
			}
		}

		private double DetectFirstBeatPos()
		{
			if (beatValues != null && beatValues.Length != 0)
			{
				int num = Math.Min(15, beatValues.Length);
				int num2 = 0;
				double num3 = beatValues[0];
				int i;
				for (i = 0; (double)beatValues[i] < 0.05; i++)
				{
				}
				for (int j = 0; j < num; j++)
				{
					double num4 = (double)beatValues[i + j] / Math.Pow(j + 1, 0.30103);
					if (num4 > num3)
					{
						num3 = num4;
						num2 = j + i;
					}
				}
				return beatPos[num2];
			}
			return 0.0;
		}

		internal void SetBeatPosDetections(float[] beatPos, float[] beatValues)
		{
			if (beatPos != null && beatValues != null)
			{
				this.beatPos = (float[])beatPos.Clone();
				this.beatValues = (float[])beatValues.Clone();
				float num = 0f;
				double num2 = 0.0;
				for (int i = 0; i < beatValues.Length; i++)
				{
					num = ((this.beatValues[i] > num) ? this.beatValues[i] : num);
					num2 += (double)this.beatValues[i];
				}
				num2 /= (double)beatValues.Length;
				float num3 = 1f / num;
				int num4 = 0;
				for (int j = 0; j < beatValues.Length; j++)
				{
					if ((double)this.beatValues[j] > 0.01 * num2)
					{
						this.beatPos[num4] = this.beatPos[j];
						this.beatValues[num4] = this.beatValues[j] * num3;
						num4++;
					}
				}
				Array.Resize(ref this.beatValues, num4);
				Array.Resize(ref this.beatPos, num4);
				base.FirstBeatOffset = DetectFirstBeatPos();
				InitBeatTicks(base.FirstBeatOffset, true);
				double endTime = Math.Max(10.0, 1.5 * base.FirstBeatOffset);
				SetZoomRange(0.0, endTime);
				Update();
			}
			else
			{
				this.beatPos = null;
				this.beatValues = null;
			}
		}

		public void InitBeatTicks(double firstOffset = 0.0, bool resetBeats = false)
		{
			if (data != null && data.durationSec != 0.0)
			{
				beats.Init(data.durationSec, firstOffset, resetBeats);
				InitBeatPolys();
				if (!adjustingBpm && event_ScaleChanged != null)
				{
					event_ScaleChanged(beats);
				}
			}
			else
			{
				beats.Clear();
			}
		}

		private void calcBeatPoints(Beat b)
		{
			double num = CalcTimeToXCoord(b.OrigTime + firstBeatMoveOffset);
			double num2 = CalcTimeToXCoord(b.adjustedTime + firstBeatMoveOffset);
			if (num2 > waveformCanvas.ActualWidth)
			{
				num2 = waveformCanvas.ActualWidth;
			}
			else if (num2 < 0.0)
			{
				num2 = 0.0;
			}
			if (num > waveformCanvas.ActualWidth)
			{
				num = waveformCanvas.ActualWidth;
			}
			else if (num < 0.0)
			{
				num = 0.0;
			}
			double num3 = num - num2;
			num3 = ((num3 < -100.0) ? (-100.0) : ((num3 > 100.0) ? 100.0 : num3));
			double x = num - 0.04 * num3;
			double x2 = num - 0.15 * num3;
			double x3 = num2 + 0.15 * num3;
			double x4 = num2 + 0.04 * num3;
			b.points[0].X = num;
			b.points[0].Y = 0.0;
			b.points[1].X = num;
			b.points[1].Y = 0.0;
			b.points[2].X = x;
			b.points[2].Y = 20.0;
			b.points[3].X = x2;
			b.points[3].Y = 23.0;
			b.points[4].X = x3;
			b.points[4].Y = 27.0;
			b.points[5].X = x4;
			b.points[5].Y = 30.0;
			b.points[6].X = num2;
			b.points[6].Y = 33.0;
			b.points[7].X = num2;
			b.points[7].Y = scaleCanvas.ActualHeight;
		}

		public void DrawBeatDetections()
		{
			if (beats.Length == 0 || beatPos == null || !EditingAllowed)
			{
				return;
			}
			for (int i = 0; i < beatPos.Length; i++)
			{
				if (!((double)beatPos[i] < zoomedBeginTime))
				{
					if ((double)beatPos[i] > zoomedEndTime)
					{
						break;
					}
					double num = CalcTimeToXCoord(beatPos[i]);
					float num2 = ((beatValues[i] > 1f) ? 1f : beatValues[i]);
					Line element = new Line
					{
						Stroke = Brushes.White,
						StrokeThickness = 1.0,
						Opacity = num2,
						X1 = num,
						X2 = num,
						Y1 = 0.0,
						Y2 = scaleCanvas.ActualHeight,
						SnapsToDevicePixels = true
					};
					scaleCanvas.Children.Add(element);
				}
			}
		}

		private void DrawScaleMinSec()
		{
			if (data.durationSec < 1.0)
			{
				return;
			}
			if (pixelsPerSecond > 20.0)
			{
				int num = (int)Math.Ceiling(zoomedBeginTime);
				int num2 = (int)Math.Floor(zoomedEndTime);
				double actualHeight = scaleCanvas.ActualHeight;
				int num3 = ((!(pixelsPerSecond < 40.0)) ? 1 : 5);
				for (int i = num; i <= num2; i++)
				{
					double num4 = CalcTimeToXCoord(i);
					Line element = new Line
					{
						Stroke = Brushes.SteelBlue,
						StrokeThickness = ((i % 5 == 0) ? 2.0 : 1.0),
						X1 = num4,
						X2 = num4,
						Y1 = 0.0,
						Y2 = scaleCanvas.ActualHeight,
						SnapsToDevicePixels = true
					};
					scaleCanvas.Children.Add(element);
					if (i % num3 == 0)
					{
						int num5 = i;
						int num6 = num5 / 60;
						num5 -= 60 * num6;
						Label element2 = new Label
						{
							Foreground = Brushes.SteelBlue,
							Content = string.Format("{0}:{1:00}", num6, num5)
						};
						scaleCanvas.Children.Add(element2);
						Canvas.SetLeft(element2, num4);
						Canvas.SetTop(element2, scaleCanvas.ActualHeight - 20.0);
					}
				}
				return;
			}
			int num7 = (int)Math.Ceiling(zoomedBeginTime / 5.0);
			int num8 = (int)Math.Floor(zoomedEndTime / 5.0);
			double actualHeight2 = scaleCanvas.ActualHeight;
			int num9 = (int)(40.0 / pixelsPerSecond);
			int num10 = 1;
			while (num9 > 2)
			{
				num10 *= 2;
				num9 /= 2;
			}
			for (int j = num7; j <= num8; j++)
			{
				double num11 = CalcTimeToXCoord(5 * j);
				Line element3 = new Line
				{
					Stroke = Brushes.SteelBlue,
					StrokeThickness = ((j % 2 == 0) ? 2.0 : 1.0),
					X1 = num11,
					X2 = num11,
					Y1 = 0.0,
					Y2 = scaleCanvas.ActualHeight
				};
				scaleCanvas.Children.Add(element3);
				if (j % num10 == 0)
				{
					int num12 = 5 * j;
					int num13 = num12 / 60;
					num12 -= 60 * num13;
					Label element4 = new Label
					{
						Foreground = Brushes.SteelBlue,
						Content = string.Format("{0}:{1:00}", num13, num12)
					};
					scaleCanvas.Children.Add(element4);
					Canvas.SetLeft(element4, num11);
					Canvas.SetTop(element4, scaleCanvas.ActualHeight - 20.0);
				}
			}
		}

		public override void DrawScale()
		{
			base.DrawScale();
			DrawBeatDetections();
		}

		protected override void drawBeat(int beatIndex, bool cleanDraw = false)
		{
			if (beatIndex >= beats.Length - 1)
			{
				return;
			}
			Beat beat = beats.beats[beatIndex];
			Beat beat2 = beats.beats[beatIndex + 1];
			if (beat2.adjustedTime < viewBeginTime && beat2.OrigTime < viewBeginTime)
			{
				return;
			}
			Polygon polygon = beatPolys[beatIndex];
			if (polygon != null)
			{
				calcBeatPoints(beat);
				calcBeatPoints(beat2);
				int num = beat.points.Length;
				for (int i = 0; i < num; i++)
				{
					polygon.Points[i] = beat.points[i];
					polygon.Points[num + i] = beat2.points[num - i - 1];
				}
				scaleCanvas.Children.Insert(0, polygon);
			}
			if (beatIndex == 0)
			{
				double num4 = (firstBeatLine.X1 = (firstBeatLine.X2 = beat.points[0].X + 2.0));
				firstBeatLine.Y1 = 0.0;
				firstBeatLine.Y2 = scaleCanvas.ActualHeight;
				scaleCanvas.Children.Remove(firstBeatLine);
				scaleCanvas.Children.Add(firstBeatLine);
			}
			else if (EditingAllowed)
			{
				double num5 = CalcTimeToXCoord(beats.beats[beatIndex].OrigTime + firstBeatMoveOffset);
				Line line = beatTickLines[beatIndex];
				line.Stroke = (beats.Dirty ? Brushes.Blue : Brushes.LimeGreen);
				scaleCanvas.Children.Remove(line);
				if (num5 >= 0.0 && num5 <= scaleCanvas.ActualWidth)
				{
					line.X1 = num5;
					line.X2 = num5;
					line.Y1 = 0.0;
					line.Y2 = 20.0;
					scaleCanvas.Children.Add(line);
				}
			}
			if (cleanDraw)
			{
				DrawBeatNumber(beatIndex);
			}
		}

		private void DrawBeatNumber(int beatIndex)
		{
			if (60.0 * pixelsPerSecond / base.Bpm > 35.0)
			{
				double num = CalcTimeToXCoord(beats.beats[beatIndex].OrigTime + firstBeatMoveOffset);
				if (num >= 0.0 && num < scaleCanvas.ActualWidth - 30.0)
				{
					int num2 = beatIndex;
					int num3 = num2 / 4;
					num2 -= num3 * 4;
					TextBlock element = new TextBlock
					{
						Foreground = Brushes.LightGray,
						Text = string.Format("{0}.{1}", num3 + 1, num2 + 1),
						SnapsToDevicePixels = true
					};
					scaleCanvas.Children.Add(element);
					Canvas.SetLeft(element, num + 4.0);
				}
			}
		}

		protected override void drawProgressIndicator()
		{
			progressBeat.OrigTime = getCurrentPlayerPos();
			progressBeat.adjustedTime = getCurrentAdjustedBeatPlayerPos();
			calcBeatPoints(progressBeat);
			progressPointCollection.Clear();
			for (int i = 0; i < progressBeat.points.Length; i++)
			{
				progressPointCollection.Add(progressBeat.points[i]);
			}
			progressLine.Points = progressPointCollection;
		}

		public void MultiEditMode(bool on)
		{
			isMultiEdit = on;
		}

		public void setBpm(double value)
		{
			if (beats.bpm != value)
			{
				if (beats.Length > 0)
				{
					StoreUndoPoint();
				}
				beats.bpm = value;
				InitBeatTicks(base.FirstBeatOffset, !beats.Dirty);
				DrawScale();
			}
		}

		public void PanStep(int panNumBeats)
		{
			double beginZoomTime = zoomedBeginTime + (double)panNumBeats * 60.0 / base.Bpm;
			MoveZoom(beginZoomTime);
		}

		internal void MoveZoom(double beginZoomTime)
		{
			double num = zoomedEndTime - zoomedBeginTime;
			double num2 = ((beginZoomTime < 0.0) ? 0.0 : beginZoomTime);
			double num3 = num2 + num;
			if (num3 > data.durationSec)
			{
				num3 = data.durationSec;
				num2 = num3 - num;
			}
			SetZoomRange(num2, num3);
		}

		public override void SetZoomRange(double beginTime, double endTime, bool notifyOther = true)
		{
			currentBeatIndex = -1;
			base.SetZoomRange(beginTime, endTime, notifyOther);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (beats.Length == 0)
			{
				return;
			}
			Point position = e.GetPosition(this);
			double num = CalcXCoordToTime(position.X);
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				if (currentBeatIndex < 0)
				{
					return;
				}
				if (!undoStored)
				{
					undoStored = true;
					StoreUndoPoint();
				}
				if (adjustingBpm)
				{
					if (currentBeatIndex > 0)
					{
						beats.bpm = Math.Round((double)currentBeatIndex * 60.0 / (num - base.FirstBeatOffset), 2);
						InitBeatTicks(base.FirstBeatOffset, !beats.Dirty);
						DrawScale();
						if (event_BpmFirstBeatChanged != null)
						{
							event_BpmFirstBeatChanged(base.Bpm, base.FirstBeatOffset);
						}
					}
				}
				else if (editing)
				{
					if (currentBeatIndex > 0)
					{
						Beat beat = beats.beats[currentBeatIndex];
						beat.adjustedTime = ((num < curBeatMinTime) ? curBeatMinTime : ((num > curBeatMaxTime) ? curBeatMaxTime : num));
						if (beat.adjustedTime != beat.OrigTime && !beats.Dirty)
						{
							beats.Dirty = true;
							DrawScale();
						}
						if (currentBeatIndex > 0)
						{
							scaleCanvas.Children.Remove(beatPolys[currentBeatIndex - 1]);
							drawBeat(currentBeatIndex - 1);
						}
						scaleCanvas.Children.Remove(beatPolys[currentBeatIndex]);
						if (isMultiEdit)
						{
							double moveDelta = beat.MoveDelta;
							for (int i = currentBeatIndex + 1; i < beats.Length; i++)
							{
								beats.beats[i].MoveDelta = moveDelta;
							}
							DrawScale();
						}
						else
						{
							scaleCanvas.Children.Remove(beatPolys[currentBeatIndex]);
							drawBeat(currentBeatIndex);
						}
					}
					else if (currentBeatIndex == 0)
					{
						firstBeatMoveOffset = num - beats.beats[0].adjustedTime;
						if (!isMultiEdit)
						{
							for (int j = 1; j < beats.Length; j++)
							{
								beats.beats[j].MoveDelta = 0.0 - firstBeatMoveOffset;
							}
						}
						DrawScale();
					}
				}
			}
			else if (e.RightButton == MouseButtonState.Pressed)
			{
				if (panningBpm)
				{
					if (!undoStored)
					{
						undoStored = true;
						StoreUndoPoint();
					}
					firstBeatMoveOffset = num - rightMouseDownPosTime;
					DrawScale();
					return;
				}
			}
			else
			{
				bool flag = false;
				currentBeatIndex = -1;
				double num2 = 10.0 / pixelsPerSecond;
				if (!EditingAllowed)
				{
					if (Math.Abs(base.FirstBeatOffset - num) < num2)
					{
						currentBeatIndex = 0;
						flag = true;
					}
				}
				else
				{
					if (position.Y < 26.0)
					{
						double num3 = num - base.FirstBeatOffset;
						double num4 = 60.0 / base.Bpm;
						currentBeatIndex = (int)Math.Round(num3 / num4);
						flag = Math.Abs(num3 - (double)currentBeatIndex * num4) < num2;
					}
					else
					{
						currentBeatIndex = beats.FindNearestBeat(num, num2);
						flag = currentBeatIndex >= 0;
					}
					if (currentBeatIndex >= beats.Length)
					{
						currentBeatIndex = beats.Length - 1;
					}
				}
				Mouse.OverrideCursor = (flag ? Cursors.SizeWE : Cursors.Arrow);
			}
			if (event_WarpChanged != null)
			{
				double obj = 0.0;
				if (currentBeatIndex >= 0 && currentBeatIndex < beats.beats.Length)
				{
					Beat beat2 = beats.beats[currentBeatIndex];
					obj = beat2.adjustedTime - beat2.OrigTime;
				}
				event_WarpChanged(obj);
			}
			base.OnMouseMove(e);
		}

		public void SetClean()
		{
			beats.Dirty = false;
		}

		protected override void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (beats.Length != 0)
			{
				Point position = e.GetPosition(this);
				rightMouseDownPosTime = CalcXCoordToTime(position.X);
				if (position.Y < 26.0)
				{
					panningBpm = true;
					CaptureMouse();
				}
				else
				{
					base.OnMouseRightButtonDown(sender, e);
				}
			}
		}

		protected void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (beats.Length == 0)
			{
				return;
			}
			Point position = e.GetPosition(this);
			if (currentBeatIndex >= 0)
			{
				leftMouseDownPosTime = CalcXCoordToTime(position.X);
				if (position.Y < 26.0 && currentBeatIndex > 0)
				{
					adjustingBpm = true;
				}
				else if (currentBeatIndex >= 0)
				{
					double num = 15.0 / base.Bpm;
					curBeatMinTime = ((currentBeatIndex > 0) ? (beats.beats[currentBeatIndex - 1].adjustedTime + num) : 0.0);
					curBeatMaxTime = ((currentBeatIndex < beats.Length - 1) ? (beats.beats[currentBeatIndex + 1].adjustedTime - num) : data.durationSec);
					editing = true;
				}
				base.FreezeProgress = true;
				CaptureMouse();
			}
		}

		protected override void OnMouseLeave(MouseEventArgs e)
		{
			Mouse.OverrideCursor = Cursors.Arrow;
			editing = false;
			base.OnMouseLeave(e);
		}

		protected override void OnMouseUp(MouseButtonEventArgs e)
		{
			if (beats.Length == 0)
			{
				return;
			}
			if (undoStored)
			{
				if (isMultiEdit)
				{
					beats.FixMoved();
				}
				if (firstBeatMoveOffset != 0.0)
				{
					beats.MoveAllOffset(firstBeatMoveOffset);
					firstBeatMoveOffset = 0.0;
					DrawScale();
					if (event_BpmFirstBeatChanged != null)
					{
						event_BpmFirstBeatChanged(base.Bpm, base.FirstBeatOffset);
					}
				}
				if (currentBeatIndex >= 0)
				{
					if (event_ScaleChanged != null)
					{
						event_ScaleChanged(beats);
					}
					currentBeatIndex = -1;
				}
				beats.setupAudioModification(NAudioControl.Instance.dspStream);
				undoStored = false;
			}
			adjustingBpm = false;
			panningBpm = false;
			if (e.LeftButton == MouseButtonState.Released)
			{
				editing = false;
			}
			base.OnMouseUp(e);
		}

		public void StoreUndoPoint()
		{
			UndoableBeatCollection change = new UndoableBeatCollection(this, beats);
			UndoHistory.Instance.Add(change);
		}

		public IUndoable ReCommit(IUndoable change)
		{
			UndoableBeatCollection result = new UndoableBeatCollection(this, beats);
			UndoableBeatCollection other = (UndoableBeatCollection)change;
			beats = new BeatCollection(other);
			InitBeatPolys();
			DrawScale();
			event_BpmFirstBeatChanged(base.Bpm, base.FirstBeatOffset);
			return result;
		}
	}
}
