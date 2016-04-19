using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Kerosene.Tools
{
	// ====================================================
	/// <summary>
	/// EXTRACTED FROM THE 'KEROSENE.TOOLS' LIBRARY. Provides source-code compatible calls
	/// for the usage of this project, without including the complete tools library.
	/// </summary>
	public static class MethodEx_
	{
		/// <summary>
		/// EXTRACTED FROM THE 'KEROSENE.TOOLS' LIBRARY.
		/// </summary>
		/// <param name="method"></param>
		/// <param name="chain"></param>
		/// <returns></returns>
		public static string EasyName(this MethodBase method, bool chain = false)
		{
			if (method == null) throw new NullReferenceException("method");

			StringBuilder sb = new StringBuilder();

			if (method.DeclaringType != null)
				sb.AppendFormat("{0}.", method.DeclaringType.EasyName());

			sb.Append(method.Name);

			return sb.ToString();
		}
	}
}
