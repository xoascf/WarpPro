using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Beat_Editor.Controller;
using Beat_Editor.View;
using Microsoft.Win32;
using Mp3Lib;
using Sample_NAudio;

namespace Beat_Editor
{
	public partial class MainWindow : Window, IComponentConnector
	{
		private enum MaximizeMode
		{
			Toggle,
			Normal,
			Maximize
		}

		private WaveformDataProvider waveData;

		public static readonly Brush SelectedToggleButtonBrush = new SolidColorBrush(Color.FromArgb(128, 45, 115, 200));

		public static readonly Brush HoverOnButtronBrush = new SolidColorBrush(Color.FromArgb(96, 45, 115, 200));

		private string curFilePath = "";

		private bool isFileOpenReady;

		private bool isFileCurrentlyOpening;

		private double prevWarp;

		private BitmapImage playImage = new BitmapImage(new Uri("/images/play.png", UriKind.Relative));

		private BitmapImage pauseImage = new BitmapImage(new Uri("/images/pause.png", UriKind.Relative));

		private string decimalSeparator = Thread.CurrentThread.CurrentCulture.NumberFormat.CurrencyDecimalSeparator;

		private double _BPMPrevValue;

		private double _BPMCurValue;

		private double _targetBPMvalue;

		private bool isMultiEditEnabled;

		private bool isProcessingEnabled;

		private bool isViewFiltering;

		private bool isMetronomeEnabled;

		private bool isZoomFollow;

		private List<double> tapticks = new List<double>();

		private double TargetBPM
		{
			get
			{
				return _targetBPMvalue;
			}
			set
			{
				bool flag = Math.Abs(_BPMPrevValue - _targetBPMvalue) < 0.01;
				value = ((value > 300.0) ? 300.0 : ((value < 10.0) ? 10.0 : value));
				_targetBPMvalue = value;
				editTargetBPM.Text = Math.Round(value, 1).ToString();
				if (NAudioControl.Instance.inputStream != null)
				{
					double position = NAudioControl.Instance.Position;
					waveform.beats.targetBPM = value;
					UpdateMetronomeRate();
					SetupAudioModification();
					bool flag2 = Math.Abs(_BPMCurValue - value) < 0.01;
					if (!flag || !flag2)
					{
						NAudioControl.Instance.Position = position;
					}
				}
			}
		}

		private double BPM
		{
			get
			{
				return _BPMCurValue;
			}
			set
			{
				value = ((value > 300.0) ? 300.0 : ((value < 10.0) ? 10.0 : value));
				value = Math.Round(value, 3);
				_BPMCurValue = value;
				editBPM.Text = value.ToString();
				waveform.setBpm(value);
				TargetBPM = value;
				_BPMPrevValue = value;
			}
		}

		private bool VerifyLicenseIssue()
		{
			if (LicenseController.Instance.IsLicenseIssueValid)
			{
				return true;
			}
			if (MessageBox.Show("Need to connect Internet server to refresh license information. \r\nPlease enable the Internet connection and click Ok to retry", "WarpPro needs Internet connection!", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation) == MessageBoxResult.Cancel && MessageBox.Show("Do you want to Exit the application?", "Can't connect to Internet", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
			{
				Environment.Exit(0);
			}
			return false;
		}

		private void handler_LicenseSyncCompleted(bool success)
		{
#if NET40
			Dispatcher.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate
#else
			base.Dispatcher.Invoke(delegate
#endif
			{
				if (success)
				{
					RefreshPurchaseVisible();
				}
				if (!VerifyLicenseIssue())
				{
					LicenseController.Instance.SyncLicenseAsync();
				}
				else
				{
					ShowDialogIfTrialExpired();
				}
			});
		}

		public MainWindow()
		{
			InitializeComponent();
			LicenseController.Instance.event_LicenseSyncCompleted += handler_LicenseSyncCompleted;
			LicenseController.Instance.SyncLicenseAsync();
			SelectedToggleButtonBrush.Freeze();
			HoverOnButtronBrush.Freeze();
			base.MinHeight = 500.0;
			base.MinWidth = 800.0;
			base.MaxHeight = SystemParameters.WorkArea.Height + 10.0;
			base.MaxWidth = SystemParameters.WorkArea.Width + 10.0;
			NAudioControl instance = NAudioControl.Instance;
			editTargetBPM.IsUndoEnabled = false;
			editBPM.IsUndoEnabled = false;
			waveform.Pair(waveformOV);
			WaveFormBeatEditor waveFormBeatEditor = waveform;
			waveFormBeatEditor.event_CursorPosition = (Action<double>)Delegate.Combine(waveFormBeatEditor.event_CursorPosition, new Action<double>(EventHandler_CursorPos));
			WaveFormOverview waveFormOverview = waveformOV;
			waveFormOverview.event_CursorPosition = (Action<double>)Delegate.Combine(waveFormOverview.event_CursorPosition, new Action<double>(EventHandler_CursorPos));
			waveFormBeatEditor = waveform;
			waveFormBeatEditor.event_PlayPosition = (Action<double>)Delegate.Combine(waveFormBeatEditor.event_PlayPosition, new Action<double>(EventHandler_PlayPos));
			waveFormBeatEditor = waveform;
			waveFormBeatEditor.event_WarpChanged = (Action<double>)Delegate.Combine(waveFormBeatEditor.event_WarpChanged, new Action<double>(EventHandker_WarpChanged));
			waveFormBeatEditor = waveform;
			waveFormBeatEditor.event_BpmFirstBeatChanged = (Action<double, double>)Delegate.Combine(waveFormBeatEditor.event_BpmFirstBeatChanged, new Action<double, double>(EventHandler_BpmFirstBeatChanged));
			waveform.AttachScaleChangeListener(waveformOV.ScaleChangeListener);
			waveFormBeatEditor = waveform;
			waveFormBeatEditor.event_zoomChanged = (Action<double, double, bool>)Delegate.Combine(waveFormBeatEditor.event_zoomChanged, new Action<double, double, bool>(EventHandler_ZoomChanged));
			waveFormOverview = waveformOV;
			waveFormOverview.event_zoomChanged = (Action<double, double, bool>)Delegate.Combine(waveFormOverview.event_zoomChanged, new Action<double, double, bool>(EventHandler_ZoomChanged));
			instance.PlaybackStoppedAction += playbackStopped_action;
			RefreshPurchaseVisible();
			SetMultiEditMode(true);
			SetProcessEnabled(true);
			SetZoomFollowEnabled(true);
		}

		private void RefreshPurchaseVisible()
		{
			bool purchaseVisible = ((LicenseController.Instance.LicenseLevel == 0) ? true : false);
			SetPurchaseVisible(purchaseVisible);
		}

		private void SetPurchaseVisible(bool visible)
		{
			Visibility visibility = ((!visible) ? Visibility.Hidden : Visibility.Visible);
			purchaseButton.Visibility = visibility;
			purchaseSeparator.Visibility = visibility;
			if (!visible)
			{
				menuItem_Info.Items.Remove(menuItem_Purchase);
			}
		}

		private void EventHandler_BpmFirstBeatChanged(double bpm, double firstBeat)
		{
			BPM = bpm;
			NAudioControl.Instance.metronome.FirstBeatPos = firstBeat;
		}

		public void EventHandler_ZoomChanged(double begin_t, double end_t, bool notifyOther)
		{
			if (viewText != null)
			{
				int num = (int)(begin_t / 60.0);
				begin_t -= 60.0 * (double)num;
				int num2 = (int)(end_t / 60.0);
				end_t -= 60.0 * (double)num2;
				viewText.Text = string.Format("view: {0}:{1:00.00} - {2}:{3:00.00}s", num, begin_t, num2, end_t);
			}
		}

		public void EventHandler_CursorPos(double time)
		{
			if (posText != null)
			{
				int num = (int)(time / 60.0);
				double num2 = time - (double)(60 * num);
				posText.Text = string.Format("pos: {0}:{1:00.000}s", num, num2);
			}
		}

		public void EventHandker_WarpChanged(double warpsec)
		{
			if (warpText != null && prevWarp != warpsec)
			{
				prevWarp = warpsec;
				warpText.Text = string.Format("warp: {0:0.000}s", warpsec);
			}
		}

		public void EventHandler_PlayPos(double time)
		{
			if (playPosText != null)
			{
				int num = (int)(time / 60.0);
				double num2 = time - (double)(60 * num);
				playPosText.Text = string.Format("play:{0}:{1:00.000}s", num, num2);
			}
		}

		private void FileOpenReadyHandler(OpenProgress sender, double bpm)
		{
			sender.BPMReadyHandler = (Action<OpenProgress, double>)Delegate.Remove(sender.BPMReadyHandler, new Action<OpenProgress, double>(FileOpenReadyHandler));
			isFileCurrentlyOpening = false;
			BPM = bpm;
			waveform.SetBeatPosDetections(sender.beatPos, sender.beatValues);
			UndoHistory.Instance.Clear();
			isFileOpenReady = true;
			ShowDialogIfTrialExpired();
		}

		private void ShowDialogIfTrialExpired()
		{
			if (LicenseController.Instance.IsTrialExpired)
			{
				TrialExpiredWindow trialExpiredWindow = new TrialExpiredWindow(this);
				trialExpiredWindow.Owner = this;
				trialExpiredWindow.Show();
			}
		}

		private void LoadWaveformData(string filename)
		{
			if (isViewFiltering)
			{
				if (waveData == null)
				{
					waveData = new WaveformDataProviderLPF();
				}
			}
			else if (waveData == null)
			{
				waveData = new WaveformDataProvider();
			}
			WaveformDataProvider waveformDataProvider = waveData;
			waveform.setDataProvider(waveformDataProvider);
			waveformOV.setDataProvider(waveformDataProvider);
			if (!waveformDataProvider.loaded && filename.Length > 0)
			{
				isFileCurrentlyOpening = true;
				OpenProgress openProgress;
				OpenProgress openProgress2 = (openProgress = new OpenProgress(this));
				openProgress.BPMReadyHandler = (Action<OpenProgress, double>)Delegate.Combine(openProgress.BPMReadyHandler, new Action<OpenProgress, double>(FileOpenReadyHandler));
				openProgress2.Load(waveformDataProvider, filename);
			}
		}

		private void readFileTags(string mp3FileName)
		{
			string text = "WarpPro";
			try
			{
				string title = new Mp3File(mp3FileName).TagHandler.Title;
				if (title.Length > 0)
				{
					text = text + " - " + title;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("exp readFileTags: " + ex.Message);
			}
			base.Title = text;
		}

		private void OpenFile(string filepath)
		{
			try
			{
				if (CloseFile())
				{
					isFileOpenReady = false;
					NAudioControl.Instance.OpenFile(filepath);
					if (NAudioControl.Instance.CanPlay)
					{
						readFileTags(filepath);
						curFilePath = filepath;
						fileText.Text = Path.GetFileName(filepath);
						setPlayButtonMode(true);
						LoadWaveformData(filepath);
					}
					else
					{
						MessageBox.Show(string.Format("Can't open audio file {0}: Unsupported format?", Path.GetFileName(filepath)));
					}
				}
			}
			catch (Exception value)
			{
				Console.WriteLine(value);
			}
		}

		private void open_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Audio files (*.mp3; *.wav)|*.mp3;*.wav";
			if (openFileDialog.ShowDialog() == true)
			{
				OpenFile(openFileDialog.FileName);
			}
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			if (!CloseFile())
			{
				e.Cancel = true;
			}
		}

		private void close_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			NAudioControl.Instance.Dispose();
		}

		private void setPlayButtonMode(bool isPlay)
		{
			if (isPlay)
			{
				playText.Text = "Play";
				playIcon.Source = playImage;
			}
			else
			{
				playText.Text = "Pause";
				playIcon.Source = pauseImage;
			}
		}

		private void UpdateMetronomeRate()
		{
			if (NAudioControl.Instance.metronome != null)
			{
				NAudioControl.Instance.metronome.setBpm(_targetBPMvalue);
			}
		}

		public void SetupAudioModification()
		{
			waveform.beats.setupAudioModification(NAudioControl.Instance.dspStream);
		}

		private void Play(bool beginZoomed = false)
		{
			if (waveform != null && isFileOpenReady && (NAudioControl.Instance.CanPlay || NAudioControl.Instance.CanStop))
			{
				setPlayButtonMode(false);
				if (NAudioControl.Instance.IsPlaying)
				{
					NAudioControl.Instance.Stop();
				}
				double bPM = BPM;
				double targetBPM = TargetBPM;
				waveform.ZoomFollowPlayback = isZoomFollow;
				MetronomeStream metronome = NAudioControl.Instance.metronome;
				metronome.Enable = isMetronomeEnabled;
				metronome.FirstBeatPos = waveform.FirstBeatOffset / NAudioControl.Instance.dspStream.TempoChange;
				UpdateMetronomeRate();
				if (beginZoomed)
				{
					NAudioControl.Instance.Position = waveformOV.beats.getOrigTimeOfNearestAdjustedTime(waveformOV.zoomedBeginTime);
				}
				else
				{
					NAudioControl.Instance.Position = NAudioControl.Instance.dspStream.CurrentTime.TotalSeconds;
				}
				SetupAudioModification();
				NAudioControl.Instance.Play();
			}
		}

		private void play_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (playText.Text == "Play")
			{
				if (NAudioControl.Instance.CanPlay)
				{
					Play(true);
				}
			}
			else if (NAudioControl.Instance.CanPause)
			{
				NAudioControl.Instance.Pause();
				setPlayButtonMode(true);
				waveform.StartLazyRenderTimer();
			}
		}

		private void playbackStopped_action()
		{
#if NET40
			Dispatcher.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate
#else
			base.Dispatcher.Invoke(delegate
#endif
			{
				setPlayButtonMode(true);
				waveform.StartLazyRenderTimer();
			});
		}

		private void SetMultiEditMode(bool multiEdit)
		{
			isMultiEditEnabled = multiEdit;
			multiButton.Background = (multiEdit ? SelectedToggleButtonBrush : toolBar.Background);
			waveform.MultiEditMode(multiEdit);
		}

		private void SetProcessEnabled(bool enabled)
		{
			isProcessingEnabled = enabled;
			if (NAudioControl.Instance.dspStream != null)
			{
				NAudioControl.Instance.dspStream.ProcessingEnabled = isProcessingEnabled;
			}
			NAudioControl.Instance.Stop();
		}

		private void SetMetronomeEnabled(bool enabled)
		{
			isMetronomeEnabled = enabled;
			metronomeButton.Background = (enabled ? SelectedToggleButtonBrush : toolBar.Background);
			if (NAudioControl.Instance.metronome != null)
			{
				NAudioControl.Instance.metronome.Enable = enabled;
			}
		}

		private void SetZoomFollowEnabled(bool enable)
		{
			isZoomFollow = enable;
			waveform.ZoomFollowPlayback = isZoomFollow;
			if (!isZoomFollow)
			{
				waveform.StartLazyRenderTimer();
			}
		}

		private void multiButton_Click(object sender, RoutedEventArgs e)
		{
			SetMultiEditMode(!isMultiEditEnabled);
		}

		private void processButton_Click(object sender, RoutedEventArgs e)
		{
			SetProcessEnabled(!isProcessingEnabled);
		}

		private void stop_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (isFileOpenReady)
			{
				NAudioControl.Instance.Stop();
				NAudioControl.Instance.dspStream.Position = 0L;
				waveform.StartLazyRenderTimer();
			}
		}

		private void metronomeButton_Click(object sender, RoutedEventArgs e)
		{
			SetMetronomeEnabled(!isMetronomeEnabled);
		}

		private void saveCmd_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (!isFileOpenReady)
			{
				return;
			}
			try
			{
				if (LicenseController.Instance.IsTrialExpired)
				{
					ShowDialogIfTrialExpired();
				}
				else
				{
					new SaveDialog(this, curFilePath, waveform.beats).Show();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("saveCmd_Executed exp:" + ex);
			}
		}

		private void NewTap(double msec)
		{
			if (!isFileOpenReady)
			{
				return;
			}
			tapticks.Add(msec);
			while (msec - tapticks[0] > 10000.0)
			{
				tapticks.RemoveAt(0);
			}
			double[] array = tapticks.ToArray();
			if (array.Length < 2)
			{
				return;
			}
			double num = 0.0;
			int num2 = array.Length - 1;
			int num3 = 1;
			while (true)
			{
				if (num3 < array.Length)
				{
					double num4 = array[num3] - array[num3 - 1];
					if (num4 > 2000.0)
					{
						num2 = array.Length - num3 - 1;
						if (num2 < 1)
						{
							break;
						}
						num = 0.0;
					}
					else
					{
						num += num4;
					}
					num3++;
					continue;
				}
				double num5 = Math.Round(60000.0 * (double)num2 / num, 2);
				double position = NAudioControl.Instance.Position;
				double num6 = 60.0 / num5;
				int num7 = (int)(position / num6);
				double num8 = position - (double)num7 * num6;
				int num9 = (int)((waveform.FirstBeatOffset - num8) / num6 + 0.5);
				if (num9 < 0)
				{
					num9 = 0;
				}
				waveform.FirstBeatOffset = num8 + num6 * (double)num9 - 0.1;
				BPM = num5;
				break;
			}
		}

		private void buttonTap_Click(object sender, RoutedEventArgs e)
		{
			double msec = (double)DateTime.Now.Ticks / 10000.0;
			NewTap(msec);
		}

		private void resetButton_Click(object sender, RoutedEventArgs e)
		{
			if (isFileOpenReady)
			{
				waveform.StoreUndoPoint();
				waveform.InitBeatTicks(waveform.FirstBeatOffset, true);
				SetupAudioModification();
				waveform.Update();
			}
		}

		private void undoCmd_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			UndoHistory.Instance.UndoLast();
		}

		private void redoCmd_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			UndoHistory.Instance.RedoLast();
		}

		private void Window_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
			case Key.T:
				buttonTap.Focus();
				break;
			case Key.Prior:
				waveform.PanStep(6);
				break;
			case Key.Next:
				waveform.PanStep(-6);
				break;
			case Key.End:
				waveform.MoveZoom(waveform.data.durationSec);
				break;
			case Key.Home:
				waveform.MoveZoom(0.0);
				break;
			case Key.Left:
				waveform.PanStep(-1);
				break;
			case Key.Right:
				waveform.PanStep(1);
				break;
			case Key.OemMinus:
				waveform.ZoomStep(false, 0);
				break;
			case Key.OemPlus:
				waveform.ZoomStep(true, 0);
				break;
			}
		}

		private void buttonX2_Click(object sender, RoutedEventArgs e)
		{
			if (BPM < 145.0)
			{
				BPM *= 2.0;
			}
		}

		private void buttonX2_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (BPM > 50.0)
			{
				BPM *= 0.5;
			}
		}

		private void Window_Drop(object sender, DragEventArgs e)
		{
			if (!isFileCurrentlyOpening)
			{
				string[] array = (string[])e.Data.GetData(DataFormats.FileDrop);
				if (array.Length >= 1)
				{
					OpenFile(array[0]);
				}
			}
		}

		private void Window_DragEnter(object sender, DragEventArgs e)
		{
			e.Effects = DragDropEffects.Copy;
		}

		private void editBPM_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Return)
			{
				editBPM_LostFocus(sender, null);
			}
		}

		private void editTargetBPM_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Return)
			{
				editTargetBPM_LostFocus(sender, null);
			}
		}

		private void editBPM_LostFocus(object sender, RoutedEventArgs e)
		{
			double result;
			if (!double.TryParse(editBPM.Text.Replace(".", decimalSeparator).Replace(",", decimalSeparator), out result))
			{
				result = BPM;
			}
			BPM = result;
		}

		private void editTargetBPM_LostFocus(object sender, RoutedEventArgs e)
		{
			double result;
			if (!double.TryParse(editTargetBPM.Text.Replace(".", decimalSeparator).Replace(",", decimalSeparator), out result))
			{
				result = TargetBPM;
			}
			TargetBPM = result;
		}

		internal void OpenPurchaseLink()
		{
			PurchaseWindow purchaseWindow = new PurchaseWindow();
			purchaseWindow.Owner = this;
			purchaseWindow.Show();
		}

		private void purchaseButton_Click(object sender, RoutedEventArgs e)
		{
			OpenPurchaseLink();
		}

		private void helpCmd_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			Process.Start("https://www.warppro.com/doc");
		}

		private void MenuItem_ClickPurchase(object sender, RoutedEventArgs e)
		{
			OpenPurchaseLink();
		}

		private void MenuItem_ClickAbout(object sender, RoutedEventArgs e)
		{
			About about = new About();
			about.Owner = Window.GetWindow(this);
			about.Show();
		}

		private void closeCmd_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			Close();
		}

		private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			try
			{
				if (e.GetPosition(this).Y < 30.0)
				{
					DragMove();
				}
			}
			catch
			{
			}
		}

		private void Close_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			Close();
		}

		private void Maximize_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			Maximize(MaximizeMode.Toggle);
		}

		private void Minimize_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			base.WindowState = WindowState.Minimized;
		}

		private void Maximize(MaximizeMode mode)
		{
			bool flag;
			switch (mode)
			{
			default:
				flag = false;
				break;
			case MaximizeMode.Maximize:
				flag = true;
				break;
			case MaximizeMode.Toggle:
				flag = base.WindowState != WindowState.Maximized;
				break;
			}
			if (flag)
			{
				base.MaxWidth = SystemParameters.WorkArea.Width + 10.0;
				base.MaxHeight = SystemParameters.WorkArea.Height + 10.0;
				base.WindowState = WindowState.Maximized;
			}
			else
			{
				base.WindowState = WindowState.Normal;
			}
		}

		private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			Point position = e.GetPosition(this);
			if (e.LeftButton == MouseButtonState.Pressed && position.Y < 30.0 && position.Y > 0.0 && position.X > 0.0)
			{
				Maximize(MaximizeMode.Toggle);
			}
		}

		private void TextBlock_MouseEnter(object sender, MouseEventArgs e)
		{
			((TextBlock)sender).Background = HoverOnButtronBrush;
		}

		private void TextBlock_MouseLeave(object sender, MouseEventArgs e)
		{
			((TextBlock)sender).Background = base.Background;
		}

		private void MenuSettings_Click(object sender, RoutedEventArgs e)
		{
			SettingsWindow settingsWindow = new SettingsWindow();
			settingsWindow.Owner = this;
			settingsWindow.Show();
		}

		private void menuItem_Feedback_Click(object sender, RoutedEventArgs e)
		{
			Process.Start(string.Format("https://warppro.com/send-feedback/?id={0}", LicenseController.LocalLicenseId));
		}

		private bool CloseFile()
		{
			if (waveform.IsDirtyEditing && MessageBox.Show("You have unsaved changes. Are you sure you want to proceed?", "Confirm Closing " + Path.GetFileName(curFilePath), MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.Cancel)
			{
				return false;
			}
			NAudioControl.Instance.Close();
			waveform.setDataProvider(null);
			waveformOV.setDataProvider(null);
			waveform.InitBeatTicks();
			waveform.SetBeatPosDetections(null, null);
			waveData = null;
			BPM = 60.0;
			UndoHistory.Instance.Clear();
			isFileOpenReady = false;
			isFileCurrentlyOpening = false;
			return true;
		}

		private void MenuItem_CloseFile_Click(object sender, RoutedEventArgs e)
		{
			CloseFile();
		}
	}
}
