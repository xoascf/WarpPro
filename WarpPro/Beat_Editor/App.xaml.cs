using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows;
using Beat_Editor.Controller;
using Microsoft.Win32;

namespace Beat_Editor
{
	public partial class App : Application
	{
		public static readonly string VersionStr = _GetVerStr();

		public App()
		{
			Console.WriteLine("Starting app ver " + VersionStr);
			Updater.CheckAndInstallUpdatePackage();
			if (!Updater.AmICurrent() && Updater.StartCurrentPackage() != null)
			{
				Console.WriteLine("Started other copy, exit here");
				Process.GetCurrentProcess().Kill();
			}
			Registry.SetValue("HKEY_CURRENT_USER\\Software\\Microsoft\\Internet Explorer\\Main\\FeatureControl\\FEATURE_BROWSER_EMULATION", "WarpPro.exe", 10000, RegistryValueKind.DWord);
			Registry.SetValue("HKEY_CURRENT_USER\\Software\\Microsoft\\Internet Explorer\\Main\\FeatureControl\\FEATURE_BROWSER_EMULATION", "WarpPro.vshost.exe", 10000, RegistryValueKind.DWord);
		}

		private static string _GetVerStr()
		{
			Version version = typeof(App).Assembly.GetName().Version;
			return string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
		}

		[STAThread]
		public static void Main()
		{
			App app = new App();
			app.InitializeComponent();
			app.Run();
		}
	}
}
