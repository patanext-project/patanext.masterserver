using System;
using Grpc.Core.Logging;

namespace project
{
	public static class Logger
	{
		public static void Log(object data)
		{
			Console.WriteLine("> " + data);
		}

		public static void Warning(object data)
		{
			var prevColor = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine("[Warning] " + data);
			Console.ForegroundColor = prevColor;
		}

		public static void Error(object data, bool throwException)
		{
			var prevColor = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("[Error] " + data);
			Console.ForegroundColor = prevColor;

			if (throwException)
			{
				throw new Exception("Error! " + data.ToString());
			}
		}
	}
}