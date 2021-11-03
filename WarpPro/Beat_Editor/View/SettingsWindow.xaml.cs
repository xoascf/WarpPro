using System;
using System.CodeDom.Compiler;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using Sample_NAudio;

namespace Beat_Editor.View
{
	public partial class SettingsWindow : Window, IComponentConnector
	{
		public static readonly Brush HoverOnButtronBrush = new SolidColorBrush(Color.FromArgb(96, 45, 115, 200));

		public SettingsWindow()
		{
			InitializeComponent();
			HoverOnButtronBrush.Freeze();
			string[] deviceNames = NAudioControl.DeviceNames;
			ObservableCollection<string> observableCollection = new ObservableCollection<string>();
			int selectedIndex = 0;
			string outDeviceName = NAudioControl.OutDeviceName;
			for (int i = 0; i < deviceNames.Length; i++)
			{
				observableCollection.Add(deviceNames[i]);
				if (deviceNames[i] == outDeviceName)
				{
					selectedIndex = i;
				}
			}
			comboBox_Devices.ItemsSource = observableCollection;
			comboBox_Devices.SelectedIndex = selectedIndex;
		}

		private void Close_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			Close();
		}

		private void DockPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			DragMove();
		}

		private void TextBlock_MouseEnter(object sender, MouseEventArgs e)
		{
			((TextBlock)sender).Background = HoverOnButtronBrush;
		}

		private void TextBlock_MouseLeave(object sender, MouseEventArgs e)
		{
			((TextBlock)sender).Background = base.Background;
		}

		private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (e.GetPosition(this).Y < 25.0)
			{
				DragMove();
			}
		}

		private void buttonOk_Click(object sender, RoutedEventArgs e)
		{
			NAudioControl.OutDeviceName = comboBox_Devices.Text;
			Close();
		}

		private void buttonCancel_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
