using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Kerosene.Tools
{
	// ====================================================
	/// <summary>
	/// EXTRACTED FROM THE 'KEROSENE.TOOLS' LIBRARY. Provides source-code compatible calls
	/// for the usage of this project, without including the complete tools library.
	/// </summary>
	public static class ConsoleEx
	{
		/// <summary>
		/// EXTRACTED FROM THE 'KEROSENE.TOOLS' LIBRARY.
		/// </summary>
		public static bool Interactive
		{
			[DebuggerStepThrough]
			get { return _Interactive; }
			set { _Interactive = value; }
		}
		static bool _Interactive = true;

		/// <summary>
		/// EXTRACTED FROM THE 'KEROSENE.TOOLS' LIBRARY.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="args"></param>
		public static void WriteLine(string message = null, params object[] args)
		{
			if (message == null) message = string.Empty;
			else if (args != null && args.Length != 0) message = string.Format(message, args);

			Console.WriteLine(message);
		}

		/// <summary>
		/// EXTRACTED FROM THE 'KEROSENE.TOOLS' LIBRARY.
		/// </summary>
		/// <param name="header"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public static string ReadLine(string header = null, params object[] args)
		{
			if (header != null) WriteLine(header, args);

			var str = Interactive ? Console.ReadLine() : string.Empty;
			if (!Interactive) WriteLine();

			return str;
		}

		/// <summary>
		/// EXTRACTED FROM THE 'KEROSENE.TOOLS' LIBRARY.
		/// </summary>
		/// <param name="header"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public static bool AskInteractive(string header = null, params object[] args)
		{
			Interactive = true;
			var str = ReadLine(header ?? "\n=== Press [N] for non-interactive mode... ");

			if (str.ToUpper() == "N") Interactive = false;
			return Interactive;
		}
	}
}
