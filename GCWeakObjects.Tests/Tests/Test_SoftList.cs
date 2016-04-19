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
	public class Test_SoftList
	{
		/// <summary>
		/// An example of a reference type.
		/// </summary>
		public class Person
		{
			public Person(string name) { Name = name; }
			public override string ToString() { return string.Format("Name:'{0}'", Name); }
			public string Name { get; }
		}

		// ================================================

		//[OnlyThisMethod]
		[TestMethod]
		public void Expires_After_Next_GC()
		{
			var num = 5;
			var list = new SoftList<Person>();
			for (int i = 1; i <= num; i++) list.Add(new Person(i.ToString()));

			ConsoleEx.WriteLine("\n> First collection...");
			GC.Collect();
			GC.WaitForPendingFinalizers();
			Assert.AreEqual(num, list.Count);

			ConsoleEx.WriteLine("\n> Second collection...");
			GC.Collect();
			GC.WaitForPendingFinalizers();
			Assert.AreEqual(0, list.Count);
		}

		//[OnlyThisMethod]
		[TestMethod]
		public void Survive_Many_Cycles()
		{
			var cycles = 2;
			var num = 5;
			var list = new SoftList<Person>() { GCCycles = cycles };
			for (int i = 1; i <= num; i++) list.Add(new Person(i.ToString()));

			// Survive for the given number of cycles...
			for (int i = 0; i < cycles; i++)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();

				ConsoleEx.WriteLine("- Iteration: {0} ==> {1}", i, list);
				Assert.AreEqual(num, list.Count);
			}

			// Collected afterwards...
			var max = 10; ConsoleEx.WriteLine("\n> Testing for max {0} iterations...", max);
			for (int i = 0; i < max; i++)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();

				ConsoleEx.WriteLine("- Iteration: {0} ==> {1}", i, list);
				if (list.Count == 0) break;
			}
			Assert.AreEqual(0, list.Count);
		}

		//[OnlyThisMethod]
		[TestMethod]
		public void Survive_While_Used()
		{
			var num = 5;
			var list = new SoftList<Person>();
			for (int i = 1; i <= num; i++) list.Add(new Person(i.ToString()));

			var max = 10; ConsoleEx.WriteLine("\n> Testing for max {0} iterations...", max);
			for (int i = 0; i < max; i++)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
				ConsoleEx.WriteLine("- Iteration: {0} ==> {1}", i, list);

				var target = list[0]; // Capturing the target to prevent its collection...
				Assert.IsNotNull(target);
				Assert.IsTrue(list.Count >= 1);
			}
			Assert.IsTrue(list.Count >= 1);
		}

		//[OnlyThisMethod]
		[TestMethod]
		public void Contains()
		{
			var num = 5;
			var list = new SoftList<Person>();
			for (int i = 1; i <= num; i++) list.Add(new Person(i.ToString()));

			ConsoleEx.WriteLine("\n> First collection...");
			GC.Collect();
			GC.WaitForPendingFinalizers();
			Assert.AreEqual(num, list.Count);

			ConsoleEx.WriteLine("\n> Testing not the same reference...");
			var obj = new Person("1");
			var tmp = list.Contains(obj);
			Assert.IsFalse(tmp);

			ConsoleEx.WriteLine("\n> Testing the same reference...");
			obj = list.Find(x => x.Name == "1");
			Assert.IsNotNull(obj);

			obj = list[0];
			tmp = list.Contains(obj);
			Assert.IsTrue(tmp);
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

			str = string.Format("Testing {0} Standard List...", count.ToString("#,###"));
			var std = executor(str, () =>
			{
				var list = new List<Person>(); for (long i = 0; i < count; i++)
				{
					GC.Collect();
					GC.WaitForPendingFinalizers();

					var obj = new Person("James Bond");
					list.Add(obj);
				}
			});

			str = string.Format("Testing {0} Soft List...", count.ToString("#,###"));
			var neo = executor(str, () =>
			{
				var list = new SoftList<Person>(); for (long i = 0; i < count; i++)
				{
					GC.Collect();
					GC.WaitForPendingFinalizers();

					var obj = new Person("James Bond");
					list.Add(obj);
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
