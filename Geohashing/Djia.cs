using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net;
using System.Threading.Tasks;

namespace Geohashing
{
	public static class Djia
	{
		private static readonly IsolatedStorageSettings settings = System.ComponentModel.DesignerProperties.IsInDesignTool ? null : IsolatedStorageSettings.ApplicationSettings;
		private static readonly Settings appSettings = new Settings();
		private const string settingPrefix = @"\DJIA\";

		public static async Task<string> Get(DateTime date)
		{
			string key = settingPrefix + date.ToString("yyyy-MM-dd");
			if (!settings.Contains(key))
			{
				while (settings.Keys.Count - Settings.SettingsCount > appSettings.DjiaBufferSize)
				{
					// Delete the oldest value
					List<string> dates = new List<string>();
					foreach (string oldKey in settings.Keys)
						if (oldKey.StartsWith(settingPrefix))
							dates.Add(oldKey.Substring(settingPrefix.Length));
					dates.Sort();
					settings.Remove(settingPrefix + dates[0]);
				}
				string djia = await Load(date);
				if (appSettings.DjiaBufferSize == 0)
					return djia; // Don't save
				settings[key] = djia;
				settings.Save();
			}
			return (string)settings[key];
		}

		private static async Task<string> Load(DateTime date)
		{
			HttpWebResponse response = await HttpWebRequest.Create("http://geo.crox.net/djia/" + date.Year + "/" + date.Month + "/" + date.Day).GetResponseAsync();
			string content;
			using (StreamReader sr = new StreamReader(response.GetResponseStream()))
				content = await sr.ReadToEndAsync();
			if (response.StatusCode == HttpStatusCode.OK)
				return content;
			throw new NoDjiaException(content);
		}
	}

	public class NoDjiaException : Exception
	{
		public enum NoDjiaCause { NotAvailable, DateFormat, NoInternet, UnknownConnectionError, Unknown }

		public NoDjiaCause Cause { get; private set; }

		public NoDjiaException() : base() { }
		public NoDjiaException(NoDjiaCause cause)
		{
			Cause = cause;
		}
		public NoDjiaException(string message)
			: base(message)
		{
			if (message.Contains("error"))
			{
				if (message.Contains("date format error"))
					Cause = NoDjiaCause.DateFormat;
				else if (message.Contains("not available"))
					Cause = NoDjiaCause.NotAvailable;
			}
			else if (message.Length == 0)
				Cause = NoDjiaCause.NoInternet;
			else
				Cause = NoDjiaCause.Unknown;
		}
	}
}
