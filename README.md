A Geohashing app for the Windows Phone.

Features
========
* On startup, your location and the according Geohash is loaded.
* You can change the graticule by tapping and holding anywhere on the map, and choosing "Use this graticule" on the context menu.
* You can change the date using the DatePicker at the bottom.
* You can change the map mode in the settings.

Installation
============
I don't think the app is ready for publishing, so if you want to use it already, you must:
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
* Show distance to target
* Wiki integration (might by tricky)
  * Post message
  * Upload picture
    * Shoot pictures from within the app
      * If possible, include the location in metadata
  * Show graticule page
* Send location to Maps app
* More settings:
  * Metric/Imperial units for distance
  * Deg/Min/Sec for coordinates
  * Wiki credentials (if I get the integration to work)
