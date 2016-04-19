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
	public class Test_SoftDictionary
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
		public class MyDictionary : SoftDictionary<Person, Engine>
		{
			/// <summary>
			/// Initializes a new instance, populated with test contents.
			/// We need to pass cycles and ticks arguments as we are populating it.
			/// </summary>
			public MyDictionary(
				int count,
				long cycles = 0,
				long ticks = 0)
				: base(new Person.Comparer())
			{
				GCCycles = cycles;
				GCTicks = ticks;

				for (int i = 0; i < count; i++)
				{
					var key = new Person((i + 1).ToString());
					var value = new Engine((i + 1).ToString());
					Add(key, value);
				}
			}
		}

		// ================================================

		//[OnlyThisMethod]
		[TestMethod]
		public void Expires_After_Next_GC()
		{
			var count = 5;
			var dict = new MyDictionary(count);

			ConsoleEx.WriteLine("\n> First collection...");
			GC.Collect();
			GC.WaitForPendingFinalizers();
			ConsoleEx.WriteLine("- {0}", dict);
			Assert.AreEqual(count, dict.Count);

			ConsoleEx.WriteLine("\n> Second collection...");
			GC.Collect();
			GC.WaitForPendingFinalizers();
			ConsoleEx.WriteLine("- {0}", dict);
			Assert.AreEqual(0, dict.Count);
		}

		//[OnlyThisMethod]
		[TestMethod]
		public void Survive_Many_Cycles()
		{
			var cycles = 2;
			var count = 5;
			var dict = new MyDictionary(count, cycles: cycles);

			// Survive for the given number of cycles...
			for (int i = 0; i < cycles; i++)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();

				ConsoleEx.WriteLine("- Iteration: {0} ==> {1}", i, dict);
				Assert.AreEqual(count, dict.Count);
			}

			// Collected afterwards...
			var max = 10; ConsoleEx.WriteLine("\n> Testing for max {0} iterations...", max);
			for (int i = 0; i < max; i++)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();

				ConsoleEx.WriteLine("- Iteration: {0} ==> {1}", i, dict);
				if (dict.Count == 0) break;
			}
			Assert.AreEqual(0, dict.Count);
		}

		//[OnlyThisMethod]
		[TestMethod]
		public void Survive_While_Used()
		{
			var count = 5;
			var dict = new MyDictionary(count);

			var max = 10; ConsoleEx.WriteLine("\n> Testing for max {0} iterations...", max);
			for (int i = 0; i < max; i++)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();

				ConsoleEx.WriteLine("- Iteration: {0} ==> {1}", i, dict);
				var key = dict.Keys.ElementAt(0);
				var value = dict[key];
				Assert.IsNotNull(value);
				Assert.IsTrue(dict.Count >= 1);
			}
		}

		//[OnlyThisMethod]
		[TestMethod]
		public void ContainsKey()
		{
			var count = 5;
			var dict = new MyDictionary(count);
			GC.Collect();
			GC.WaitForPendingFinalizers();
			ConsoleEx.WriteLine("- {0}", dict);
			Assert.AreEqual(count, dict.Count);

			var key = new Person("1");
			var obj = dict[key];
			Assert.IsNotNull(obj);
		}

		//[OnlyThisMethod]
		[TestMethod]
		public void TryGetValue()
		{
			var count = 5;
			var dict = new MyDictionary(count);
			GC.Collect();
			GC.WaitForPendingFinalizers();
			ConsoleEx.WriteLine("- {0}", dict);
			Assert.AreEqual(count, dict.Count);

			Person key = new Person("WhatEver");
			Engine value = null;
			var r = dict.TryGetValue(key, out value);
			Assert.IsFalse(r);
			Assert.IsNull(value);

			key = new Person("1");
			r = dict.TryGetValue(key, out value);
			Assert.IsTrue(r);
			Assert.IsNotNull(value);
		}

		//[OnlyThisMethod]
		[TestMethod]
		public void Performance()
		{
			Func<string, Action, double> executor = (message, action) =>
			{
				ConsoleEx.WriteLine("\n> {0}", message);

				Stopwatch watch = new Stopwatch();
				watch.Start();
				action();
				watch.Stop();

				var secs = (double)watch.ElapsedMilliseconds / 1000;
				ConsoleEx.WriteLine("- Elapsed: {0} secs.", secs.ToString("#,###.0000"));
				return secs;
			};

			long count = 4 * 1000;
			string str = null;

			str = string.Format("Testing {0} Standard Dictionary...", count.ToString("#,###"));
			var std = executor(str, () =>
			{
				var dict = new Dictionary<Person, Engine>(); for (long i = 0; i < count; i++)
				{
					GC.Collect();
					GC.WaitForPendingFinalizers();

					var key = new Person(i.ToString());
					var value = new Engine(i.ToString());
					dict.Add(key, value);
				}
			});

			str = string.Format("Testing {0} Soft Dictionary...", count.ToString("#,###"));
			var neo = executor(str, () =>
			{
				var dict = new MyDictionary(0); for (long i = 0; i < count; i++)
				{
					GC.Collect();
					GC.WaitForPendingFinalizers();

					var key = new Person(i.ToString());
					var value = new Engine(i.ToString());
					dict.Add(key, value);
				}
			});

			var dif = neo - std;
			var fct = neo / std; if (fct < 1) fct = 1 / fct;

			ConsoleEx.WriteLine("\n> Test - Standard = {0} => Factor: {1}",
				dif.ToString("#,###.0000"),
				fct.ToString("#,###.00"));
		}
	}
}
