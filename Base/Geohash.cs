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
using Microsoft.Phone.Maps.Controls;
using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;

namespace Geohashing
{
	public class Geohash
	{
		private static readonly DateTime Rule30WValidityStart = new DateTime(2008, 05, 27);

		private readonly DateTime date;
		private readonly GeoCoordinate position;

		public GeoCoordinate Position
		{
			get
			{
				return position;
			}
		}

		public LocationRectangle Graticule
		{
			get
			{
				int top = (int)Math.Ceiling(position.Latitude);
				int left = (int)Math.Floor(position.Longitude);
				return new LocationRectangle(
					north: top,
					west: left,
					south: top - 1,
					east: left + 1);
			}
		}

		public bool Rule30WApplies
		{
			get
			{
				return date.CompareTo(Rule30WValidityStart) >= 0 && position.Longitude > -30;
			}
		}

		public DateTime Date
		{
			get
			{
				return date.Date;
			}
		}

		private Geohash(DateTime date, GeoCoordinate position)
		{
			this.date = date;
			this.position = position;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Geohash))
				return false;
			Geohash other = (Geohash)obj;
			return other.Position.Equals(Position) && other.Date.Equals(Date);
		}

		public override int GetHashCode()
		{
			return Position.GetHashCode() ^ Date.GetHashCode();
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

		public static async Task<Geohash> Get(GeoCoordinate position, DateTime date, GeohashMode geohashMode = GeohashMode.CurrentGraticule)
		{
			int[] deltas = geohashMode == GeohashMode.CurrentGraticule ? new[] { 0 } : new[] { -1, 0, 1 };

			GeoCoordinate nearestHash = position; // Will get overwritten anyways,
			double distance = Double.MaxValue;    // because of this
			foreach (int dx in deltas)
				foreach (int dy in deltas)
				{
					GeoCoordinate newCoordinate = new GeoCoordinate(position.Latitude - dx, position.Longitude - dy);
					string[] appendices = calculateAppendices(date, await Djia.Get(convertDate30W(date, newCoordinate)));
					string latStr = (int)newCoordinate.Latitude + "." + appendices[0];
					string lonStr = (int)newCoordinate.Longitude + "." + appendices[1];
					double latitude = Convert.ToDouble(latStr, CultureInfo.InvariantCulture);
					double longitude = Convert.ToDouble(lonStr, CultureInfo.InvariantCulture);
					GeoCoordinate newHash = new GeoCoordinate(latitude, longitude);
					double newDistance = position.GetDistanceTo(newHash);
					if (newDistance < distance)
					{
						nearestHash = newHash;
						distance = newDistance;
					}
				}

			return new Geohash(date, nearestHash);
		}

		private static DateTime convertDate30W(DateTime date, GeoCoordinate position)
		{
			return position.Longitude > -30 && date.CompareTo(Rule30WValidityStart) >= 0 ? date.Subtract(TimeSpan.FromDays(1)) : date;
		}

		private static string[] calculateAppendices(DateTime date, string djia)
		{
			string dateString = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
			string fullString = dateString + '-' + djia;
			byte[] result = MD5.Digest(fullString);

			ulong part1 = ((ulong)result[0x0] << 0x38)
				+ ((ulong)result[0x1] << 0x30)
				+ ((ulong)result[0x2] << 0x28)
				+ ((ulong)result[0x3] << 0x20)
				+ ((ulong)result[0x4] << 0x18)
				+ ((ulong)result[0x5] << 0x10)
				+ ((ulong)result[0x6] << 0x08)
				+ ((ulong)result[0x7] << 0x00);
			ulong part2 = ((ulong)result[0x8] << 0x38)
				+ ((ulong)result[0x9] << 0x30)
				+ ((ulong)result[0xA] << 0x28)
				+ ((ulong)result[0xB] << 0x20)
				+ ((ulong)result[0xC] << 0x18)
				+ ((ulong)result[0xD] << 0x10)
				+ ((ulong)result[0xE] << 0x08)
				+ ((ulong)result[0xF] << 0x00);
			string appendix1 = ((part1 / 2.0) / (long.MaxValue + (ulong)1)).ToString(CultureInfo.InvariantCulture).Substring("0.".Length); // Some tricks are required to divide by ulong.MaxValue + 1
			string appendix2 = ((part2 / 2.0) / (long.MaxValue + (ulong)1)).ToString(CultureInfo.InvariantCulture).Substring("0.".Length);

			return new[] { appendix1, appendix2 };
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
