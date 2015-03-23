A Geohashing app for the Windows Phone.

State of the project
====================

Development of this app has, for my part, ended. I have lost interest in the Windows Phone platform, my main desktop OS is no longer Windows, and I no longer have a Windows Phone device available to debug on.

I am happy with the state the app is left in. It can be considered complete (Wiki integration, see below, is an optional feature), and there are no known bugs nor crashes. You are of course welcome to continue development yourself under the terms of the license.

Features
========
* On startup, your location and the according Geohash is loaded and displayed on the map.
* An overlay in the map shows position, geohash, and distance.
* You can change the graticule by tapping and holding anywhere on the map.
* You can change the date using the DatePicker at the bottom.
* You can send the location to the Maps app for navigation instructions.
* You can pin the current location to the start screen. The live tile will refresh periodically, showing you a small map as well as coordinates and distance.
* You can choose between seeing the geohash for your current graticule and the geohash that is nearest to you.
* You can change the map mode in the settings.
* You can choose between decimal and DMS (degree minutes seconds) coordinate display in the settings.
* You can choose between metric and imperial units in the settings.

Installation
============
To run the app from source, you must:
* Locally clone this repository (if you want to change anything, fork it first)
* Import the project into Visual Studio (I use 2012, don't know if 2010 or earlier will work - you'll probably need the Windows Phone SDK)
* Run it on a simulator to try it out. If you want to use it *on your phone*, you have to:
   * Get yourself a developer account, if you don't already have one
   * Register your phone for development, if you haven't already
   * Run the app on "Device" (remember, I could be evil - be careful :) )

Contributing
============
You can contribute by:
* [Reporting bugs](https://github.com/lucaswerkmeister/wp_geohashing/issues)
* Requesting features (issue with label "enhancement")
* Contributing code.

Planned features
================
Most of these are inspired (which is a fancy word for "copied") from the Android app:
* Wiki integration (next version)
   * Post message
   * Upload picture
      * Shoot pictures from within the app
         * If possible, include the location in metadata
   * Show graticule page
   * Settings: credentials
