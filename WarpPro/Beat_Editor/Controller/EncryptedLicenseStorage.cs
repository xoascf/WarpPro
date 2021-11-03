using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Beat_Editor.Controller
{
	internal class EncryptedLicenseStorage : LicenseStorage
	{
		private static EncryptedLicenseStorage _instance;

		private string keyname = LicenseController.GetLocalMachineId(false);

		internal new static EncryptedLicenseStorage Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new EncryptedLicenseStorage();
				}
				return _instance;
			}
		}

		protected EncryptedLicenseStorage()
		{
		}

		protected override void WriteRaw(string data)
		{
			byte[] bytes = Encoding.ASCII.GetBytes(data);
			byte[] bytes2 = Encoding.ASCII.GetBytes(keyname);
			byte[] bytes3 = ProtectedData.Protect(bytes, bytes2, DataProtectionScope.LocalMachine);
			File.WriteAllBytes(path, bytes3);
		}

		protected override string ReadRaw()
		{
			byte[] bytes = Encoding.ASCII.GetBytes(keyname);
			byte[] bytes2 = ProtectedData.Unprotect(File.ReadAllBytes(path), bytes, DataProtectionScope.LocalMachine);
			return Encoding.ASCII.GetString(bytes2);
		}
	}
}
