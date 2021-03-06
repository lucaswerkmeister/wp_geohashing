﻿/*
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

			cartographicModeListPicker.ItemsSource = GetLocalizedEnumValues(typeof(MapCartographicMode));
			cartographicModeListPicker.SelectedItem = cartographicModeListPicker.Items[(int)settings.CartographicMode];

			geohashModeListPicker.ItemsSource = GetLocalizedEnumValues(typeof(GeohashMode));
			geohashModeListPicker.SelectedItem = geohashModeListPicker.Items[(int)settings.HashMode];

			coordinatesModeListPicker.ItemsSource = GetLocalizedEnumValues(typeof(CoordinatesDisplay));
			coordinatesModeListPicker.SelectedItem = coordinatesModeListPicker.Items[(int)settings.CoordinatesMode];

			lengthUnitListPicker.ItemsSource = GetLocalizedEnumValues(typeof(UnitSystem));
			lengthUnitListPicker.SelectedItem = lengthUnitListPicker.Items[(int)settings.LengthUnit];

			StringReplacer replacer = (string text) => text
				.Replace("%VERSION%", new AssemblyName(Assembly.GetExecutingAssembly().FullName).Version.ToString())
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

		private IEnumerable<string> GetLocalizedEnumValues(Type enumType)
		{
			if (!enumType.IsEnum)
				throw new ArgumentException("Type must be an enum type!", "enumType");
			foreach (var value in Enum.GetValues(enumType))
				yield return (string)typeof(AppResources).GetProperty(enumType.Name + value.ToString()).GetValue(null);
		}

		private void DjiaBufferSizeTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
		{
			TextBox t = (TextBox)sender;
			int oldSelectionStart = t.SelectionStart;
			t.Text = new System.Text.RegularExpressions.Regex(@"\D").Replace(t.Text, ""); // remove any non-digits (\D)
			t.SelectionStart = Math.Min(oldSelectionStart, t.Text.Length); // Setting t.Text resets t.SelectionStart to 0, so do this even if nothing changed
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