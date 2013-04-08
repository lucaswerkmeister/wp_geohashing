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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace Geohashing
{
	public partial class SettingsPage : PhoneApplicationPage
	{
		public SettingsPage()
		{
			InitializeComponent();

			Settings settings = new Settings();

			cartographicModeListPicker.ItemsSource = Enum.GetValues(typeof(MapCartographicMode));
			cartographicModeListPicker.SelectedItem = settings.CartographicMode;

			Array rawValues = Enum.GetValues(typeof(GeohashMode));
			List<string> values = new List<string>();
			foreach (GeohashMode e in rawValues)
				values.Add((typeof(GeohashMode).GetMember(e.ToString())[0].GetCustomAttributes(typeof(DescriptionAttribute), false)[0] as DescriptionAttribute).Description);
			geohashModeListPicker.ItemsSource = values;
			geohashModeListPicker.SelectedItem = (typeof(GeohashMode).GetMember(settings.HashMode.ToString())[0].GetCustomAttributes(typeof(DescriptionAttribute), false)[0] as DescriptionAttribute).Description;
		}

		private void DjiaBufferSizeTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
		{
			TextBox t = (TextBox)sender;
			if (t.Text.Contains(".") || t.Text.Contains(","))
			{
				t.Text = t.Text.Replace(".", "").Replace(",", "");
				t.SelectionStart = t.Text.Length;
			}
		}
	}
	public class CartographicModeIntConverter : IValueConverter
	{

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (int)(MapCartographicMode)value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Enum.GetValues(typeof(MapCartographicMode)).GetValue((int)value);
		}
	}
	public class GeohashModeIntConverter : IValueConverter
	{

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (int)(GeohashMode)value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Enum.GetValues(typeof(GeohashMode)).GetValue((int)value);
		}
	}

}