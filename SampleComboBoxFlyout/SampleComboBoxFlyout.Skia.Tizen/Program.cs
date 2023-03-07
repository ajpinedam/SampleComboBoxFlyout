using Tizen.Applications;
using Uno.UI.Runtime.Skia;

namespace SampleComboBoxFlyout.Skia.Tizen
{
	public sealed class Program
	{
		static void Main(string[] args)
		{
			var host = new TizenHost(() => new SampleComboBoxFlyout.App());
			host.Run();
		}
	}
}
