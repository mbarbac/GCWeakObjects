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
	public static class IEnumerableEx
	{
		/// <summary>
		/// EXTRACTED FROM THE 'KEROSENE.TOOLS' LIBRARY.
		/// </summary>
		/// <param name="iter"></param>
		/// <param name="action"></param>
		public static void ForEach(this IEnumerable iter, Action<object> action)
		{
			if (iter == null) throw new ArgumentNullException("iter");
			if (action == null) throw new ArgumentNullException("action");

			foreach (object obj in iter) action(obj);
		}

		/// <summary>
		/// EXTRACTED FROM THE 'KEROSENE.TOOLS' LIBRARY.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="iter"></param>
		/// <param name="action"></param>
		public static void ForEach<T>(this IEnumerable<T> iter, Action<T> action)
		{
			if (iter == null) throw new ArgumentNullException("iter");
			if (action == null) throw new ArgumentNullException("action");

			foreach (T obj in iter) action(obj);
		}
	}
}
