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
using Microsoft.Phone.Scheduler;
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
using System.Windows.Controls;
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
			Settings.GeohashModeChanged += async (sender, e) => await PointMapToCurrentGeohash();
			Settings.CoordinatesModeChanged += (sender, e) => updateInfoLayer();
			Settings.LengthUnitChanged += (sender, e) => updateInfoLayer();
		}

		protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			if (NavigationContext.QueryString.ContainsKey("lat") && NavigationContext.QueryString.ContainsKey("lon"))
			{
				coordinate = new GeoCoordinate(Double.Parse(NavigationContext.QueryString["lat"], CultureInfo.InvariantCulture), Double.Parse(NavigationContext.QueryString["lon"], CultureInfo.InvariantCulture));
				if (NavigationContext.QueryString.ContainsKey("mode"))
					settings.CartographicMode = (MapCartographicMode)Enum.Parse(typeof(MapCartographicMode), NavigationContext.QueryString["mode"]);
			}
		}

		private async void OnLoaded(object sender, RoutedEventArgs e)
		{
			if (coordinate == null && settings.Localize)
				await UpdateCurrentLocation();
			if (coordinate != null)
			{
				redrawLocationPin();
				await PointMapToCurrentGeohash();
			}
		}

		#region UI utility methods safe to call from any thread
		private void startTask(string description)
		{
			Dispatcher.BeginInvoke(() =>
			{
				SystemTray.SetProgressIndicator(this, new ProgressIndicator
				{
					IsVisible = true,
					IsIndeterminate = true,
					Text = description
				});
			});
		}

		private void endTask(string message = "")
		{
			Dispatcher.BeginInvoke(() =>
			{
				bool hasText = !String.IsNullOrWhiteSpace(message);
				SystemTray.SetProgressIndicator(this, new ProgressIndicator
				{
					IsVisible = hasText,
					IsIndeterminate = false,
					Text = message,
					Value = 0
				});
				if (hasText)
					new Thread(() =>
					{
						Thread.Sleep(TimeSpan.FromSeconds(5));
						Dispatcher.BeginInvoke(() =>
							SystemTray.SetProgressIndicator(this, new ProgressIndicator
							{
								IsVisible = false
							}));
					}).Start();
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
		private void updateInfoLayer()
		{
			Dispatcher.BeginInvoke(() =>
			{
				info.Text = "Position: " + settings.CoordinateToString(coordinate) + "\n"
							+ "Geohash: " + settings.CoordinateToString(geohash.Position) + "\n"
							+ "Distance: " + settings.LengthToString(coordinate.GetDistanceTo(geohash.Position));
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

		public async Task UpdateCurrentLocation()
		{
			startTask("Getting location...");

			try
			{
				coordinate = (await new Geolocator { DesiredAccuracy = PositionAccuracy.Default }.GetGeopositionAsync()).Coordinate.Convert();
				redrawLocationPin();

				endTask();

				await PointMapToCurrentGeohash();
			}
			catch (Exception)
			{
				endTask("Unable to fetch location");
			}
		}

		public async Task PointMapToCurrentGeohash()
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

		public async Task LoadGeohash(GeoCoordinate position, DateTime date)
		{
			geohash = await Geohash.Get(position, date, threadSafeSettings.HashMode);

			redrawGeohashPin();
			redrawGraticuleOutline();
			updateInfoLayer();
			focus();
		}

		private async void Reload_Click(object sender, EventArgs e)
		{
			await PointMapToCurrentGeohash();
		}

		private async void Relocate_Click(object sender, EventArgs e)
		{
			await UpdateCurrentLocation();
		}

		private void Settings_Click(object sender, EventArgs e)
		{
			NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
		}

		private void Pin_Click(object sender, EventArgs e)
		{
			Tiles.CreateOrUpdate(coordinate.Latitude, coordinate.Longitude, settings.HashMode, settings.CartographicMode);
		}

		private void Goto_Click(object sender, EventArgs e)
		{
			if(geohash == null)
				return;
			new MapsDirectionsTask
			{
				End = new LabeledMapLocation("Geohash for " + Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ' ' + geohash.Graticule.North + ", " + geohash.Graticule.West, geohash.Position)
			}.Show();
		}

		private async void map_Hold(object sender, System.Windows.Input.GestureEventArgs e)
		{
			coordinate = map.ConvertViewportPointToGeoCoordinate(e.GetPosition(map));
			redrawLocationPin();
			await PointMapToCurrentGeohash();
		}

		private async void dateChanged(object sender, EventArgs e)
		{
			DateTime oldValue = Date;
			DateTime? value = datePicker.Value;
			Date = value == null ? DateTime.Now : (DateTime)value;
			if (Date.Date != oldValue.Date)
				await PointMapToCurrentGeohash();
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
