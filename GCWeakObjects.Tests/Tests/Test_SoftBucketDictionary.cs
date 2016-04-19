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
	public class Test_SoftBucketDictionary
	{
		/// <summary>
		/// An example of a reference type.
		/// </summary>
		public class Person
		{
			public Person(string name) { Name = name; }
			public override string ToString() { return string.Format("Name:'{0}'", Name); }
			public string Name { get; }

			/// <summary>
			/// A comparer for this type.
			/// </summary>
			public class Comparer : CustomizableComparer<Person>
			{
				/// <summary>
				/// Initializes a new instance.
				/// </summary>
				public Comparer()
				{
					OnGetHashCodeDelegate = obj =>
					{
						return obj == null || obj.Name == null ? 0 : obj.Name.GetHashCode();
					};
				}
			}
		}

		/// <summary>
		/// An example of a reference type.
		/// </summary>
		public class Engine
		{
			public Engine(string model) { Model = model; }
			public override string ToString() { return string.Format("Model:'{0}'", Model); }
			public string Model { get; }
		}

		/// <summary>
		/// The test class itself.
		/// </summary>
		public class MyDictionary : SoftBucketDictionary<Person, Engine>
		{
			/// <summary>
			/// Initializes a new instance, populated with test contents.
			/// We need to pass cycles and ticks arguments as we are populating it.
			/// </summary>
			public MyDictionary(
				int numkeys,
				int valuesperkey,
				long cycles = 0,
				long ticks = 0)
				: base(new Person.Comparer())
			{
				GCCycles = cycles;
				GCTicks = ticks;

				for (int k = 1; k <= numkeys; k++)
				{
					var key = new Person(k.ToString());
					for (int v = 1; v <= valuesperkey; v++)
					{
						var value = new Engine((k * 10 + v).ToString());
						Add(key, value);
					}
				}
			}
		}

		// ================================================

		//[OnlyThisMethod]
		[TestMethod]
		public void Expires_After_Next_GC()
		{
			var numkeys = 4;
			var valuesperkey = 5;
			var totalvalues = numkeys * valuesperkey;
			var dict = new MyDictionary(numkeys, valuesperkey);

			ConsoleEx.WriteLine("\n> First collection...");
			GC.Collect();
			GC.WaitForPendingFinalizers();
			ConsoleEx.WriteLine("- {0}", dict);
			Assert.AreEqual(numkeys, dict.KeysCount);
			Assert.AreEqual(totalvalues, dict.ValuesCount);

			ConsoleEx.WriteLine("\n> Second collection...");
			GC.Collect();
			GC.WaitForPendingFinalizers();
			ConsoleEx.WriteLine("- {0}", dict);
			Assert.AreEqual(0, dict.KeysCount);
			Assert.AreEqual(0, dict.ValuesCount);
		}

		//[OnlyThisMethod]
		[TestMethod]
		public void Survive_Many_Cycles()
		{
			var cycles = 2;
			var numkeys = 4;
			var valuesperkey = 5;
			var totalvalues = numkeys * valuesperkey;
			var dict = new MyDictionary(numkeys, valuesperkey, cycles: cycles);

			// Survive for the given number of cycles...
			for (int i = 0; i < cycles; i++)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();

				ConsoleEx.WriteLine("- Iteration: {0} ==> {1}", i, dict);
				Assert.AreEqual(numkeys, dict.KeysCount);
				Assert.AreEqual(totalvalues, dict.ValuesCount);
			}

			// Collected afterwards...
			var max = 10; ConsoleEx.WriteLine("\n> Testing for max {0} iterations...", max);
			for (int i = 0; i < max; i++)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();

				ConsoleEx.WriteLine("- Iteration: {0} ==> {1}", i, dict);
				if (dict.KeysCount == 0) break;
				if (dict.ValuesCount == 0) break;
			}
			Assert.AreEqual(0, dict.KeysCount);
			Assert.AreEqual(0, dict.ValuesCount);
		}

		//[OnlyThisMethod]
		[TestMethod]
		public void Survive_While_Used()
		{
			var numkeys = 4;
			var valuesperkey = 5;
			var totalvalues = numkeys * valuesperkey;
			var dict = new MyDictionary(numkeys, valuesperkey);

			var max = 10; ConsoleEx.WriteLine("\n> Testing for max {0} iterations...", max);
			for (int i = 0; i < max; i++)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();

				ConsoleEx.WriteLine("- Iteration: {0} ==> {1}", i, dict);
				var key = dict.Keys.ElementAt(0);
				var bucket = dict.FindList(key); key = null;
				var target = bucket[0]; bucket = null;
				Assert.IsNotNull(target);
				Assert.IsTrue(dict.ValuesCount >= 1);
			}
			Assert.IsTrue(dict.ValuesCount >= 1);
		}
	}
}
