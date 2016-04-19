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
	public static class ExceptionEx
	{
		/// <summary>
		/// EXTRACTED FROM THE 'KEROSENE.TOOLS' LIBRARY.
		/// </summary>
		/// <param name="e"></param>
		/// <param name="format"></param>
		public static void ToConsoleEx(this Exception e, string format = null)
		{
			if (e == null) throw new ArgumentNullException("exception");

			if (format == null) format = "{{0}}";
			format = string.Format(format, GetDisplayString(e));
			ConsoleEx.WriteLine(format);
		}

		/// <summary>
		/// EXTRACTED FROM THE 'KEROSENE.TOOLS' LIBRARY.
		/// </summary>
		/// <param name="e"></param>
		/// <returns></returns>
		public static string GetDisplayString(Exception e)
		{
			StringBuilder sb = new StringBuilder();
			while (e != null)
			{
				sb.Append(e.GetType().EasyName());
				if (e.Message != null) sb.AppendFormat(": {0}", e.Message);
				if (e.StackTrace != null) sb.AppendFormat("\n{0}", e.StackTrace);

				e = e.InnerException;
				if (e != null) sb.Append("\n----- Inner Exception:\n");
			}
			return sb.ToString();
		}
	}
}
