using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Geohashing
{
	class Program
	{
		static void Main(string[] args)
		{
			string html = "{\"300d\": 2, \"hashers\": 3, \"last\": \"2012-08-11\", \"graticule\": \"Gj\u00f8vik, Norway\", \"success\": 14, \"lon\": 10.577111, \"global-lon\": 27.759930, \"w30\": true, \"attempts\": 21, \"djia\": 12620.90, \"lat\": 60.125367, \"global-lat\": -67.433906, \"30d\": 0}";

			Regex rLat = new Regex("\"lat\": ([0-9.]*)");
			Match mLat = rLat.Match(html);
			Group gLat = mLat.Groups[1];
			string sLat = gLat.Value;
			double lat = Double.Parse(sLat, CultureInfo.InvariantCulture);

			Regex rLon = new Regex("\"lon\": ([0-9.]*)");
			Match mLon = rLon.Match(html);
			Group gLon = mLon.Groups[1];
			string sLon = gLon.Value;
			double lon = Double.Parse(sLon, CultureInfo.InvariantCulture);

			Console.WriteLine(lat + "," + lon);
		}
	}
}
