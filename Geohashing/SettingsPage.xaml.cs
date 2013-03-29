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
	public enum GeoPrecision
	{
		[Description("Disabled")]
		Disabled,
		[Description("Default precision")]
		Default,
		[Description("High precision")]
		High
	}
	
	public partial class SettingsPage : PhoneApplicationPage
	{
		private const string geoPrecisionSettingName = "GeoPrecision";
		private const string autoZoomSettingName = "AutoZoom";

		private const GeoPrecision geoPrecisionSettingDefault = GeoPrecision.Default;
		private const bool autoZoomSettingDefault = true;

		public static event EventHandler<SettingChangedEventArgs> GeoPrecisionChanged;
		public static event EventHandler<SettingChangedEventArgs> AutoZoomChanged;

		private static IsolatedStorageSettings isolatedStore = System.ComponentModel.DesignerProperties.IsInDesignTool ? null : IsolatedStorageSettings.ApplicationSettings;

		public static GeoPrecision Localizing
		{
			get
			{
				return GetValueOrDefault(geoPrecisionSettingName, geoPrecisionSettingDefault);
			}
			set
			{
				GeoPrecision oldValue = GetValueOrDefault(geoPrecisionSettingName, geoPrecisionSettingDefault);
				if (AddOrUpdateValue(geoPrecisionSettingName, value))
				{
					Save();
					GeoPrecisionChanged(oldValue, new SettingChangedEventArgs(geoPrecisionSettingName));
				}
			}
		}

		public static bool AutoZoom
		{
			get
			{
				return GetValueOrDefault(autoZoomSettingName, autoZoomSettingDefault);
			}
			set
			{
				bool oldValue = GetValueOrDefault(autoZoomSettingName, autoZoomSettingDefault);
				if (AddOrUpdateValue(autoZoomSettingName, value))
				{
					Save();
					AutoZoomChanged(oldValue, new SettingChangedEventArgs(autoZoomSettingName));
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
		/// Updates a setting value, adding it if it is not present.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <param name="value">The setting value.</param>
		/// <returns><code>true</code> if the setting was changed, <code>false</code> otherwise.</returns>
		public static bool AddOrUpdateValue(string key, object value)
		{
			// If the key exists
			if (isolatedStore.Contains(key))
			{
				// If the value has changed
				if (isolatedStore[key] != value)
				{
					// Store the new value
					isolatedStore[key] = value;
					return true;
				}
			}
			// Otherwise create the key.
			else
			{
				isolatedStore.Add(key, value);
				return true;
			}

			return false;
		}


		/// <summary>
		/// Gets the current value of the setting or a default value if the setting is not present.
		/// </summary>
		/// <typeparam name="T">The value type.</typeparam>
		/// <param name="key">The setting key.</param>
		/// <param name="defaultValue">The default value of the setting.</param>
		/// <returns>The setting value, or (if not present) the default value.</returns>
		public static T GetValueOrDefault<T>(string key, T defaultValue)
		{
			// If the key exists, retrieve the value.
			if (isolatedStore.Contains(key))
				return (T)isolatedStore[key];
			// Otherwise, use the default value.
			else
				return defaultValue;
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
			return (int)(GeoPrecision)value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return Enum.GetValues(typeof(GeoPrecision)).GetValue((int)value);
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