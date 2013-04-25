using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
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
			string key = settingPrefix + date.ToString("yyyy-MM-dd", CultureInfo.CurrentCulture);
			if (!settings.Contains(key))
			{
				IEnumerable<string> djiaKeys = (from string setting in settings.Keys
												where setting.StartsWith(settingPrefix)
												select setting);

				while (djiaKeys.Count() > appSettings.DjiaBufferSize)
				{
					// Delete the oldest value
					List<string> dates = new List<string>();
					foreach (string oldKey in djiaKeys)
						dates.Add(oldKey.Substring(settingPrefix.Length));
					dates.Sort();
					settings.Remove(settingPrefix + dates[0]);
				}
				string djia = await Load(date);
				if (appSettings.DjiaBufferSize == 0)
					return djia; // Don't save
				settings[key] = djia;
				settings.Save();
				return djia; // No point in wasting time in the lookup below when we already have the value
			}
			return (string)settings[key];
		}

		private static async Task<string> Load(DateTime date)
		{
			HttpWebResponse response = await HttpWebRequest.Create("http://geo.crox.net/djia/" + date.Year + "/" + date.Month + "/" + date.Day).GetResponseAsync();
			if (response == null)
				throw new NoDjiaException(NoDjiaException.NoDjiaCause.UnknownConnectionError);
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
			if (String.IsNullOrEmpty(message))
				Cause = NoDjiaCause.NoInternet;
			else if (message.Contains("error"))
			{
				if (message.Contains("date format error"))
					Cause = NoDjiaCause.DateFormat;
				else if (message.Contains("not available"))
					Cause = NoDjiaCause.NotAvailable;
			}
			else
				Cause = NoDjiaCause.Unknown;
		}
	}
}
