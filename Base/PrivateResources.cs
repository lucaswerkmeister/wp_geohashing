using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geohashing
{
	/// <summary>
	/// This class holds all resources that I can't publish.
	/// It's basically file that I commit once and after that have lying around constantly with changes (inserting my actual resources) that never get committed.
	/// </summary>
	public static class PrivateResources
	{
		public const string BingMapsKey = "Enter your own Bing Maps key here (needed for Live Tile image); you can get one here: https://www.bingmapsportal.com/application/index/1207747?status=NoStatus";
		public const string MicrosoftMapServiceApplicationID = "Enter your Application ID here (only needed for publishing); you get it when you publish your app";
		public const string MicrosoftMapServiceAuthToken = "Enter your Auth Token here (only needed for publishing); you get it when you publish your app";
	}
}
