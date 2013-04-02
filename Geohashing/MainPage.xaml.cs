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
using Microsoft.Phone.Tasks;
using System;
using System.Device.Location;
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

		private GeoCoordinate lastMapHold;

		public MainPage()
		{
			Date = DateTime.Now;

			InitializeComponent();

			map.Layers.Add(geohashLayer);
			map.Layers.Add(currentLocationLayer);
			map.CartographicMode = settings.CartographicMode;
			Settings.CartographicModeChanged += (sender, e) => Dispatcher.BeginInvoke(() => map.CartographicMode = settings.CartographicMode);

			if (settings.Localize)
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
			catch (NoGeohashException e)
			{
				string message = "Unable to load hash";
				switch (e.Cause)
				{
					case NoGeohashException.NoGeohashCause.NoDjia:
						message += ": DJIA N/A"; break;
					case NoGeohashException.NoGeohashCause.NoInternet:
						message += ": No internet connection"; break;
				}
				endTask(message);
			}
		}

		public async Task<bool> LoadGeohash(GeoCoordinate position, DateTime date)
		{
			geohash = await Geohash.Get(position, date);

			redrawGeohashPin();
			redrawGraticuleOutline();

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

		private void Goto_Click(object sender, EventArgs e)
		{
			new MapsDirectionsTask
			{
				End = new LabeledMapLocation("Geohash for " + Date.ToString("yyyy-MM-dd") + ' ' + geohash.Graticule.North + ", " + geohash.Graticule.West, geohash.Position)
			}.Show();
		}

		private void changeGraticule(object sender, System.Windows.Input.GestureEventArgs e)
		{
			coordinate = lastMapHold;
			LoadGeohash(lastMapHold, Date);
		}

		private void map_Hold(object sender, System.Windows.Input.GestureEventArgs e)
		{
			lastMapHold = map.ConvertViewportPointToGeoCoordinate(e.GetPosition(map));
		}

		private void dateChanged(object sender, EventArgs e)
		{
			DateTime oldValue = Date;
			DateTime? value = datePicker.Value;
			Date = value == null ? DateTime.Now : (DateTime)value;
			if (Date.Date != oldValue.Date)
				PointMapToCurrentGeohash();
		}

		public static GeoCoordinateCollection CreateRectangle(LocationRectangle rect)
		{
			GeoCoordinateCollection ret = new GeoCoordinateCollection();

			ret.Add(rect.Northwest);
			ret.Add(rect.Northeast);
			ret.Add(rect.Southeast);
			ret.Add(rect.Southwest);

			return ret;
		}
	}
}