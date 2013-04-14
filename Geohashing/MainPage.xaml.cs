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
using Microsoft.Phone.Controls;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Maps.Toolkit;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Windows.Devices.Geolocation;

namespace Geohashing
{
	public partial class MainPage : PhoneApplicationPage
	{
		private MapLayer currentLocationLayer = new MapLayer();
		private MapLayer geohashLayer = new MapLayer();
		private MapPolygon graticulePolygon = new MapPolygon { FillColor = Colors.Transparent, StrokeColor = Colors.Red, StrokeThickness = 2 };
		private Geohash geohash;
		private GeoCoordinate coordinate;
		private DateTime Date
		{
			get;
			set;
		}
		private Settings settings { get { return (Settings)App.Current.Resources["settings"]; } }
		private Settings threadSafeSettings { get { return new Settings(); } }

		public MainPage()
		{
			Date = DateTime.Now;

			InitializeComponent();

			map.Layers.Add(geohashLayer);
			map.Layers.Add(currentLocationLayer);
			map.CartographicMode = settings.CartographicMode;
			Settings.CartographicModeChanged += (sender, e) => Dispatcher.BeginInvoke(() => map.CartographicMode = settings.CartographicMode);
			Settings.GeohashModeChanged += (sender, e) => PointMapToCurrentGeohash();

			if (settings.Localize)
				new Thread(() =>
					UpdateCurrentLocation()
					).Start(); // Apparently, doing this from the constructor thread isn't allowed (Dispatcher neither)
		}

		protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			if (NavigationContext.QueryString.ContainsKey("lat") && NavigationContext.QueryString.ContainsKey("lon"))
			{
				coordinate = new GeoCoordinate(Double.Parse(NavigationContext.QueryString["lat"]), Double.Parse(NavigationContext.QueryString["lon"]));
				if (NavigationContext.QueryString.ContainsKey("mode"))
					settings.CartographicMode = (MapCartographicMode)Enum.Parse(typeof(MapCartographicMode), NavigationContext.QueryString["mode"]);
				redrawLocationPin();
				PointMapToCurrentGeohash();
			}
		}

		#region UI utility methods safe to call from any thread
		private void startTask(string description)
		{
			Dispatcher.BeginInvoke(() =>
			{
				progressText.Text = description;
				progressText.Visibility = progressBar.Visibility = Visibility.Visible;
			});
		}

		private void endTask(string message = "")
		{
			Dispatcher.BeginInvoke(() =>
			{
				progressText.Text = message;
				progressBar.Visibility = Visibility.Collapsed;
				progressText.Visibility = String.IsNullOrEmpty(message) ? Visibility.Collapsed : Visibility.Visible;
			});
		}

		private void redrawLocationPin()
		{
			Dispatcher.BeginInvoke(() =>
			{
				currentLocationLayer.Clear();
				currentLocationLayer.Add(new MapOverlay
				{
					GeoCoordinate = coordinate,
					Content = new Pushpin
					{
						GeoCoordinate = coordinate,
						Style = (Style)App.Current.Resources["pushpinLocation"]
					},
					PositionOrigin = new Point(0, 1)
				});
			});
		}
		private void redrawGeohashPin()
		{
			Dispatcher.BeginInvoke(() =>
			{
				geohashLayer.Clear();
				geohashLayer.Add(new MapOverlay
				{
					GeoCoordinate = geohash.Position,
					Content = new Pushpin
					{
						GeoCoordinate = geohash.Position,
						Style = (Style)App.Current.Resources["pushpinHash"]
					},
					PositionOrigin = new Point(0, 1)
				});
			});
		}
		private void redrawGraticuleOutline()
		{
			Dispatcher.BeginInvoke(() =>
			{
				map.MapElements.Remove(graticulePolygon);
				graticulePolygon.Path = CreateRectangle(geohash.Graticule);
				map.MapElements.Add(graticulePolygon);
			});
		}

		private void focus()
		{
			Dispatcher.BeginInvoke(() =>
			{
				if (settings.AutoZoom)
					map.SetView(new LocationRectangle(geohash.Position, 2, 2), MapAnimationKind.Parabolic);
			});
		}
		#endregion

		public async void UpdateCurrentLocation()
		{
			startTask("Getting location...");

			try
			{
				coordinate = (await new Geolocator { DesiredAccuracy = PositionAccuracy.Default }.GetGeopositionAsync()).Coordinate.Convert();
				redrawLocationPin();

				endTask();

				PointMapToCurrentGeohash();
			}
			catch (Exception)
			{
				endTask("Unable to fetch location");
			}
		}

		public async void PointMapToCurrentGeohash()
		{
			if (coordinate == null)
				return;

			startTask("Loading hash...");

			try
			{
				await LoadGeohash(coordinate, Date);

				endTask();
			}
			catch (NoDjiaException e)
			{
				string message = "Unable to load hash";
				switch (e.Cause)
				{
					case NoDjiaException.NoDjiaCause.NotAvailable:
						message += ": DJIA N/A"; break;
					case NoDjiaException.NoDjiaCause.NoInternet:
						message += ": No internet connection"; break;
				}
				endTask(message);
			}
		}

		public async Task<bool> LoadGeohash(GeoCoordinate position, DateTime date)
		{
			geohash = await Geohash.Get(position, date, threadSafeSettings.HashMode);

			redrawGeohashPin();
			redrawGraticuleOutline();

			focus();

			return true;
		}

		public void UpdateTiles()
		{
			IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication();
			if (!store.DirectoryExists("/Shared/ShellContent"))
				store.CreateDirectory("/Shared/ShellContent");
			foreach (ShellTile tile in ShellTile.ActiveTiles)
				if (tile.NavigationUri.ToString().Contains("?")) // exclude the primary tile
					updateTile(tile);
			foreach (string file in store.GetFileNames("/Shared/ShellContent/*")
				.Where((fileName) =>
					DateTime.Now.Date.Subtract(TimeSpan.FromDays(3)).CompareTo(
					DateTime.Parse(fileName.Substring(0, "yyyy-MM-dd".Length), CultureInfo.InvariantCulture))
					>= 0))
				store.DeleteFile("/Shared/ShellContent/" + file); // Delete all tile images older than 3 days
		}

		private void Reload_Click(object sender, EventArgs e)
		{
			PointMapToCurrentGeohash();
		}

		private void Relocate_Click(object sender, EventArgs e)
		{
			UpdateCurrentLocation();
		}

		private void Settings_Click(object sender, EventArgs e)
		{
			NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
		}

		private void Pin_Click(object sender, EventArgs e)
		{
			Uri uri = new Uri("/MainPage.xaml"
				+ "?lat=" + coordinate.Latitude
				+ "&lon=" + coordinate.Longitude
				+ "&hashmode=" + settings.HashMode.ToString()
				+ "&mapmode=" + settings.CartographicMode.ToString()
				, UriKind.Relative);
			createOrUpdateTile(uri);
		}

		private async void createOrUpdateTile(Uri uri)
		{
			if (!ShellTile.ActiveTiles.Any((existing) => existing.NavigationUri.Equals(uri)))
				ShellTile.Create(uri, await createTileData(uri), false);
			else
				ShellTile.ActiveTiles.First((existing) => existing.NavigationUri.Equals(uri)).Update(await createTileData(uri));
		}

		private void Goto_Click(object sender, EventArgs e)
		{
			new MapsDirectionsTask
			{
				End = new LabeledMapLocation("Geohash for " + Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ' ' + geohash.Graticule.North + ", " + geohash.Graticule.West, geohash.Position)
			}.Show();
		}

		private void map_Hold(object sender, System.Windows.Input.GestureEventArgs e)
		{
			coordinate = map.ConvertViewportPointToGeoCoordinate(e.GetPosition(map));
			redrawLocationPin();
			PointMapToCurrentGeohash();
		}

		private void dateChanged(object sender, EventArgs e)
		{
			DateTime oldValue = Date;
			DateTime? value = datePicker.Value;
			Date = value == null ? DateTime.Now : (DateTime)value;
			if (Date.Date != oldValue.Date)
				PointMapToCurrentGeohash();
		}

		private async Task<FlipTileData> createTileData(Uri tileUri)
		{
			string query = tileUri.ToString().Substring(tileUri.ToString().IndexOf('?') + 1);
			string[] parts = query.Split('&');
			string key = "";
			IEnumerable<string> filter = from part in parts
										 where part.Split('=')[0] == key
										 select part.Split('=')[1];
			key = "lat";
			double lat = Double.Parse(filter.First());
			key = "lon";
			double lon = Double.Parse(filter.First());
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
					{
						WebRequest request = HttpWebRequest.Create(mapRequest);
						HttpWebResponse response = await request.GetResponseAsync();
						Stream responseStream = response.GetResponseStream();
						responseStream.CopyTo(stream);
					}

				return new FlipTileData
				{
					Title = title,
					BackgroundImage = new Uri("isostore:" + filename, UriKind.Absolute),
					BackTitle = title,
					BackContent = "Geohash is at " + hash.Position.Latitude.ToString("F2", CultureInfo.CurrentCulture) + ", " + hash.Position.Longitude.ToString("F2", CultureInfo.CurrentCulture) + "\n"
									+ "Distance: " + (hash.Position.GetDistanceTo(location) / 1000).ToString("F2", CultureInfo.CurrentCulture) + "km"
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

		private async void updateTile(ShellTile tile)
		{
			tile.Update(await createTileData(tile.NavigationUri));
		}

		public static GeoCoordinateCollection CreateRectangle(LocationRectangle rect)
		{
			if (rect == null)
				throw new ArgumentNullException("rect");

			GeoCoordinateCollection ret = new GeoCoordinateCollection();

			ret.Add(rect.Northwest);
			ret.Add(rect.Northeast);
			ret.Add(rect.Southeast);
			ret.Add(rect.Southwest);

			return ret;
		}
	}
}