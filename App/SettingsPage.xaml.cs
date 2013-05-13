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
using Geohashing.Resources;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Maps.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Geohashing
{
	public partial class SettingsPage : PhoneApplicationPage
	{
		private delegate string StringReplacer(string input);

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

			rawValues = Enum.GetValues(typeof(CoordinatesDisplay));
			values = new List<string>();
			foreach (CoordinatesDisplay e in rawValues)
				values.Add((typeof(CoordinatesDisplay).GetMember(e.ToString())[0].GetCustomAttributes(typeof(DescriptionAttribute), false)[0] as DescriptionAttribute).Description);
			coordinatesModeListPicker.ItemsSource = values;
			coordinatesModeListPicker.SelectedItem = (typeof(CoordinatesDisplay).GetMember(settings.CoordinatesMode.ToString())[0].GetCustomAttributes(typeof(DescriptionAttribute), false)[0] as DescriptionAttribute).Description;

			lengthUnitListPicker.ItemsSource = Enum.GetValues(typeof(UnitSystem));
			lengthUnitListPicker.SelectedItem = settings.LengthUnit;

			StringReplacer replacer = (string text) => text.Replace("%VERSION%", new AssemblyName(Assembly.GetExecutingAssembly().FullName).Version.ToString())
				.Replace("%SOURCELINK%", @"<Hyperlink NavigateUri=""https://www.github.com/lucaswerkmeister/wp_geohashing"" TargetName=""_"">" + AppResources.AboutSourceLink + @"</Hyperlink>")
				.Replace("%BUGREPORTLINK%", @"<Hyperlink NavigateUri=""https://www.github.com/lucaswerkmeister/wp_geohashing/issues/new"" TargetName=""_"">" + AppResources.AboutBugReportLink + @"</Hyperlink>")
				.Replace("%EMAILLINK%", @"<Hyperlink NavigateUri=""mailto:mail@lucaswerkmeister.de"" TargetName=""_"">" + AppResources.AboutEmailLink + @"</Hyperlink>");
			AboutRichTextBox.Xaml = @"
				<Section 
					xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
					<Paragraph>
						" + replacer(AppResources.AboutParagraph1) + @"
					</Paragraph>
					<Paragraph>
						" + replacer(AppResources.AboutParagraph2) + @"
					</Paragraph>
					<Paragraph>
						" + replacer(AppResources.AboutParagraph3) + @"
					</Paragraph>
					<Paragraph>
						" + replacer(AppResources.AboutParagraph4) + @"
					</Paragraph>
				</Section>";

			PrivacyPolicyRichTextBox.Xaml = @"
				<Section
					xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
					<Paragraph>
						" + AppResources.PrivacyPolicyParagraph1 + @"
					</Paragraph>
					<Paragraph>
						" + AppResources.PrivacyPolicyParagraph2 + @"
					</Paragraph>
				</Section>";
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

		/// <summary>
		/// Scrolls the page down so that the entire list picker fits into it.
		/// 
		/// Code adapted from http://www.telerik.com/community/forums/windows-phone/listpicker/listpicker-expanded-don-t-scroll-correctly.aspx#2318835.
		/// </summary>
		/// <param name="sender">Event sender.</param>
		/// <param name="e">Event args.</param>
		private void scrollToLengthUnitListPicker(object sender, RoutedEventArgs e)
		{
			if (lengthUnitListPicker.ListPickerMode != ListPickerMode.Expanded)
				return; // page init

			scrollViewer.UpdateLayout();

			double maxScrollPos = scrollViewer.ExtentHeight - scrollViewer.ViewportHeight;
			double scrollPos = scrollViewer.VerticalOffset - scrollViewer.TransformToVisual(lengthUnitListPicker).Transform(new Point(0, 0)).Y;

			scrollPos = Math.Max(scrollPos, 0);
			scrollPos = Math.Min(scrollPos, maxScrollPos);

			scrollViewer.ScrollToVerticalOffset(scrollPos);
		}
	}

	public class EnumIntConverter : IValueConverter
	{

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (int)value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Enum.GetValues(targetType).GetValue((int)value);
		}
	}
}