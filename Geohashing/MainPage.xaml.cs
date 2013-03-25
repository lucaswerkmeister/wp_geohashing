using Microsoft.Phone.Controls;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Maps.Toolkit;
using System;
using System.Windows;
using Windows.Devices.Geolocation;

namespace Geohashing
{
	public partial class MainPage : PhoneApplicationPage
	{
		MapLayer currentLocationLayer;
		Geolocator locator;
		Geohash geohash;

		public MainPage()
		{
			InitializeComponent();

			currentLocationLayer = new MapLayer();
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
				LoadGeohash((await locator.GetGeopositionAsync()).Coordinate, DateTime.Now);

				progressText.Visibility = progressBar.Visibility = Visibility.Collapsed;
			}
			catch (NoGeohashException)
			{
				progressBar.Visibility = Visibility.Collapsed;
				progressText.Text = "Unable to load hash";
			}
		}

		public async void LoadGeohash(Geocoordinate position, DateTime date)
		{
			geohash = await Geohash.Get(position, date);

			MapOverlay overlay = new MapOverlay
			{
				GeoCoordinate = geohash.Position,
				Content = new Pushpin
				{
					GeoCoordinate = geohash.Position
				},
				PositionOrigin = new Point(0, 1)
			};
			MapLayer layer = new MapLayer();
			layer.Add(overlay);
			map.Layers.Add(layer);

			map.SetView(new LocationRectangle(geohash.Position, 2, 2), MapAnimationKind.Parabolic);
		}

		private void Reload_Click(object sender, EventArgs e)
		{
			PointMapToCurrentGeohash();
		}
	}
}