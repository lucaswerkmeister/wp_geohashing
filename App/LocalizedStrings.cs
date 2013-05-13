using Geohashing.Resources;

namespace Geohashing
{
	public class LocalizedStrings
	{
		private static AppResources localizedResources = new AppResources();

		public AppResources AppResources
		{
			get { return localizedResources; }
		}
	}
}