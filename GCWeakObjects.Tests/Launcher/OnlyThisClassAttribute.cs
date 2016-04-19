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
	/// If used in any test class identifies the set of classes the launcher will execute,
	/// excluding those not decorated with this attribute.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
	public class OnlyThisClassAttribute : Attribute { }
}
