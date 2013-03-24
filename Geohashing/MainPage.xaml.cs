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

namespace Geohashing
{
	public partial class MainPage : PhoneApplicationPage
	{
		// Constructor
		public MainPage()
		{
			InitializeComponent();

			PointMapToCurrentGeohash();

			// Sample code to localize the ApplicationBar
			//BuildLocalizedApplicationBar();
		}

		// Sample code for building a localized ApplicationBar
		//private void BuildLocalizedApplicationBar()
		//{
		//    // Set the page's ApplicationBar to a new instance of ApplicationBar.
		//    ApplicationBar = new ApplicationBar();

		//    // Create a new button and set the text value to the localized string from AppResources.
		//    ApplicationBarIconButton appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.add.rest.png", UriKind.Relative));
		//    appBarButton.Text = AppResources.AppBarButtonText;
		//    ApplicationBar.Buttons.Add(appBarButton);

		//    // Create a new menu item with the localized string from AppResources.
		//    ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.AppBarMenuItemText);
		//    ApplicationBar.MenuItems.Add(appBarMenuItem);
		//}

		// Map Layer code from http://wp.qmatteoq.com/maps-in-windows-phone-8-and-phone-toolkit-a-winning-team-part-1/
		public async void PointMapToCurrentGeohash()
		{
			Geohash geohash = await Geohash.Get();

			MapOverlay overlay = new MapOverlay
			{
				GeoCoordinate = geohash.Position,
				Content = new Ellipse
				{
					Fill = new SolidColorBrush(Colors.Red),
					Width = 40,
					Height = 40
				}
			};
			MapLayer layer = new MapLayer();
			layer.Add(overlay);
			map.Layers.Add(layer);
		}
	}
}