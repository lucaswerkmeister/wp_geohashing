using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Geohashing.Resources;
using Microsoft.Phone.Maps.Controls;
using System.Windows.Shapes;
using System.Windows.Media;
using Microsoft.Phone.Maps.Toolkit;
using Windows.Devices.Geolocation;

namespace Geohashing
{
	public partial class MainPage : PhoneApplicationPage
	{
		MapLayer currentLocationLayer;
		Geolocator locator;

		public MainPage()
		{
			InitializeComponent();

			PointMapToCurrentGeohash();

			currentLocationLayer = new MapLayer();
			locator = new Geolocator();
			locator.DesiredAccuracy = PositionAccuracy.High;
			locator.MovementThreshold = 100;
			locator.PositionChanged += updateCurrentLocation;
			map.Layers.Add(currentLocationLayer);
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

		// Map Layer code from http://wp.qmatteoq.com/maps-in-windows-phone-8-and-phone-toolkit-a-winning-team-part-1/
		public async void PointMapToCurrentGeohash()
		{
			Geohash geohash = await Geohash.Get();

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
	}
}