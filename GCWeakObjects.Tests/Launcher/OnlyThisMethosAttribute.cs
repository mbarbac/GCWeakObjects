using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kerosene.Tools
{
	// ====================================================
	/// <summary>
	/// If used in any test method identifies the set of methods the launcher will execute,
	/// excluding those not decorated with this attribute.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
	public class OnlyThisMethodAttribute : Attribute { }
}
