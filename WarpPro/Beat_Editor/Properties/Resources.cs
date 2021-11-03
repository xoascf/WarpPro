using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace Beat_Editor.Properties
{
	[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
	[DebuggerNonUserCode]
	[CompilerGenerated]
	internal class Resources
	{
		private static ResourceManager resourceMan;

		private static CultureInfo resourceCulture;

		[EditorBrowsable(EditorBrowsableState.Advanced)]
		internal static ResourceManager ResourceManager
		{
			get
			{
				if (resourceMan == null)
				{
					resourceMan = new ResourceManager("Beat_Editor.Properties.Resources", typeof(Resources).Assembly);
				}
				return resourceMan;
			}
		}

		[EditorBrowsable(EditorBrowsableState.Advanced)]
		internal static CultureInfo Culture
		{
			get
			{
				return resourceCulture;
			}
			set
			{
				resourceCulture = value;
			}
		}

		internal static string BuildDate
		{
			get
			{
				return ResourceManager.GetString("BuildDate", resourceCulture);
			}
		}

		internal static Bitmap pause
		{
			get
			{
				return (Bitmap)ResourceManager.GetObject("pause", resourceCulture);
			}
		}

		internal static Bitmap play
		{
			get
			{
				return (Bitmap)ResourceManager.GetObject("play", resourceCulture);
			}
		}

		internal Resources()
		{
		}
	}
}
