using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
#if NET40
using System.IO.Packaging;
#else
using System.IO.Compression;
#endif
using System.Net;
using System.Threading;

namespace Beat_Editor.Controller
{
	public class Updater
	{
		public static readonly string UpdateFolder = DataFolder + "\\new\\";

		public static readonly string PrevFolder = DataFolder + "\\prev\\";

		public static readonly string CurFolder = DataFolder + "\\current\\";

		private static string MANIFEST_FILENAME = "manifest";

		private static string DataFolder
		{
			get
			{
				return Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\WarpPro";
			}
		}

		private static bool VerifyAndExtractPackage()
		{
			if (!Verifier.VerifyManifestTarget(UpdateFolder, MANIFEST_FILENAME, "package"))
			{
				return false;
			}
			string text = Verifier.ReadManifestValue(UpdateFolder + MANIFEST_FILENAME, "package");
			UnzipOverwriting(UpdateFolder + text, UpdateFolder);
			return Verifier.VerifyManifestTarget(UpdateFolder, MANIFEST_FILENAME, "exepath");
		}

		public static void UnzipOverwriting(string zipfile, string extractpath)
		{
#if NET40
			// Taken from https://stackoverflow.com/a/508030
			using (Package package = Package.Open(zipfile, FileMode.Open, FileAccess.Read))
			{
				foreach (PackagePart part in package.GetParts())
				{
					char[] invalidChars = Path.GetInvalidFileNameChars();
                    System.Text.StringBuilder sb = new System.Text.StringBuilder(part.Uri.OriginalString.Length);
					foreach (char c in part.Uri.OriginalString)
					{
						sb.Append(Array.IndexOf(invalidChars, c) < 0 ? c : '_');
					}
					string destinationFileName = extractpath + sb.ToString();
					using (Stream source = part.GetStream(FileMode.Open, FileAccess.Read))
					using (Stream destination = File.OpenWrite(destinationFileName))
					{
						byte[] buffer = new byte[0x1000];
						int read;
						while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
						{
							destination.Write(buffer, 0, read);
						}
					}
				}
			}
#else

			using (ZipArchive zipArchive = ZipFile.OpenRead(zipfile))
			{
				foreach (ZipArchiveEntry entry in zipArchive.Entries)
				{
					string destinationFileName = extractpath + entry.FullName;
					entry.ExtractToFile(destinationFileName, true);
				}
			}
#endif
		}

		private static bool MoveDir(string SourcePath, string TargetPath)
		{
			try
			{
				Directory.Delete(TargetPath, true);
			}
			catch
			{
			}
			try
			{
				Directory.CreateDirectory(TargetPath);
				FileInfo[] files = new DirectoryInfo(SourcePath).GetFiles();
				foreach (FileInfo fileInfo in files)
				{
					fileInfo.CopyTo(Path.Combine(TargetPath, fileInfo.Name), true);
				}
				Directory.Delete(SourcePath, true);
				return true;
			}
			catch (Exception value)
			{
				Console.WriteLine(value);
				return false;
			}
		}

		public static bool CheckAndInstallUpdatePackage(string updatefilename = null)
		{
			try
			{
				if (Directory.Exists(UpdateFolder))
				{
					if (updatefilename != null)
					{
						string text = UpdateFolder + updatefilename;
						if (File.Exists(text))
						{
							UnzipOverwriting(text, UpdateFolder);
							File.Delete(text);
						}
					}
					if (!Verifier.VerifyManifestTarget(UpdateFolder, MANIFEST_FILENAME, "exepath") && !VerifyAndExtractPackage())
					{
						return false;
					}
					MoveDir(CurFolder, PrevFolder);
					MoveDir(UpdateFolder, CurFolder);
					return true;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("exp in CheckUpdatePackage:" + ex);
			}
			return false;
		}

		public static bool AmICurrent()
		{
			return Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName).Equals(Path.GetDirectoryName(CurFolder));
		}

		public static Process StartCurrentPackage(bool start = true)
		{
			try
			{
				if (Verifier.VerifyManifestTarget(CurFolder, MANIFEST_FILENAME, "exepath"))
				{
					string text = Verifier.ReadManifestValue(CurFolder + MANIFEST_FILENAME, "exepath");
					Process process = Process.Start(new ProcessStartInfo(CurFolder + text)
					{
						UseShellExecute = true,
						WorkingDirectory = CurFolder
					});
					if (process != null)
					{
						Thread.Sleep(1000);
						if (!process.HasExited)
						{
							return process;
						}
					}
				}
			}
			catch (Exception value)
			{
				Console.WriteLine(value);
			}
			return null;
		}

		private static void BG_DownloadUpdate(object sender, DoWorkEventArgs e)
		{
			string text = (string)e.Argument;
			int num = text.LastIndexOf('/');
			if (num <= 0)
			{
				return;
			}
			try
			{
				string text2 = UpdateFolder + text.Substring(num + 1);
				if (!Directory.Exists(UpdateFolder))
				{
					Directory.CreateDirectory(UpdateFolder);
				}
				if (File.Exists(text2))
				{
					File.Delete(text2);
				}
				using (WebClient webClient = new WebClient())
				{
					webClient.DownloadFile(text, text2);
				}
				if (File.Exists(text2))
				{
					UnzipOverwriting(text2, UpdateFolder);
					File.Delete(text2);
				}
				if (!VerifyAndExtractPackage())
				{
					Directory.Delete(UpdateFolder, true);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("BG_DownloadUpdate exp:" + ex);
			}
		}

		internal static void CheckUpdateAvailability(string version, string url)
		{
			if (version.CompareTo(App.VersionStr) > 0)
			{
				BackgroundWorker backgroundWorker = new BackgroundWorker();
				backgroundWorker.DoWork += BG_DownloadUpdate;
				backgroundWorker.RunWorkerAsync(url);
			}
		}
	}
}
