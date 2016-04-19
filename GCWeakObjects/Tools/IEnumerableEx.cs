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
	/// Helpers and extensions for working with <see cref="IEnumerable"/> objects.
	/// </summary>
	public static class IEnumerableEx
	{
		/// <summary>
		/// Executes the given delegate for each member of the collection.
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
		/// Executes the given delegate for each member of the collection.
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
