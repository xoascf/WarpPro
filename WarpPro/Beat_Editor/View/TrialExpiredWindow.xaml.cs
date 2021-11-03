using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Beat_Editor.View
{
	public partial class TrialExpiredWindow : Window, IComponentConnector
	{
		private MainWindow mainwnd;

		public TrialExpiredWindow(MainWindow mw)
		{
			mainwnd = mw;
			InitializeComponent();
			textBox_note.Text = "Oh no, the free Trial period has expired\n\nPlease purchase a license to unlock full feature set\n\nThe audio now plays at reduced quality";
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			mainwnd.OpenPurchaseLink();
			Close();
		}

		private void Button_Click_1(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
