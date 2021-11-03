using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Navigation;
using Beat_Editor.Controller;
using MSHTML;

namespace Beat_Editor.View
{
	public partial class PurchaseWindow : Window, IComponentConnector
	{
		private enum MaximizeMode
		{
			Toggle,
			Normal,
			Maximize
		}

		private readonly string DisableScriptError = "function noError() { return true;} window.onerror = noError;";

		private bool isPaypalVisited;

		public static readonly Brush HoverOnButtronBrush = new SolidColorBrush(Color.FromArgb(96, 45, 115, 200));

		private X509Certificate2 latestCert2;

		private static Dictionary<string, X509Certificate2> verifiedCertCache = new Dictionary<string, X509Certificate2>();

		public PurchaseWindow()
		{
			InitializeComponent();
			HoverOnButtronBrush.Freeze();
			textCert.Text = "";
			dockCert.Visibility = Visibility.Hidden;
			double num = SystemParameters.WorkArea.Height - 30.0;
			if (base.Height > num)
			{
				base.Height = num;
			}
			browserView.Navigate(LicenseController.PurchaseUrl);
		}

		private void InjectDisableScript()
		{
			HTMLDocument obj = (HTMLDocument)browserView.Document;
			IHTMLScriptElement iHTMLScriptElement = (IHTMLScriptElement)obj.createElement("SCRIPT");
			iHTMLScriptElement.type = "text/javascript";
			iHTMLScriptElement.text = DisableScriptError;
			foreach (IHTMLElement item in obj.getElementsByTagName("head"))
			{
				((HTMLHeadElement)item).appendChild((IHTMLDOMNode)iHTMLScriptElement);
			}
		}

		private void browserView_Navigating(object sender, NavigatingCancelEventArgs e)
		{
			string argument = e.Uri.Scheme + "://" + e.Uri.Host;
			textUrl.Text = e.Uri.AbsoluteUri;
			BackgroundWorker backgroundWorker = new BackgroundWorker();
			backgroundWorker.DoWork += BG_UpdateCertificateIndicators;
			backgroundWorker.RunWorkerAsync(argument);
		}

		private void browserView_Navigated(object sender, NavigationEventArgs e)
		{
			string absoluteUri = e.Uri.AbsoluteUri;
			textUrl.Text = absoluteUri;
			if (absoluteUri.Contains("paypal.com"))
			{
				isPaypalVisited = true;
			}
			InjectDisableScript();
		}

		private void BG_LicenseCheck(object sender, DoWorkEventArgs e)
		{
			int licenseLevel = LicenseController.Instance.LicenseLevel;
			int num = 0;
			LicenseController.Instance.SyncLicense(90);
			num = LicenseController.Instance.LicenseLevel;
			if (licenseLevel != num)
			{
#if NET40
				Dispatcher.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate
#else
				base.Dispatcher.Invoke(delegate
#endif
				{
					System.Windows.Forms.MessageBox.Show("Thank you for purchasing WarpPro! The Application will now restart to activate the new license ...");
					System.Windows.Forms.Application.Restart();
					Environment.Exit(0);
				});
			}
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			if (isPaypalVisited)
			{
				BackgroundWorker backgroundWorker = new BackgroundWorker();
				backgroundWorker.DoWork += BG_LicenseCheck;
				backgroundWorker.RunWorkerAsync();
			}
		}

		private X509Certificate2 CheckCertificate(string host)
		{
			HttpWebRequest obj = (HttpWebRequest)WebRequest.Create(host);
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
			((HttpWebResponse)obj.GetResponse()).Close();
			X509Certificate2 x509Certificate = new X509Certificate2(obj.ServicePoint.Certificate);
			if (x509Certificate.Verify())
			{
				return x509Certificate;
			}
			return null;
		}

		private void BG_UpdateCertificateIndicators(object sender, DoWorkEventArgs e)
		{
			try
			{
				string text = (string)e.Argument;
#if NET40
				Dispatcher.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate
#else
				base.Dispatcher.Invoke(delegate
#endif
				{
					dockCert.Visibility = Visibility.Hidden;
					textCert.Text = "";
				});
				lock (verifiedCertCache)
				{
					latestCert2 = null;
					if (verifiedCertCache.ContainsKey(text))
					{
						latestCert2 = verifiedCertCache[text];
					}
				}
				if (latestCert2 == null)
				{
					latestCert2 = CheckCertificate(text);
					if (latestCert2 != null)
					{
						lock (verifiedCertCache)
						{
							verifiedCertCache.Add(text, latestCert2);
						}
					}
				}
				if (latestCert2 == null)
				{
					return;
				}
#if NET40
				Dispatcher.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate
#else
				base.Dispatcher.Invoke(delegate
#endif
				{
					string[] array = latestCert2.Subject.Split('=', ',');
					if (array[0].ToUpper() == "CN")
					{
						textCert.Text = array[1];
					}
					dockCert.Visibility = Visibility.Visible;
				});
			}
			catch (Exception ex)
			{
				Console.WriteLine("exp" + ex);
			}
		}

		private void TextBlock_MouseEnter(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			((TextBlock)sender).Background = HoverOnButtronBrush;
		}

		private void TextBlock_MouseLeave(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			((TextBlock)sender).Background = base.Background;
		}

		private void Window_MouseLeave(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			ReleaseMouseCapture();
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

		private void Close_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			Close();
		}

		private void TextBlock_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
		{
			((TextBlock)sender).Background = HoverOnButtronBrush;
		}

		private void TextBlock_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
		{
			((TextBlock)sender).Background = base.Background;
		}

		private void DockPanel_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
		{
			((DockPanel)sender).Background = Brushes.SteelBlue;
		}

		private void DockPanel_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
		{
			((DockPanel)sender).Background = base.Background;
		}

		private void DockPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (latestCert2 != null)
			{
				X509Certificate2UI.DisplayCertificate(latestCert2);
			}
		}

		private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			try
			{
				Point position = e.GetPosition(this);
				Point point = closeBlock.PointToScreen(new Point(0.0, 0.0));
				if (position.Y > 0.0 && position.Y < 30.0 && position.X > 0.0 && position.X < point.X && base.WindowState == WindowState.Normal)
				{
					DragMove();
				}
			}
			catch
			{
			}
		}

		private void textUrl_PreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.RightButton == MouseButtonState.Pressed)
			{
				textUrl.SelectAll();
			}
		}

		private void MenuItem_ClickCopy(object sender, RoutedEventArgs e)
		{
			textUrl.SelectAll();
			textUrl.Copy();
		}

		private void MenuItem_ClickOpenBrowser(object sender, RoutedEventArgs e)
		{
			Process.Start(textUrl.Text);
		}
	}
}
