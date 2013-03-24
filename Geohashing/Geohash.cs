using System;
using System.Device.Location;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;

namespace Geohashing
{
	public class Geohash
	{
		private readonly GeoCoordinate position;

		public GeoCoordinate Position
		{
			get
			{
				return position;
			}
		}

		private Geohash(GeoCoordinate position)
		{
			this.position = position;
		}

		public static async Task<Geohash> Get()
		{
			return await Get(DateTime.Now);
		}

		public static async Task<Geohash> Get(Geocoordinate position)
		{
			return await Get(position, DateTime.Now);
		}

		public static async Task<Geohash> Get(DateTime date)
		{
			Geolocator geolocator = new Geolocator();
			geolocator.DesiredAccuracyInMeters = 50;

			return await Get((await geolocator.GetGeopositionAsync()).Coordinate, date);
		}

		public static async Task<Geohash> Get(Geocoordinate position, DateTime date)
		{
			HttpWebResponse response = await HttpWebRequest.Create("http://relet.net/geo/" + (int)position.Latitude + "/" + (int)position.Longitude + "/" + date.ToString("yyyy-MM-dd")).GetResponseAsync();
			string html;
			using (StreamReader sr = new StreamReader(response.GetResponseStream()))
				html = await sr.ReadToEndAsync();

			Regex rLat = new Regex("\"lat\": (-?[0-9.]*)");
			Match mLat = rLat.Match(html);
			Group gLat = mLat.Groups[1];
			string sLat = gLat.Value;
			double lat = Double.Parse(sLat, CultureInfo.InvariantCulture);

			Regex rLon = new Regex("\"lon\": (-?[0-9.]*)");
			Match mLon = rLon.Match(html);
			Group gLon = mLon.Groups[1];
			string sLon = gLon.Value;
			double lon = Double.Parse(sLon, CultureInfo.InvariantCulture);

			return new Geohash(new GeoCoordinate(lat, lon));
		}
	}

	static class HttpWebRequestExtension
	{
		// From http://matthiasshapiro.com/2012/12/10/window-8-win-phone-code-sharing-httpwebrequest-getresponseasync/
		public static Task<HttpWebResponse> GetResponseAsync(this WebRequest request)
		{
			var taskComplete = new TaskCompletionSource<HttpWebResponse>();
			request.BeginGetResponse(asyncResponse =>
				{
					try
					{
						HttpWebRequest responseRequest = (HttpWebRequest)asyncResponse.AsyncState;
						HttpWebResponse someResponse = (HttpWebResponse)responseRequest.EndGetResponse(asyncResponse);
						taskComplete.TrySetResult(someResponse);
					}
					catch (WebException webExc)
					{
						HttpWebResponse failedResponse = (HttpWebResponse)webExc.Response;
						taskComplete.TrySetResult(failedResponse);
					}
				}, request);
			return taskComplete.Task;
		}
	}
}
