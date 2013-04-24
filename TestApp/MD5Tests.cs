using Geohashing;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TestApp
{
	[TestClass]
	public class MD5Tests
	{
		[TestMethod]
		public void MD5TestSuite()
		{
			Dictionary<string, string> testSuite = new Dictionary<string, string>{
				{"","d41d8cd98f00b204e9800998ecf8427e"},
				{"a", "0cc175b9c0f1b6a831c399e269772661"},
				{"abc", "900150983cd24fb0d6963f7d28e17f72"},
				{"message digest", "f96b697d7cb7938d525a2f31aaf161d0"},
				{"abcdefghijklmnopqrstuvwxyz", "c3fcd3d76192e4007dfb496cca67e13b"},
				{"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789", "d174ab98d277d9f5a5611c2c9f419d9f"},
				{"12345678901234567890123456789012345678901234567890123456789012345678901234567890", "57edf4a22be3c955ac49da2e2107b67a"}
			};

			foreach (KeyValuePair<string, string> pair in testSuite)
			{
				string input = pair.Key;
				string expectedResult = pair.Value;
				string actualResult = byteArrayToString(MD5.Digest(input));
				Assert.AreEqual(expectedResult, actualResult, "MD5 test failed for input " + input);
			}
		}

		private static string byteArrayToString(byte[] array)
		{
			StringBuilder ret = new StringBuilder();
			foreach (byte b in array)
				ret.Append(b.ToString("x2", CultureInfo.InvariantCulture));
			return ret.ToString();
		}
	}
}
