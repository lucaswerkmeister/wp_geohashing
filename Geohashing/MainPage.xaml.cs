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
using System.Windows;
using Windows.Devices.Geolocation;

namespace Geohashing
{
	public partial class MainPage : PhoneApplicationPage
	{
		MapLayer currentLocationLayer = new MapLayer();
		MapLayer geohashLayer = new MapLayer();
		Geolocator locator;
		Geohash geohash;

		GeoCoordinate lastMapHold;

		public MainPage()
		{
			InitializeComponent();

			map.Layers.Add(geohashLayer);
			map.Layers.Add(currentLocationLayer);
			locator = new Geolocator();
			locator.DesiredAccuracy = PositionAccuracy.High;
			locator.MovementThreshold = 100;
			locator.PositionChanged += updateCurrentLocation;

			PointMapToCurrentGeohash();
		}

		private void updateCurrentLocation(Geolocator g, PositionChangedEventArgs e)
		{
			Dispatcher.BeginInvoke(() =>
			{
				currentLocationLayer.Clear();
				System.Device.Location.GeoCoordinate coords = new System.Device.Location.GeoCoordinate(e.Position.Coordinate.Latitude, e.Position.Coordinate.Longitude);
				currentLocationLayer.Add(new MapOverlay
				{
					GeoCoordinate = coords,
					Content = new Pushpin
					{
						GeoCoordinate = coords
					},
					PositionOrigin = new Point(0, 1)
				});
			});
		}

		public async void PointMapToCurrentGeohash()
		{
			progressText.Text = "Loading hash...";
			progressText.Visibility = progressBar.Visibility = Visibility.Visible;

			try
			{
				LoadGeohash((await locator.GetGeopositionAsync()).Coordinate.Convert(), DateTime.Now);

				progressText.Visibility = progressBar.Visibility = Visibility.Collapsed;
			}
			catch (NoGeohashException)
			{
				progressBar.Visibility = Visibility.Collapsed;
				progressText.Text = "Unable to load hash";
			}
		}

		public async void LoadGeohash(GeoCoordinate position, DateTime date)
		{
			progressText.Dispatcher.BeginInvoke(() =>
			{
				progressText.Text = "Loading hash...";
				progressText.Visibility = progressBar.Visibility = Visibility.Visible;
			});

			try
			{
				geohash = await Geohash.Get(position, date);

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

				map.SetView(new LocationRectangle(geohash.Position, 2, 2), MapAnimationKind.Parabolic);

				progressText.Dispatcher.BeginInvoke(()=>{
				progressText.Visibility = progressBar.Visibility = Visibility.Collapsed;});
			}
			catch (NoGeohashException)
			{
				progressText.Dispatcher.BeginInvoke(() =>
				{
					progressBar.Visibility = Visibility.Collapsed;
					progressText.Text = "Unable to load hash";
				});
			}
		}

		private void Reload_Click(object sender, EventArgs e)
		{
			PointMapToCurrentGeohash();
		}

		private void Settings_Click(object sender, EventArgs e)
		{
			NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
		}

		private void changeGraticule(object sender, System.Windows.Input.GestureEventArgs e)
		{
			LoadGeohash(lastMapHold, DateTime.Now);
		}

		private void map_Hold(object sender, System.Windows.Input.GestureEventArgs e)
		{
			lastMapHold = map.ConvertViewportPointToGeoCoordinate(e.GetPosition(map));
		}
	}
}