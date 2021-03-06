﻿using System;
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
	public static class DebugEx
	{
		/// <summary>
		/// EXTRACTED FROM THE 'KEROSENE.TOOLS' LIBRARY.
		/// </summary>
		public static bool AutoFlush
		{
			[DebuggerStepThrough]
			get { return Debug.AutoFlush; }
			set { Debug.AutoFlush = value; }
		}

		/// <summary>
		/// EXTRACTED FROM THE 'KEROSENE.TOOLS' LIBRARY.
		/// </summary>
		public static int IndentSize
		{
			[DebuggerStepThrough]
			get { return Debug.IndentSize; }
			set { Debug.IndentSize = value; }
		}

		/// <summary>
		/// EXTRACTED FROM THE 'KEROSENE.TOOLS' LIBRARY.
		/// </summary>
		public static void AddConsoleListener() { }

		/// <summary>
		/// EXTRACTED FROM THE 'KEROSENE.TOOLS' LIBRARY.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="args"></param>
		[Conditional("DEBUG")]
		public static void WriteLine(string message = null, params object[] args)
		{
			if (message == null) message = string.Empty;
			else if (args != null && args.Length != 0) message = string.Format(message, args);

			Debug.WriteLine(message);
		}
	}
}
