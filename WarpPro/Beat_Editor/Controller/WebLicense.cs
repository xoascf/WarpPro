using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace Beat_Editor.Controller
{
	internal class WebLicense
	{
		private static readonly string _LICSERVER = "https://warppro.com/api/check.php";

		private static readonly string _AK = "d4yDwjLxcuX7hYaf";

		private static readonly string _PUBLICKEY = "<RSAKeyValue><Modulus>fNzRPNg3Hkl9faTY2T7K25GN9bHDWAQdudLYcLULDdWdoI2qgy8DemK4WvKf1ZyZ6U/Nie+KHD1bHOOCoBKM9Q==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

		internal string webLicenseString;

		internal bool isValid;

		internal dynamic dict;

		private bool VerifySignatureSha1(string data_string, string signature_base64)
		{
			RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider();
			rSACryptoServiceProvider.FromXmlString(_PUBLICKEY);
			return rSACryptoServiceProvider.VerifyData(Encoding.UTF8.GetBytes(data_string), signature: Convert.FromBase64String(signature_base64), halg: CryptoConfig.MapNameToOID("SHA1"));
		}

		internal WebLicense(string webLicenseString)
		{
			JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
			string text = "";
			try
			{
				dynamic val = javaScriptSerializer.DeserializeObject(webLicenseString);
				string signature_base = (string)val["sign"];
				text = (string)val["data"];
				isValid = VerifySignatureSha1(text, signature_base);
				if (isValid)
				{
					dynamic val2 = javaScriptSerializer.DeserializeObject(text);
					if (((string)val2["licid"]).Equals(LicenseController.LocalLicenseId))
					{
						this.webLicenseString = webLicenseString;
						dict = val2;
					}
					else
					{
						isValid = false;
					}
				}
			}
			catch
			{
				isValid = false;
			}
		}

		internal static WebLicense QueryWebLicense(LicenseInfo lic, int waitForLicenseSeconds = 0)
		{
			try
			{
				string text = lic.mtbs.ToString().Replace(",", ".");
				string text2 = lic.onTimeAfterLastUpdate.ToString().Replace(",", ".");
				string text3 = string.Format(_LICSERVER + "?access={0}&startcount={1}&mtbs={2}&ontime={3}&id={4}&savecountd={5}&ver={6}", _AK, lic.startsAfterLastUpdate, text, text2, LicenseController.LocalLicenseId, lic.savesAfterLastUpdate, App.VersionStr);
				if (waitForLicenseSeconds > 0)
				{
					text3 += string.Format("&waitlicense={0}", waitForLicenseSeconds);
				}
				HttpWebResponse httpWebResponse = (HttpWebResponse)((HttpWebRequest)WebRequest.Create(text3)).GetResponse();
				string text4;
				using (StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8))
				{
					text4 = streamReader.ReadToEnd();
				}
				httpWebResponse.Close();
				WebLicense webLicense = new WebLicense(text4);
				if (webLicense.isValid)
				{
					lic.startsAfterLastUpdate = 0;
					lic.onTimeAfterLastUpdate = 0f;
					lic.savesAfterLastUpdate = 0;
				}
				return webLicense;
			}
			catch
			{
				return null;
			}
		}
	}
}
