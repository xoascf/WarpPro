using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Beat_Editor
{
	public partial class SaveProgress : Window, IComponentConnector
	{
		private FileProcessor fp;

		public SaveProgress(Window owner, FileProcessor fp, string name)
		{
			this.fp = fp;
			InitializeComponent();
			base.Owner = owner;
			base.WindowStartupLocation = WindowStartupLocation.CenterOwner;
			progress.Value = 0.0;
			base.Title = "Save " + name;
		}

		public void SetProgress(int percent)
		{
			progress.Value = percent;
			if (percent > 100)
			{
				Close();
				Application.Current.MainWindow.Focus();
			}
		}

		private void buttonCancel_Click(object sender, RoutedEventArgs e)
		{
			fp.Cancel();
		}
	}
}
