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

		private IsolatedStorageSettings isolatedStore;

		public enum GeoPrecision
		{
			[Description("Disabled")]
			Disabled,
			[Description("Default precision")]
			Default,
			[Description("High precision")]
			High
		}
		public GeoPrecision Localizing
		{
			get
			{
				return GetValueOrDefault(geoPrecisionSettingName, geoPrecisionSettingDefault);
			}
			set
			{
				AddOrUpdateValue(geoPrecisionSettingName, value);
				Save();
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

			if (!System.ComponentModel.DesignerProperties.IsInDesignTool)
				isolatedStore = IsolatedStorageSettings.ApplicationSettings;
		}

		/// <summary>
		/// Update a setting value. If the setting does not exist, then add the setting.
		/// </summary>
		/// <param name="Key"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public bool AddOrUpdateValue(string Key, Object value)
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
		public valueType GetValueOrDefault<valueType>(string Key, valueType defaultValue)
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
		public void Save()
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
}