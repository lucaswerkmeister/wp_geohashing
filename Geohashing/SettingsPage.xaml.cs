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
		private const string geoPrecisionSettingName = "GeoPrecision";

		private const GeoPrecision geoPrecisionSettingDefault = GeoPrecision.Default;

		public delegate void SettingChangedEventHandler(object sender, SettingChangedEventArgs e);
		public static event SettingChangedEventHandler GeoPrecisionChanged;

		private static IsolatedStorageSettings isolatedStore = System.ComponentModel.DesignerProperties.IsInDesignTool ? null : IsolatedStorageSettings.ApplicationSettings;

		public enum GeoPrecision
		{
			[Description("Disabled")]
			Disabled,
			[Description("Default precision")]
			Default,
			[Description("High precision")]
			High
		}
		public static GeoPrecision Localizing
		{
			get
			{
				return GetValueOrDefault(geoPrecisionSettingName, geoPrecisionSettingDefault);
			}
			set
			{
				GeoPrecision oldValue = GetValueOrDefault(geoPrecisionSettingName, geoPrecisionSettingDefault);
				if (oldValue != value)
				{
					AddOrUpdateValue(geoPrecisionSettingName, value);
					Save();
					GeoPrecisionChanged(oldValue, new SettingChangedEventArgs(geoPrecisionSettingName));
				}
			}
		}

		public SettingsPage()
		{
			InitializeComponent();

			Array rawValues = Enum.GetValues(typeof(GeoPrecision));
			List<string> values = new List<string>();
			foreach (GeoPrecision p in rawValues)
				values.Add((typeof(GeoPrecision).GetMember(p.ToString())[0].GetCustomAttributes(typeof(DescriptionAttribute), false)[0] as DescriptionAttribute).Description);
			localizing.ItemsSource = values;
		}

		/// <summary>
		/// Update a setting value. If the setting does not exist, then add the setting.
		/// </summary>
		/// <param name="Key"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static bool AddOrUpdateValue(string Key, Object value)
		{
			bool valueChanged = false;

			// If the key exists
			if (isolatedStore.Contains(Key))
			{
				// If the value has changed
				if (isolatedStore[Key] != value)
				{
					// Store the new value
					isolatedStore[Key] = value;
					valueChanged = true;
				}
			}
			// Otherwise create the key.
			else
			{
				isolatedStore.Add(Key, value);
				valueChanged = true;
			}

			return valueChanged;
		}


		/// <summary>
		/// Get the current value of the setting, or if it is not found, set the setting to the default setting.
		/// </summary>
		/// <typeparam name="valueType"></typeparam>
		/// <param name="Key"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public static valueType GetValueOrDefault<valueType>(string Key, valueType defaultValue)
		{
			valueType value;

			// If the key exists, retrieve the value.
			if (isolatedStore.Contains(Key))
			{
				value = (valueType)isolatedStore[Key];
			}
			// Otherwise, use the default value.
			else
			{
				value = defaultValue;
			}

			return value;
		}


		/// <summary>
		/// Save the settings.
		/// </summary>
		public static void Save()
		{
			isolatedStore.Save();
		}
	}
	public class GeoPrecisionToIntConverter : IValueConverter
	{

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return (int)(Geohashing.SettingsPage.GeoPrecision)value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return Enum.GetValues(typeof(Geohashing.SettingsPage.GeoPrecision)).GetValue((int)value);
		}
	}

	public class SettingChangedEventArgs : EventArgs
	{
		public string SettingName { get; private set; }

		public SettingChangedEventArgs(string settingName)
		{
			this.SettingName = settingName;
		}
	}
}