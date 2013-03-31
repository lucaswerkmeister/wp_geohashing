/*
 * Geohashing, a Windows Phone 8 app for geohashing.
 * Copyright (C) 2013  Lucas Werkmeister
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
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

		public static async Task<Geohash> Get(GeoCoordinate position)
		{
			return await Get(position, DateTime.Now);
		}

		public static async Task<Geohash> Get(DateTime date)
		{
			Geolocator geolocator = new Geolocator();
			geolocator.DesiredAccuracyInMeters = 50;

			return await Get((await geolocator.GetGeopositionAsync()).Coordinate.Convert(), date);
		}

		public static async Task<Geohash> Get(GeoCoordinate position, DateTime date)
		{
			HttpWebResponse response = await HttpWebRequest.Create("http://relet.net/geo/" + (int)position.Latitude + "/" + (int)position.Longitude + "/" + date.ToString("yyyy-MM-dd")).GetResponseAsync();
			if (response.StatusCode == HttpStatusCode.NotFound)
				throw new NoGeohashException(NoGeohashException.NoGeohashCause.NoInternet);
			else if (response.StatusCode != HttpStatusCode.OK)
				throw new NoGeohashException(NoGeohashException.NoGeohashCause.UnknownConnectionError);
			string html;
			using (StreamReader sr = new StreamReader(response.GetResponseStream()))
				html = await sr.ReadToEndAsync();

			Regex rErr = new Regex("\"error\": \"([^\"]*)\"");
			Match mErr = rErr.Match(html);
			Group gErr = mErr.Groups[1];
			string sErr = gErr.Value;
			if (sErr.Length > 0)
				throw new NoGeohashException(sErr);

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

	public class NoGeohashException : Exception
	{
		public enum NoGeohashCause { NoInternet, NoDjia, UnknownConnectionError, Unknown }

		public NoGeohashCause Cause { get; private set; }

		public NoGeohashException() : base() { }
		public NoGeohashException(string message)
			: base(message)
		{
			if (message.Contains("DJIA for") && message.Contains("not available yet"))
				Cause = NoGeohashCause.NoDjia;
		}
		public NoGeohashException(NoGeohashCause cause)
		{
			Cause = cause;
		}
		public NoGeohashException(string message, Exception innerException)
			: base(message, innerException)
		{
			if (message.Contains("DJIA for") && message.Contains("not available yet"))
				Cause = NoGeohashCause.NoDjia;
		}
	}

	public static class Extensions
	{
		// From http://matthiasshapiro.com/2012/12/10/window-8-win-phone-code-sharing-httpwebrequest-getresponseasync/
		public static Task<HttpWebResponse> GetResponseAsync(this WebRequest request)
		{
			if (request == null)
				throw new ArgumentNullException("request");
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

		public static GeoCoordinate Convert(this Geocoordinate coordinates)
		{
			if (coordinates == null)
				throw new ArgumentNullException("coordinates");
			return new GeoCoordinate(coordinates.Latitude, coordinates.Longitude);
		}
	}
}
