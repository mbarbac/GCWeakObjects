using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Kerosene.Tools.Tests
{
	// ====================================================
	//[OnlyThisClass]
	[TestClass]
	public class Test_CustomizableComparer
	{
		//[OnlyThisMethod]
		[TestMethod]
		public void Value_Type()
		{
			var comparer = new CustomizableComparer<int>();
			var objA = 5;
			var objB = 7;
			Assert.IsFalse(comparer.Equals(objA, objB));

			objA = 0;
			objB = 7;
			Assert.IsFalse(comparer.Equals(objA, objB));
		}

		//[OnlyThisMethod]
		[TestMethod]
		public void Compare_Strings()
		{
			var s1 = "MyTest";
			var s2 = new StringBuilder().Append("My").Append("Test").ToString();
			var s3 = String.Intern(s2);

			var comparer = new CustomizableComparer<string>();

			Assert.IsFalse(ReferenceEquals(s1, s2));
			Assert.IsTrue(comparer.Equals(s1, s2));

			Assert.IsTrue(ReferenceEquals(s1, s3));
			Assert.IsTrue(comparer.Equals(s1, s3));
		}
	}
}
