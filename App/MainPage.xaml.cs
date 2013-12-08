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
using Geohashing.Resources;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Maps.Toolkit;
using Microsoft.Phone.Scheduler;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using System;
using System.Device.Location;
using System.Globalization;
using System.Linq;
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

			BuildLocalizedApplicationBar();

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
			if (e.NavigationMode == System.Windows.Navigation.NavigationMode.New && NavigationContext.QueryString.ContainsKey("lat") && NavigationContext.QueryString.ContainsKey("lon"))
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

		private void BuildLocalizedApplicationBar()
		{
			ApplicationBar = new ApplicationBar { Mode = ApplicationBarMode.Minimized, IsVisible = true, IsMenuEnabled = true };

			ApplicationBarIconButton relocateButton = new ApplicationBarIconButton { Text = AppResources.ReloadLocationButtonText, IconUri = new Uri("/Images/feature.search.png", UriKind.Relative) };
			ApplicationBarIconButton settingsButton = new ApplicationBarIconButton { Text = AppResources.SettingsAboutButtonText, IconUri = new Uri("/Images/feature.settings.png", UriKind.Relative) };
			ApplicationBarIconButton pinButton = new ApplicationBarIconButton { Text = AppResources.PinStartScreenButtonText, IconUri = new Uri("/Images/favs.png", UriKind.Relative) };
			ApplicationBarIconButton sendToMapsButton = new ApplicationBarIconButton { Text = AppResources.OpenInMapsButtonText, IconUri = new Uri("/Images/next.png", UriKind.Relative) };

			relocateButton.Click += Relocate_Click;
			settingsButton.Click += Settings_Click;
			pinButton.Click += Pin_Click;
			sendToMapsButton.Click += Goto_Click;

			ApplicationBar.Buttons.Add(relocateButton);
			ApplicationBar.Buttons.Add(settingsButton);
			ApplicationBar.Buttons.Add(pinButton);
			ApplicationBar.Buttons.Add(sendToMapsButton);
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
					Task.Run(async () =>
					{
						await Task.Delay(TimeSpan.FromSeconds(5));
						Dispatcher.BeginInvoke(() =>
							SystemTray.SetProgressIndicator(this, new ProgressIndicator
							{
								IsVisible = false
							}));
					});
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
				if (geohash != null)
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
				if (geohash != null)
				{
					graticulePolygon.Path = CreateRectangle(geohash.Graticule);
					map.MapElements.Add(graticulePolygon);
				}
			});
		}
		private void updateInfoLayer()
		{
			Dispatcher.BeginInvoke(() =>
			{
				info.Text = geohash == null ? "" :
							AppResources.OverlayPositionPrefix + ": " + settings.CoordinateToString(coordinate) + "\n" +
							AppResources.OverlayGeohashPrefix + ": " + settings.CoordinateToString(geohash.Position) + "\n" +
							AppResources.OverlayDistancePrefix + ": " + settings.LengthToString(coordinate.GetDistanceTo(geohash.Position));
			});
		}

		private void focus()
		{
			Dispatcher.BeginInvoke(() =>
			{
				if (settings.AutoZoom)
					map.SetView(LocationRectangle.CreateBoundingRectangle(geohash.Position, coordinate), MapAnimationKind.Parabolic);
			});
		}
		#endregion

		public async Task UpdateCurrentLocation()
		{
			startTask(AppResources.GettingLocationText);

			try
			{
				coordinate = (await new Geolocator { DesiredAccuracy = PositionAccuracy.Default }.GetGeopositionAsync(TimeSpan.Zero, TimeSpan.FromSeconds(10))).Coordinate.Convert();
				redrawLocationPin();

				endTask();

				await PointMapToCurrentGeohash();
			}
			catch (Exception)
			{
				endTask(AppResources.CantLocateText);
			}
		}

		public async Task PointMapToCurrentGeohash()
		{
			if (coordinate == null)
				return;

			startTask(AppResources.LoadingHashText);

			try
			{
				await LoadGeohash(coordinate, Date);

				endTask();
			}
			catch (NoDjiaException e)
			{
				string message = AppResources.CantLoadHashPrefix;
				switch (e.Cause)
				{
					case NoDjiaException.NoDjiaCause.NotAvailable:
						message += ": " + AppResources.NoDjiaSuffix; break;
					case NoDjiaException.NoDjiaCause.NoInternet:
						message += ": " + AppResources.NoInternetSuffix; break;
				}
				endTask(message);
			}
		}

		public async Task LoadGeohash(GeoCoordinate position, DateTime date)
		{
			try
			{
				geohash = await Geohash.Get(position, date, threadSafeSettings.HashMode);

				focus();
			}
			catch (NoDjiaException e)
			{
				geohash = null;
				throw e;
			}
			finally
			{
				redrawGeohashPin();
				redrawGraticuleOutline();
				updateInfoLayer();
			}
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

		private async void Pin_Click(object sender, EventArgs e)
		{
			if (coordinate == null)
				return;
			Uri uri = Tiles.MakeUri(coordinate.Latitude, coordinate.Longitude, settings.HashMode, settings.CartographicMode);
			if (!ShellTile.ActiveTiles.Any((existing) => existing.NavigationUri.Equals(uri)))
			{
				startTask(AppResources.PreparingTileText);
				FlipTileData tileData = await Tiles.CreateTileData(uri);
				endTask();
				ShellTile.Create(uri, tileData, false);
			}
			if (ScheduledActionService.Find("tileUpdater") == null)
				ScheduledActionService.Add(new PeriodicTask("tileUpdater") { Description = "Updates the secondary live tiles." });
		}

		private void Goto_Click(object sender, EventArgs e)
		{
			if (geohash == null)
				return;
			new MapsDirectionsTask
			{
				End = new LabeledMapLocation(AppResources.MapsAppLabel
					.Replace("yyyy", Date.Year.ToString("D4", CultureInfo.CurrentUICulture))
					.Replace("MM", Date.Month.ToString("D2", CultureInfo.CurrentUICulture))
					.Replace("dd", Date.Day.ToString("D2", CultureInfo.CurrentUICulture))
					.Replace("%LAT%", geohash.Graticule.North.ToString(CultureInfo.CurrentUICulture))
					.Replace("%LON%", geohash.Graticule.West.ToString(CultureInfo.CurrentUICulture)),
					geohash.Position)
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

		private void map_Loaded(object sender, RoutedEventArgs e)
		{
			Microsoft.Phone.Maps.MapsSettings.ApplicationContext.ApplicationId = PrivateResources.MicrosoftMapServiceApplicationID;
			Microsoft.Phone.Maps.MapsSettings.ApplicationContext.AuthenticationToken = PrivateResources.MicrosoftMapServiceAuthToken;
		}
	}
}
