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
	[Serializable]
	public class NotFoundException : Exception
	{
		/// <summary>
		/// EXTRACTED FROM THE 'KEROSENE.TOOLS' LIBRARY.
		/// </summary>
		public NotFoundException() { }

		/// <summary>
		/// EXTRACTED FROM THE 'KEROSENE.TOOLS' LIBRARY.
		/// </summary>
		/// <param name="message"></param>
		public NotFoundException(string message) : base(message) { }

		/// <summary>
		/// EXTRACTED FROM THE 'KEROSENE.TOOLS' LIBRARY.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="inner"></param>
		public NotFoundException(string message, Exception inner) : base(message, inner) { }
	}
}
