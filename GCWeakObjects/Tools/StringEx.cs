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
	/// Helpers and extensions for working with <see cref="System.String"/> objects.
	/// </summary>
	public static class StringEx
	{
		/// <summary>
		/// Returns a formatted string using the source one as the format specification,
		/// along with the given optionaly arguments.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public static string FormatWith(this string source, params object[] args)
		{
			if (source == null) throw new NullReferenceException("source");
			if (args != null) source = string.Format(source, args);
			return source;
		}
	}
}
