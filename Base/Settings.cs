using Microsoft.Phone.Maps.Controls;
using System;
using System.ComponentModel;
using System.Device.Location;
using System.Globalization;
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

	public enum GeohashMode
	{
		[Description("Nearest")]
		Nearest,
		[Description("Current graticule")]
		CurrentGraticule
	}

	public enum CoordinatesDisplay
	{
		[Description("Decimal (1.034)")]
		Decimal,
		[Description("DMS (1°2'3'')")]
		DegreeMinutesSeconds
	}

	public enum UnitSystem
	{
		Metric,
		Imperial
	}

	public class Settings
	{
		private const string localizeSettingName = "Localize";
		private const string autoZoomSettingName = "AutoZoom";
		private const string cartographicModeSettingName = "CartographicMode";
		private const string djiaBufferSizeSettingName = "DjiaBufferSize";
		private const string geohashModeSettingName = "GeohashMode";
		private const string coordinatesModeSettingName = "CoordinatesMode";
		private const string lengthUnitSettingName = "LengthUnit";
		private const string loadImagesOverWifiSettingName = "LoadImagesOverWifi";

		private const bool localizeSettingDefault = true;
		private const bool autoZoomSettingDefault = true;
		private const MapCartographicMode cartographicModeSettingDefault = MapCartographicMode.Road;
		private const int djiaBufferSizeSettingDefault = 7;
		private const GeohashMode geohashModeSettingDefault = GeohashMode.CurrentGraticule;
		private const CoordinatesDisplay coordinatesModeSettingDefault = CoordinatesDisplay.DegreeMinutesSeconds;
		private const UnitSystem lengthUnitSettingDefault = UnitSystem.Metric;
		private const bool loadImagesOverWifiSettingDefault = false;

		public static event EventHandler<SettingChangedEventArgs> LocalizeChanged;
		public static event EventHandler<SettingChangedEventArgs> AutoZoomChanged;
		public static event EventHandler<SettingChangedEventArgs> CartographicModeChanged;
		public static event EventHandler<SettingChangedEventArgs> DjiaBufferSizeChanged;
		public static event EventHandler<SettingChangedEventArgs> GeohashModeChanged;
		public static event EventHandler<SettingChangedEventArgs> CoordinatesModeChanged;
		public static event EventHandler<SettingChangedEventArgs> LengthUnitChanged;
		public static event EventHandler<SettingChangedEventArgs> LoadImagesOverWifiChanged;

		private IsolatedStorageSettings isolatedStore = System.ComponentModel.DesignerProperties.IsInDesignTool ? null : IsolatedStorageSettings.ApplicationSettings;

		public bool Localize
		{
			get
			{
				return GetValueOrDefault(localizeSettingName, localizeSettingDefault);
			}
			set
			{
				bool oldValue = GetValueOrDefault(localizeSettingName, localizeSettingDefault);
				if (AddOrUpdateValue(localizeSettingName, value))
				{
					Save();
					if (LocalizeChanged != null)
						LocalizeChanged(oldValue, new SettingChangedEventArgs(localizeSettingName));
				}
			}
		}

		public bool AutoZoom
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
					if (AutoZoomChanged != null)
						AutoZoomChanged(oldValue, new SettingChangedEventArgs(autoZoomSettingName));
				}
			}
		}

		public MapCartographicMode CartographicMode
		{
			get
			{
				return GetValueOrDefault(cartographicModeSettingName, cartographicModeSettingDefault);
			}
			set
			{
				MapCartographicMode oldValue = GetValueOrDefault(cartographicModeSettingName, cartographicModeSettingDefault);
				if (AddOrUpdateValue(cartographicModeSettingName, value))
				{
					Save();
					if (CartographicModeChanged != null)
						CartographicModeChanged(oldValue, new SettingChangedEventArgs(cartographicModeSettingName));
				}
			}
		}

		public int DjiaBufferSize
		{
			get
			{
				return GetValueOrDefault(djiaBufferSizeSettingName, djiaBufferSizeSettingDefault);
			}
			set
			{
				int oldValue = GetValueOrDefault(djiaBufferSizeSettingName, djiaBufferSizeSettingDefault);
				if (AddOrUpdateValue(djiaBufferSizeSettingName, value))
				{
					Save();
					if (DjiaBufferSizeChanged != null)
						DjiaBufferSizeChanged(oldValue, new SettingChangedEventArgs(djiaBufferSizeSettingName));
				}
			}
		}

		public GeohashMode HashMode
		{
			get
			{
				return GetValueOrDefault(geohashModeSettingName, geohashModeSettingDefault);
			}
			set
			{
				GeohashMode oldValue = GetValueOrDefault(geohashModeSettingName, geohashModeSettingDefault);
				if (AddOrUpdateValue(geohashModeSettingName, value))
				{
					Save();
					if (GeohashModeChanged != null)
						GeohashModeChanged(oldValue, new SettingChangedEventArgs(geohashModeSettingName));
				}
			}
		}

		public CoordinatesDisplay CoordinatesMode
		{
			get
			{
				return GetValueOrDefault(coordinatesModeSettingName, coordinatesModeSettingDefault);
			}
			set
			{
				CoordinatesDisplay oldValue = GetValueOrDefault(coordinatesModeSettingName, coordinatesModeSettingDefault);
				if (AddOrUpdateValue(coordinatesModeSettingName, value))
				{
					Save();
					if (CoordinatesModeChanged != null)
						CoordinatesModeChanged(oldValue, new SettingChangedEventArgs(coordinatesModeSettingName));
				}
			}
		}

		public UnitSystem LengthUnit
		{
			get
			{
				return GetValueOrDefault(lengthUnitSettingName, lengthUnitSettingDefault);
			}
			set
			{
				UnitSystem oldValue = GetValueOrDefault(lengthUnitSettingName, lengthUnitSettingDefault);
				if (AddOrUpdateValue(lengthUnitSettingName, value))
				{
					Save();
					if (LengthUnitChanged != null)
						LengthUnitChanged(oldValue, new SettingChangedEventArgs(lengthUnitSettingName));
				}
			}
		}

		public bool LoadImagesOverWifi
		{
			get
			{
				return GetValueOrDefault(loadImagesOverWifiSettingName, loadImagesOverWifiSettingDefault);
			}
			set
			{
				bool oldValue = GetValueOrDefault(loadImagesOverWifiSettingName, loadImagesOverWifiSettingDefault);
				if (AddOrUpdateValue(loadImagesOverWifiSettingName, value))
				{
					Save();
					if (LoadImagesOverWifiChanged != null)
						LoadImagesOverWifiChanged(oldValue, new SettingChangedEventArgs(loadImagesOverWifiSettingName));
				}
			}
		}

		/// <summary>
		/// Converts a GeoCoordinate to a string, based on the CoordinatesMode setting.
		/// </summary>
		/// <param name="coordinate">The coordinate.</param>
		/// <returns>A string representation of the coordinate.</returns>
		// Implementation based on http://www.sharpgis.net/post/2011/11/20/Correctly-displaying-your-current-location.aspx
		public string CoordinateToString(GeoCoordinate coordinate)
		{
			if (coordinate == null)
				throw new ArgumentNullException("coordinate");
			char ns = coordinate.Latitude < 0 ? 'S' : 'N'; //Southern or Northern hemisphere?
			char ew = coordinate.Longitude < 0 ? 'W' : 'E'; //Eastern or Western hemisphere?
			switch (CoordinatesMode)
			{
				case CoordinatesDisplay.Decimal:
					return coordinate.Latitude.ToString("F3", CultureInfo.CurrentCulture) + "°, " + coordinate.Longitude.ToString("F3", CultureInfo.CurrentCulture) + '°';
				case CoordinatesDisplay.DegreeMinutesSeconds:
					double degLat = Math.Abs(coordinate.Latitude);
					double degLon = Math.Abs(coordinate.Longitude);
					double minLat = (degLat - (int)degLat) * 60;
					double minLon = (degLon - (int)degLon) * 60;
					double secLat = (minLat - (int)minLat) * 60;
					double secLon = (minLon - (int)minLon) * 60;
					return String.Format(CultureInfo.CurrentCulture, "{0}{1}°{2}'{3}\", {4}{5}°{6}'{7}\"", ns, (int)degLat, (int)minLat, (int)Math.Round(secLat), ew, (int)degLon, (int)minLon, (int)Math.Round(secLon));
				default:
					return "Unknown coordinates mode " + CoordinatesMode + " - this should never happen";
			}
		}

		public string LengthToString(double length)
		{
			switch (LengthUnit)
			{
				case UnitSystem.Metric:
					return (length / 1000).ToString("F2", CultureInfo.CurrentCulture) + "km";
				case UnitSystem.Imperial:
					return (length / 1609.344).ToString("F2", CultureInfo.CurrentCulture) + "mi";
				default:
					return "Unknown unit system " + LengthUnit + " - this should never happen";
			}
		}

		/// <summary>
		/// Updates a setting value, adding it if it is not present.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <param name="value">The setting value.</param>
		/// <returns><code>true</code> if the setting was changed, <code>false</code> otherwise.</returns>
		private bool AddOrUpdateValue(string key, object value)
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
		private T GetValueOrDefault<T>(string key, T defaultValue)
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
		private void Save()
		{
			isolatedStore.Save();
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
