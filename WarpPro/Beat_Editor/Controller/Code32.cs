namespace Beat_Editor.Controller
{
	internal class Code32
	{
		private const string tokens = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";

		internal static string long2code(ulong value)
		{
			string text = "";
			uint length = (uint)"23456789ABCDEFGHJKLMNPQRSTUVWXYZ".Length;
			uint num = 0u;
			for (int i = 0; i < 16; i++)
			{
				if (i != 4 && i != 9 && i != 14)
				{
					int index = (int)(value % length);
					value /= length;
					char c = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ"[index];
					text += c;
					uint num2 = num;
					num = (num2 ^ (num2 << 1)) + c;
				}
				else
				{
					text += "-";
				}
			}
			for (int j = 0; j < 3; j++)
			{
				int index2 = (int)(num % length);
				num /= length;
				text += "23456789ABCDEFGHJKLMNPQRSTUVWXYZ"[index2];
			}
			return text;
		}

		internal static bool verifycode(string code)
		{
			code = code.ToUpper().Replace("-", "");
			uint length = (uint)"23456789ABCDEFGHJKLMNPQRSTUVWXYZ".Length;
			uint num = 0u;
			if (code.Length != 16)
			{
				return false;
			}
			for (int i = 0; i < 13; i++)
			{
				char c = code[i];
				uint num2 = num;
				num = (num2 ^ (num2 << 1)) + c;
			}
			string text = "";
			for (int j = 0; j < 3; j++)
			{
				int index = (int)(num % length);
				num /= length;
				text += "23456789ABCDEFGHJKLMNPQRSTUVWXYZ"[index];
			}
			return code.EndsWith(text);
		}

		internal static long code2long(string code)
		{
			code = code.ToUpper().Replace("-", "");
			ulong num = 0uL;
			int length = code.Length;
			if (length != 16)
			{
				return -1L;
			}
			int num2 = length - 4;
			while (true)
			{
				if (num2 >= 0)
				{
					num *= (uint)"23456789ABCDEFGHJKLMNPQRSTUVWXYZ".Length;
					int num3 = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ".IndexOf(code[num2]);
					if (num3 < 0)
					{
						break;
					}
					num += (uint)num3;
					num2--;
					continue;
				}
				return (long)num;
			}
			return -1L;
		}
	}
}
