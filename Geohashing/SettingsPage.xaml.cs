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
using System.Windows.Controls;
using System.Windows.Data;

namespace Geohashing
{
	public partial class SettingsPage : PhoneApplicationPage
	{
		public SettingsPage()
		{
			InitializeComponent();

			cartographicModeListPicker.ItemsSource = Enum.GetValues(typeof(MapCartographicMode));
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

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return (int)(MapCartographicMode)value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return Enum.GetValues(typeof(MapCartographicMode)).GetValue((int)value);
		}
	}

}