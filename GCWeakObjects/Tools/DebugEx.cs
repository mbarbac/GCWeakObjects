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
	/// Helpers and extensions for working with DEBUG environments.
	/// </summary>
	public static class DebugEx
	{
		/// <summary>
		/// Writes the given message into the listeners adding a newline terminator, also
		/// intercepting appropriately any embedded newline characters.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="args"></param>
		[Conditional("DEBUG")]
		public static void WriteLine(string message = null, params object[] args)
		{
			if (message == null) message = string.Empty;
			else if (args != null && args.Length != 0) message = string.Format(message, args);

			Debug.WriteLine(string.Empty);
		}
	}
}
