using Geohashing;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Threading.Tasks;

namespace TestApp
{
	[TestClass]
	public class GeohashTests
	{
		/// <summary>
		/// Tests the geohash from the original comic.
		/// </summary>
		[TestMethod]
		public void TestOriginal()
		{
			GeoCoordinate origin = new GeoCoordinate(37.421542, -122.085589);
			GeoCoordinate expectedTarget = new GeoCoordinate(37.857713, -122.544544);
			GeoCoordinate actualTarget = await(Geohash.Get(origin, new DateTime(2005, 05, 26), GeohashMode.CurrentGraticule)).Position;
			AssertCoordinatesEqual(expectedTarget, actualTarget);
		}

		struct TestSuiteEntry { public readonly DateTime date; public readonly GeoCoordinate coordinateWest; public readonly GeoCoordinate coordinateEast; public TestSuiteEntry(DateTime date, GeoCoordinate coordinateWest, GeoCoordinate coordinateEast) { this.date = date; this.coordinateWest = coordinateWest; this.coordinateEast = coordinateEast; } }
		/// <summary>
		/// The test suite from http://wiki.xkcd.com/geohashing/30W_Time_Zone_Rule.
		/// </summary>
		[TestMethod]
		public void Test30W()
		{
			IList<TestSuiteEntry> entries = new List<TestSuiteEntry>()
			{
				new TestSuiteEntry(new DateTime(2008, 05, 20), new GeoCoordinate(68.63099, -30.61895), new GeoCoordinate(68.63099, -29.61895)),
				new TestSuiteEntry(new DateTime(2008, 05, 21), new GeoCoordinate(68.17947, -30.86154), new GeoCoordinate(68.17947, -29.86154)),
				new TestSuiteEntry(new DateTime(2008, 05, 22), new GeoCoordinate(68.97287, -30.23870), new GeoCoordinate(68.97287, -29.23870)),
				new TestSuiteEntry(new DateTime(2008, 05, 23), new GeoCoordinate(68.40025, -30.72277), new GeoCoordinate(68.40025, -29.72277)),
				new TestSuiteEntry(new DateTime(2008, 05, 24), new GeoCoordinate(68.12665, -30.54753), new GeoCoordinate(68.12665, -29.54753)),
				new TestSuiteEntry(new DateTime(2008, 05, 25), new GeoCoordinate(68.94177, -30.18287), new GeoCoordinate(68.94177, -29.18287)),
				new TestSuiteEntry(new DateTime(2008, 05, 26), new GeoCoordinate(68.67313, -30.60731), new GeoCoordinate(68.67313, -29.60731)),
				new TestSuiteEntry(new DateTime(2008, 05, 27), new GeoCoordinate(68.20968, -30.10144), new GeoCoordinate(68.12537, -29.57711)),
				new TestSuiteEntry(new DateTime(2008, 05, 28), new GeoCoordinate(68.68745, -30.21221), new GeoCoordinate(68.71044, -29.11273)),
				new TestSuiteEntry(new DateTime(2008, 05, 29), new GeoCoordinate(68.46470, -30.03412), new GeoCoordinate(68.27833, -29.74114)),
				new TestSuiteEntry(new DateTime(2008, 05, 30), new GeoCoordinate(68.85310, -30.24460), new GeoCoordinate(68.32272, -29.70458))
			};
			GeoCoordinate originWest = new GeoCoordinate(68, -30);
			GeoCoordinate originEast = new GeoCoordinate(68, -29);
			foreach (TestSuiteEntry entry in entries)
			{
				GeoCoordinate actualWest = await(Geohash.Get(originWest, entry.date, GeohashMode.CurrentGraticule)).Position;
				GeoCoordinate actualEast = await(Geohash.Get(originEast, entry.date, GeohashMode.CurrentGraticule)).Position;
				AssertCoordinatesEqual(entry.coordinateWest, actualWest, "Test failed west");
				AssertCoordinatesEqual(entry.coordinateEast, actualEast, "Test failed east");
			}
		}

		private static void AssertCoordinatesEqual(GeoCoordinate expected, GeoCoordinate actual, string message = "")
		{
			const double delta = 0.00001;
			Assert.AreEqual(expected.Latitude, actual.Latitude, delta, message);
			Assert.AreEqual(expected.Longitude, actual.Longitude, delta, message);
		}

		/// <summary>
		/// Workaround to simply await a task synchronously.
		/// </summary>
		/// <typeparam name="T">The "return" type of the task.</typeparam>
		/// <param name="task">The task to await.</param>
		/// <returns>The result of the task, as soon as its calculation finishes.</returns>
		private static T await<T>(Task<T> task)
		{
			task.Wait();
			return task.Result;
		}
	}
}
