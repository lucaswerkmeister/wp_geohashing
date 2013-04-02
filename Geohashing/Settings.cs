using System;
using System.ComponentModel;
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

	public class Settings
	{
		private const string localizeSettingName="Localize";
		private const string autoZoomSettingName = "AutoZoom";

		private const bool localizeSettingDefault=true;
		private const bool autoZoomSettingDefault = true;

		public static event EventHandler<SettingChangedEventArgs> LocalizeChanged;
		public static event EventHandler<SettingChangedEventArgs> AutoZoomChanged;

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

		/// <summary>
		/// Updates a setting value, adding it if it is not present.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <param name="value">The setting value.</param>
		/// <returns><code>true</code> if the setting was changed, <code>false</code> otherwise.</returns>
		public bool AddOrUpdateValue(string key, object value)
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
		public T GetValueOrDefault<T>(string key, T defaultValue)
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
		public void Save()
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
