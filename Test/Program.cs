using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Geohashing
{
	public static class Program
	{
		public static void Main(string[] args)
		{
			Console.WriteLine("abc".MyToString());
			Console.WriteLine(((string)null).MyToString());
		}

		public static string MyToString(this object obj)
		{
			return obj.ToString();
		}
	}
}
