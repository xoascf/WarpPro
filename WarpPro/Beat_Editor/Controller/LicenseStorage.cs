using System;
using System.IO;
using System.Web.Script.Serialization;

namespace Beat_Editor.Controller
{
	internal class LicenseStorage
	{
		private static LicenseStorage _instance;

		protected string path = SettingsFilePath;

		internal static LicenseStorage Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new LicenseStorage();
				}
				return _instance;
			}
		}

		private static string SettingsFilePath
		{
			get
			{
				string text = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\WarpPro";
				Directory.CreateDirectory(text);
				return text + "\\warppro_settings";
			}
		}

		protected LicenseStorage()
		{
		}

		protected virtual void WriteRaw(string data)
		{
			File.WriteAllText(path, data);
		}

		protected virtual string ReadRaw()
		{
			return File.ReadAllText(path);
		}

		internal void Save(LicenseInfo info)
		{
			lock (this)
			{
				JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
				WriteRaw(javaScriptSerializer.Serialize(info));
			}
		}

		internal LicenseInfo Load()
		{
			lock (this)
			{
				try
				{
					LicenseInfo licenseInfo = new JavaScriptSerializer().Deserialize<LicenseInfo>(ReadRaw());
					licenseInfo.Update(new WebLicense(licenseInfo.latestWebLicense));
					return licenseInfo;
				}
				catch
				{
					return new LicenseInfo();
				}
			}
		}
	}
}
