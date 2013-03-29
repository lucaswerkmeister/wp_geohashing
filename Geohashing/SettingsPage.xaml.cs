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
using System;
using System.ComponentModel;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Data;
using System.IO.IsolatedStorage;

namespace Geohashing
{
	public partial class SettingsPage : PhoneApplicationPage
	{
		public SettingsPage()
		{
			InitializeComponent();

			Array rawValues = Enum.GetValues(typeof(GeoPrecision));
			List<string> values = new List<string>();
			foreach (GeoPrecision p in rawValues)
				values.Add((typeof(GeoPrecision).GetMember(p.ToString())[0].GetCustomAttributes(typeof(DescriptionAttribute), false)[0] as DescriptionAttribute).Description);
			localizing.ItemsSource = values;
		}
	}
	public class GeoPrecisionToIntConverter : IValueConverter
	{

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return (int)(GeoPrecision)value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return Enum.GetValues(typeof(GeoPrecision)).GetValue((int)value);
		}
	}

}