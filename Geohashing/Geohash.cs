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
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
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
		private static readonly DateTime Rule30WValidityStart = DateTime.Parse("2008-05-27");

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

		private Geohash(DateTime date, GeoCoordinate position)
		{
			this.date=date;
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
			bool use30wRule = date.CompareTo(Rule30WValidityStart) >= 0 && position.Longitude > -30;

			string djia = await Djia.Get(use30wRule ? date.Subtract(TimeSpan.FromDays(1)) : date);
			string dateString = date.ToString("yyyy-MM-dd");
			byte[] hashInput = Encoding.UTF8.GetBytes(dateString + '-' + djia);
			IDigest digest = new MD5Digest();
			digest.BlockUpdate(hashInput, 0, hashInput.Length);
			byte[] result = new byte[16];
			digest.DoFinal(result, 0);

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
			string appendix1 = ((part1 / 2.0) / (long.MaxValue + (ulong)1)).ToString().Substring("0.".Length); // Some tricks are required to divide by ulong.MaxValue + 1
			string appendix2 = ((part2 / 2.0) / (long.MaxValue + (ulong)1)).ToString().Substring("0.".Length);
			string latStr = (int)position.Latitude + "." + appendix1;
			string lonStr = (int)position.Longitude + "." + appendix2;
			double latitude = Convert.ToDouble(latStr, CultureInfo.InvariantCulture);
			double longitude = Convert.ToDouble(lonStr, CultureInfo.InvariantCulture);

			return new Geohash(date, new GeoCoordinate(latitude, longitude));
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
