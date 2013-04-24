using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Scheduler;
using Microsoft.Phone.Shell;
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
using System.Collections.Generic;
using System.Device.Location;
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Geohashing
{
	public static class Tiles
	{
		public static async Task UpdateAll()
		{
			IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication();
			if (!store.DirectoryExists("/Shared/ShellContent"))
				store.CreateDirectory("/Shared/ShellContent");
			await Task.WhenAll(ShellTile.ActiveTiles
				.Where((tile) => tile.NavigationUri.ToString().Contains("?")) // exclude the primary tile
				.Select(tile => updateTile(tile)));
			foreach (string file in store.GetFileNames("/Shared/ShellContent/*")
				.Where((fileName) =>
					DateTime.Now.Date.Subtract(TimeSpan.FromDays(3)).CompareTo(
					DateTime.Parse(fileName.Substring(0, "yyyy-MM-dd".Length), CultureInfo.InvariantCulture))
					>= 0))
				store.DeleteFile("/Shared/ShellContent/" + file); // Delete all tile images older than 3 days
		}

		public static void CreateOrUpdate(double latitude, double longitude, GeohashMode hashMode, MapCartographicMode mapMode)
		{
			Uri uri = new Uri("/MainPage.xaml"
				+ "?lat=" + latitude.ToString(CultureInfo.InvariantCulture)
				+ "&lon=" + longitude.ToString(CultureInfo.InvariantCulture)
				+ "&hashmode=" + hashMode.ToString()
				+ "&mapmode=" + mapMode.ToString()
				, UriKind.Relative);
			createOrUpdateTile(uri);
			AddUpdater();
		}

		public static void AddUpdater()
		{
			if (ScheduledActionService.Find("tileUpdater") == null)
				ScheduledActionService.Add(new PeriodicTask("tileUpdater") { Description = "Updates the secondary live tiles." });
#if DEBUG
			ScheduledActionService.LaunchForTest("tileUpdater", TimeSpan.FromSeconds(0));
#endif
		}

		public static void RemoveUpdater()
		{
			if (ScheduledActionService.Find("tileUpdater") != null)
				ScheduledActionService.Remove("tileUpdater");
		}

		private static async void createOrUpdateTile(Uri uri)
		{
			if (!ShellTile.ActiveTiles.Any((existing) => existing.NavigationUri.Equals(uri)))
				ShellTile.Create(uri, await createTileData(uri), false);
			else
				ShellTile.ActiveTiles.First((existing) => existing.NavigationUri.Equals(uri)).Update(await createTileData(uri));
		}

		private static async Task<FlipTileData> createTileData(Uri tileUri)
		{
			string query = tileUri.ToString().Substring(tileUri.ToString().IndexOf('?') + 1);
			string[] parts = query.Split('&');
			string key = "";
			IEnumerable<string> filter = from part in parts
										 where part.Split('=')[0] == key
										 select part.Split('=')[1];
			key = "lat";
			double lat = Double.Parse(filter.First(), CultureInfo.InvariantCulture);
			key = "lon";
			double lon = Double.Parse(filter.First(), CultureInfo.InvariantCulture);
			key = "mapmode";
			MapCartographicMode mapmode = (MapCartographicMode)Enum.Parse(typeof(MapCartographicMode), filter.DefaultIfEmpty(MapCartographicMode.Road.ToString()).First());
			key = "hashmode";
			GeohashMode hashmode = (GeohashMode)Enum.Parse(typeof(GeohashMode), filter.DefaultIfEmpty(GeohashMode.Nearest.ToString()).First());

			string title = "Geohash: " + lat.ToString("F2", CultureInfo.CurrentCulture) + ", " + lon.ToString("F2", CultureInfo.CurrentCulture);
			GeoCoordinate location = new GeoCoordinate(lat, lon);

			try
			{
				Geohash hash = await Geohash.Get(location, DateTime.Now, hashmode);

				const int imgSize = 210;

				string mapModeForRequest = new Dictionary<MapCartographicMode, string>
				{
					{MapCartographicMode.Hybrid, "AerialWithLabels"},
					{MapCartographicMode.Road, "Road"},
					{MapCartographicMode.Aerial, "Aerial"},
					{MapCartographicMode.Terrain, "AerialWithLabels"} // not supported by bing maps
				}[mapmode];
				string mapRequest = "http://dev.virtualearth.net/REST/v1/Imagery/Map/"
					+ mapModeForRequest
					+ "?pushpin=" + location.Latitude.ToString(CultureInfo.InvariantCulture) + "," + location.Longitude.ToString(CultureInfo.InvariantCulture) + ";0" // Icon style 0 is a star similar to the one on the "pin to start" button
					+ "&pushpin=" + hash.Position.Latitude.ToString(CultureInfo.InvariantCulture) + "," + hash.Position.Longitude.ToString(CultureInfo.InvariantCulture) + ";21" // Icon style 21 is a downwards-pointing arrow inside a speech bubble
					+ "&mapSize=" + imgSize + "," + imgSize
					+ "&format=png"
					+ "&key=" + PrivateResources.BingMapsKey;

				IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication();
				string filename = "/Shared/ShellContent/" + hash.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + '-' + location.Latitude.ToString("F2", CultureInfo.InvariantCulture) + "," + location.Longitude.ToString("F2", CultureInfo.InvariantCulture) + "-" + mapModeForRequest + ".png";
				if (!store.FileExists(filename))
					using (IsolatedStorageFileStream stream = store.OpenFile(filename, FileMode.OpenOrCreate))
						(await HttpWebRequest.Create(mapRequest).GetResponseAsync()).GetResponseStream().CopyTo(stream);

				Settings settings = new Settings();
				return new FlipTileData
				{
					Title = title,
					BackgroundImage = new Uri("isostore:" + filename, UriKind.Absolute),
					BackTitle = title,
					BackContent = "Geohash is at " + settings.CoordinateToString(hash.Position) + "\n"
									+ "Distance: " + settings.LengthToString(hash.Position.GetDistanceTo(location))
				};
			}
			catch (Exception)
			{
				return new FlipTileData
				{
					Title = title,
					BackTitle = title
				};
			}
		}

		private static async Task updateTile(ShellTile tile)
		{
			tile.Update(await createTileData(tile.NavigationUri));
		}
	}
}
