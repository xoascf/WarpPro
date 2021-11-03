using System;
using System.IO;
using System.Security.Cryptography;
using System.Web.Script.Serialization;

namespace Beat_Editor.Controller
{
	internal class Verifier
	{
		private static string publickey = "<RSAKeyValue><Modulus>WLfR+k4iH5rHntcXhSiDqJWNJ5qR9mnOlwL5V8kOca/bJKul8XhbJuOIkADqL5QXhi48Ahr/uunMJ+NTh2xzm19ucCK3vrs+W/TUl2AbIKdkpokgpjA6+E2xRNDbTK1VbLJ9xZq5yFxrWvgezCSSWffWSJlozZcUbNIFDTSuSmL7Ry1R9L1wxUX6oHTm6cBta/nQo+UTQte714shJx5YftHUMzgRgCeINB7x308zl0gbSLYuocE5c93HCHxBhDYmAvvKnJ/9m/SXrGmf24+kfm+r44Scsv+zt1V8fwHrCAa5BNZjaza0VY+Dac/9MAgwf3L7CCCeVFZgrBI7kK7A5Q==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

		private static string privatekey = "<RSAKeyValue><Modulus>WLfR+k4iH5rHntcXhSiDqJWNJ5qR9mnOlwL5V8kOca/bJKul8XhbJuOIkADqL5QXhi48Ahr/uunMJ+NTh2xzm19ucCK3vrs+W/TUl2AbIKdkpokgpjA6+E2xRNDbTK1VbLJ9xZq5yFxrWvgezCSSWffWSJlozZcUbNIFDTSuSmL7Ry1R9L1wxUX6oHTm6cBta/nQo+UTQte714shJx5YftHUMzgRgCeINB7x308zl0gbSLYuocE5c93HCHxBhDYmAvvKnJ/9m/SXrGmf24+kfm+r44Scsv+zt1V8fwHrCAa5BNZjaza0VY+Dac/9MAgwf3L7CCCeVFZgrBI7kK7A5Q==</Modulus><Exponent>AQAB</Exponent><P>oCBMHiJwc+3cHkqwY60/hNZuzc4+33CxCOS4AY2FGAwwIv7WQQgWanIOzV8p+Tc3A6oAYES1sSbaJ+pSH6WY28r0Ajtzm6rOu9pDYpLf0K7Aph4myWut4G/jo0dFZ7/E70YvZdErXwXnijJtbBTfEc4H6/ztkXkR7msc2urFno0=</P><Q>jdZIB/wYDtdkfKZBHCU5tXLGkmP0GwQ2BYXImGxgaXpx9AHzJBfqdB3dfhg2S6zXs8XUu6BEJZ2CghrM6oIQP31/77XZ47v5NR9JhV/jCULGWMS2tKkNts/XLoujYWZ4z5kOOndOf6VKmR05fOnGu57AmT6ruBp79h8VQC/rIbk=</Q><DP>izJoWQ3hKbYNUrvkyFGT1Rs/aWMwHrbs/uks2BS5LWVy9wkHIbMxIUmTeo6Og1mvVl0TRJyWQbCflnFJAL/IuNCd+87IufrrCjw7tdYuAE/Zos61MwWLOn6pqYfMWttHDCW8EEub41fTprwdiQY/wE+VbV0K/Bn+L38nr1rFfgU=</DP><DQ>T03HOoeuX+X4rmU6tGTv7k79Te6LFuv01IOn0+mMwo0O19KbQswIb+Ie7JjXCtraRA7R0hJa5/k4dkxL1LbiJM2j0cCI0ndQcG5M6kDhrVTjl9BtI3f+Tf/JnaG/uP7Vf6VhOjlo75/YpOGdOgVv84lgwI407xwHwWZBuIDSZCk=</DQ><InverseQ>Y4dAb26R1U9LAzPUm+yTyt4PqxfmbpDeTv/fy/SHcjI97bIbS4kQvxiCOOpI10ntqdmnuul+LNtCoI3c9GSZ5bQf4fO1M1/zSPAhyqOljKPHKndlPR1Xbh5EukGBNl6L3UbKgwuc7AKN0jHKdzme8EoUhXekmbtpyjZ9WYszQxU=</InverseQ><D>JOG+VE2ZGARzilAnAf92xGXvuZAjsAoKjXL6D4mRDKgr9VG6eeksE72xrxQCR2dMZEbiQYnR2HWLnuV9eyYV5XjF4647LvqOkRH2MzwL6wZL7IPDY/NXAJTj5hThpj3E7pxBdx28sFPjeXz+6KUnhmlm1uWX0ShGoHqrCPNmpZuFDEWxGKsGalHRlpiNCftagdCiYlHEyBCPOG6RBbbEf4bxI38pAAvyqEU6Mqj86Nsp7CadcId/OjD1y8k706j61dLVu1tfBmIapu7lMc25Qt0T1tqi+u6b2wkcfNEx6bhwbaLVNKLGPL32zslXELSzNTZSJIX6MO7n0/ll5UeEIQ==</D></RSAKeyValue>";

		internal static string GenerateSignature(string filePath)
		{
			RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider();
			rSACryptoServiceProvider.FromXmlString(privatekey);
			byte[] buffer = File.ReadAllBytes(filePath);
			return Convert.ToBase64String(rSACryptoServiceProvider.SignData(buffer, CryptoConfig.MapNameToOID("SHA1")));
		}

		internal static void GenerateManifest(string rootPath, string manifestFileName, string targetFileName, string targetKey)
		{
			string arg = GenerateSignature(rootPath + targetFileName);
			using (StreamWriter streamWriter = new StreamWriter(rootPath + manifestFileName))
			{
				streamWriter.WriteLine(string.Format("{{\n\t\"{0}\":\"{1}\",\n\t\"sign\":\"{2}\"\n}}", targetKey, targetFileName, arg));
			}
		}

		private static bool VerifySignature(string filePath, string signature_base64)
		{
			RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider();
			rSACryptoServiceProvider.FromXmlString(publickey);
			return rSACryptoServiceProvider.VerifyData(File.ReadAllBytes(filePath), signature: Convert.FromBase64String(signature_base64), halg: CryptoConfig.MapNameToOID("SHA1"));
		}

		internal static string ReadManifestValue(string manifestFilePath, string targetKey)
		{
			try
			{
				dynamic val;
				using (StreamReader streamReader = new StreamReader(manifestFilePath))
				{
					string input = streamReader.ReadToEnd();
					val = new JavaScriptSerializer().DeserializeObject(input);
				}
				return (string)val[targetKey];
			}
			catch (Exception value)
			{
				Console.WriteLine(value);
			}
			return null;
		}

		internal static bool VerifyManifestTarget(string rootPath, string manifestFileName, string targetKey)
		{
			try
			{
				dynamic val;
				using (StreamReader streamReader = new StreamReader(rootPath + manifestFileName))
				{
					string input = streamReader.ReadToEnd();
					val = new JavaScriptSerializer().DeserializeObject(input);
				}
				string text = (string)val[targetKey];
				string signature_base = (string)val["sign"];
				return VerifySignature(rootPath + text, signature_base);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
			return false;
		}
	}
}
