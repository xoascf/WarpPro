using System;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using Microsoft.Win32;

namespace Beat_Editor.Controller
{
	internal class LicenseController
	{
		private static readonly int MAX_SAVES = 5;

		internal static readonly string LocalLicenseId = GetLocalMachineId();

		private LicenseInfo license;

		private DateTime startTime;

		private static LicenseController _instance;

		internal int LicenseLevel
		{
			get
			{
				return license.licenseLevel;
			}
		}

		internal bool IsLicenseIssueValid
		{
			get
			{
				return license.licenseIssuedUntil >= DateTime.Now;
			}
		}

		internal bool isLicenseLastWebUpdateDateValid
		{
			get
			{
				return license.lastWebLicenseUpdate.AddDays(270.0) >= DateTime.Now;
			}
		}

		internal bool IsTrialExpired
		{
			get
			{
				if (license.licenseLevel > 0)
				{
					return false;
				}
				return FileSavesRemaining() == 0;
			}
		}

		internal static LicenseController Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new LicenseController();
					Application.Current.Exit += _instance.OnApplicationExit;
				}
				return _instance;
			}
		}

		internal static string PurchaseUrl
		{
			get
			{
				string arg = (Instance.license.sandbox ? "sb_purchase.php" : "purchase.php");
				return string.Format("https://www.warppro.com/{0}?id={1}", arg, LocalLicenseId);
			}
		}

		internal event Action<bool> event_LicenseSyncCompleted;

		private LicenseController()
		{
			startTime = DateTime.UtcNow;
			license = EncryptedLicenseStorage.Instance.Load();
			UpdateStartData(license);
		}

		internal bool SyncLicense(int waitForLicenseSeconds = 0)
		{
			lock (this)
			{
				try
				{
					WebLicense webLicense = WebLicense.QueryWebLicense(license, waitForLicenseSeconds);
					if (webLicense != null)
					{
						license.Update(webLicense, true);
						EncryptedLicenseStorage.Instance.Save(license);
						if (license.autoUpdate)
						{
							Updater.CheckUpdateAvailability(license.sw.ver, license.sw.url);
						}
						return true;
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine("exp:" + ex);
				}
			}
			return false;
		}

		private void BG_SyncLicenseWorker(object sender, DoWorkEventArgs e)
		{
			Action<bool> action = (Action<bool>)e.Argument;
			bool obj = SyncLicense();
			if (this.event_LicenseSyncCompleted != null)
			{
				this.event_LicenseSyncCompleted(obj);
			}
		}

		internal void SyncLicenseAsync()
		{
			BackgroundWorker backgroundWorker = new BackgroundWorker();
			backgroundWorker.DoWork += BG_SyncLicenseWorker;
			backgroundWorker.RunWorkerAsync();
		}

		private static string ProductId()
		{
			string text = (string)Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion", "ProductId", "");
			if (text == "")
			{
				text = (string)RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion", RegistryKeyPermissionCheck.ReadSubTree).GetValue("ProductId");
			}
			return text;
		}

		internal static string GetLocalMachineId(bool code32 = true)
		{
			string s = ProductId();
			byte[] bytes = Encoding.ASCII.GetBytes(s);
			ulong value = BitConverter.ToUInt64(SHA256.Create().ComputeHash(bytes), 0);
			if (code32)
			{
				return Code32.long2code(value);
			}
			return value.ToString("X");
		}

		private void UpdateStartData(LicenseInfo lic)
		{
			lic.startsAfterLastUpdate++;
			DateTime utcNow = DateTime.UtcNow;
			float num = (float)(utcNow - lic.lastStartDate).TotalHours;
			if (num < 10000f)
			{
				if (lic.mtbs_num < 20)
				{
					lic.mtbs_num++;
					lic.mtbs_sum += num;
					lic.mtbs = lic.mtbs_sum / (float)lic.mtbs_num;
				}
				else
				{
					lic.mtbs = 0.95f * lic.mtbs + 0.05f * num;
				}
			}
			lic.lastStartDate = utcNow;
		}

		internal void IncSaveCount()
		{
			license.savesAfterLastUpdate++;
			license.saveCount++;
			if (license.licenseLevel == 0)
			{
				SyncLicense();
			}
		}

		internal int FileSavesRemaining()
		{
			int num = MAX_SAVES - license.saveCount;
			if (num < 0)
			{
				num = 0;
			}
			return num;
		}

		private void OnApplicationExit(object sender, ExitEventArgs e)
		{
			TimeSpan timeSpan = DateTime.UtcNow - startTime;
			license.onTimeAfterLastUpdate += (float)timeSpan.TotalMinutes;
			EncryptedLicenseStorage.Instance.Save(license);
		}

		internal object GetSettingValue(string key)
		{
			try
			{
				return license.settings[key];
			}
			catch
			{
				return null;
			}
		}

		internal void SetSettingValue(string key, object value)
		{
			license.settings[key] = value;
		}
	}
}
