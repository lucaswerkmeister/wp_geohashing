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
using System;
using System.Device.Location;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Windows.Devices.Geolocation;

namespace Geohashing
{
	public partial class MainPage : PhoneApplicationPage
	{
		private MapLayer currentLocationLayer = new MapLayer();
		private MapLayer geohashLayer = new MapLayer();
		private Geohash geohash;
		private GeoCoordinate coordinate;

		private Settings settings = new Settings();

		private GeoCoordinate lastMapHold;

		public MainPage()
		{
			InitializeComponent();

			map.Layers.Add(geohashLayer);
			map.Layers.Add(currentLocationLayer);

			new Thread(() =>
				UpdateCurrentLocation()
				).Start(); // Apparently, doing this from the constructor thread isn't allowed (Dispatcher neither)
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
				progressText.Visibility = message == String.Empty ? Visibility.Collapsed : Visibility.Visible;
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
						GeoCoordinate = coordinate
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
						GeoCoordinate = geohash.Position
					},
					PositionOrigin = new Point(0, 1)
				});
			});
		}

		private void focus()
		{
			Dispatcher.BeginInvoke(() =>
				map.SetView(new LocationRectangle(geohash.Position, 2, 2), MapAnimationKind.Parabolic));
		}
		#endregion

		public async void UpdateCurrentLocation()
		{
			startTask("Getting location...");

			coordinate = (await new Geolocator { DesiredAccuracy = PositionAccuracy.Default }.GetGeopositionAsync()).Coordinate.Convert();
			redrawLocationPin();

			endTask();

			PointMapToCurrentGeohash();
		}

		public async void PointMapToCurrentGeohash()
		{
			startTask("Loading hash...");

			try
			{
				await LoadGeohash(coordinate, DateTime.Now);

				endTask();
			}
			catch (NoGeohashException e)
			{
				endTask("Unable to load hash" + (e.Message.Contains("DJIA for") && e.Message.Contains("not available yet") ? ": DJIA N/A" : String.Empty));
			}
		}

		public async Task<bool> LoadGeohash(GeoCoordinate position, DateTime date)
		{
			geohash = await Geohash.Get(position, date);

			redrawGeohashPin();

			if (settings.AutoZoom)
				focus();

			return true;
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

		private void changeGraticule(object sender, System.Windows.Input.GestureEventArgs e)
		{
			coordinate = lastMapHold;
			LoadGeohash(lastMapHold, DateTime.Now);
		}

		private void map_Hold(object sender, System.Windows.Input.GestureEventArgs e)
		{
			lastMapHold = map.ConvertViewportPointToGeoCoordinate(e.GetPosition(map));
		}
	}
}