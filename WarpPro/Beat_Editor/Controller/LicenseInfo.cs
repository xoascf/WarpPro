using System;
using System.Collections.Generic;

namespace Beat_Editor.Controller
{
	internal class LicenseInfo
	{
		public class SW
		{
			public string url;

			public string ver;

			public SW()
			{
			}

			public SW(dynamic dict)
			{
				url = dict["url"];
				ver = dict["ver"];
			}
		}

		public class ServerInfo
		{
			public string url;

			public int port;

			public ServerInfo()
			{
			}

			public ServerInfo(dynamic dict)
			{
				url = dict["url"];
				port = int.Parse(dict["port"]);
			}
		}

		internal int licenseLevel;

		internal int saveCount;

		internal bool sandbox;

		public bool autoUpdate;

		private int startcount;

		private float totalOnDurationMinutes;

		private DateTime licenseexpdate = DateTime.MaxValue;

		private DateTime maintenanceexpdate = DateTime.MaxValue;

		internal DateTime licenseIssuedUntil = DateTime.MinValue;

		private ServerInfo server;

		public SW sw;

		public Dictionary<string, object> settings = new Dictionary<string, object>();

		public float mtbs;

		public float mtbs_sum;

		public int mtbs_num;

		public string latestWebLicense;

		public int startsAfterLastUpdate;

		public DateTime lastStartDate;

		public DateTime lastWebLicenseUpdate;

		public float onTimeAfterLastUpdate;

		public int savesAfterLastUpdate;

		public void Update(WebLicense webLicense, bool updateFromWeb = false)
		{
			if (webLicense != null && webLicense.isValid)
			{
				latestWebLicense = webLicense.webLicenseString;
				dynamic dict = webLicense.dict;
				sw = new SW(dict["sw"]);
				server = new ServerInfo(dict["server"]);
				DateTime.TryParse(dict["licenseexpdate"], out licenseexpdate);
				DateTime.TryParse(dict["maintenanceexpdate"], out maintenanceexpdate);
				DateTime.TryParse(dict["issueduntil"], out licenseIssuedUntil);
				sandbox = dict["sandbox"] == "1";
				licenseLevel = int.Parse(dict["licenselevel"]);
				saveCount = int.Parse(dict["savecount"]);
				autoUpdate = dict["autoupdate"] == "1";
				startcount = int.Parse(dict["startcount"]);
				if (float.TryParse(dict["mtbs"], out mtbs) == false)
				{
					float.TryParse(dict["mtbs"].Replace(".", ","), out mtbs);
				}
				if (float.TryParse(dict["totalontime"], out totalOnDurationMinutes) == false)
				{
					float.TryParse(dict["totalontime"].Replace(".", ","), out totalOnDurationMinutes);
				}
				if (updateFromWeb)
				{
					lastWebLicenseUpdate = DateTime.UtcNow;
				}
			}
		}
	}
}
