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
	public class Test_SoftReference
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
			var item = new Person("Cervantes");
			var soft = new SoftReference(item);
			Assert.IsTrue(soft.IsAlive);
			Assert.IsNotNull(soft.WeakTarget);
			Assert.IsNotNull(soft.RawTarget);

			ConsoleEx.WriteLine("\n> First collection...");
			item = null;
			GC.Collect();
			GC.WaitForPendingFinalizers();
			Assert.IsTrue(soft.IsAlive);
			Assert.IsNotNull(soft.WeakTarget);
			Assert.IsTrue(soft.RawTarget == null);

			ConsoleEx.WriteLine("\n> Second collection...");
			GC.Collect();
			GC.WaitForPendingFinalizers();
			Assert.IsFalse(soft.IsAlive);
			Assert.IsTrue(soft.WeakTarget == null);
			Assert.IsTrue(soft.RawTarget == null);
		}

		//[OnlyThisMethod]
		[TestMethod]
		public void Survive_Many_Cycles()
		{
			var cycles = 2;
			var item = new Person("Cervantes");
			var soft = new SoftReference(item) { GCCycles = cycles };

			ConsoleEx.WriteLine("Survive for the given number of cycles...");
			for (int i = 0; i < cycles; i++)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();

				ConsoleEx.WriteLine("- Iteration: {0} ==> {1}", i, soft);
				Assert.IsTrue(soft.IsAlive);
			}

			var max = 10; ConsoleEx.WriteLine("\n> Testing for max {0} iterations...", max);
			for (int i = 0; i < max; i++)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();

				ConsoleEx.WriteLine("- Iteration: {0} ==> {1}", i, soft);
				if (!soft.IsAlive) break;
			}
			Assert.IsFalse(soft.IsAlive);
			Assert.IsTrue(soft.GCCurrentPulses == (cycles * 2));
		}

		//[OnlyThisMethod]
		[TestMethod]
		public void Survive_While_Used()
		{
			var item = new Person("Cervantes");
			var soft = new SoftReference(item);

			var max = 10; ConsoleEx.WriteLine("\n> Testing for max {0} iterations...", max);
			item = null;
			for (int i = 0; i < max; i++)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
				ConsoleEx.WriteLine("- Iteration: {0} ==> {1}", i, soft);

				var target = soft.Target;
				Assert.IsNotNull(target);
				Assert.IsTrue(soft.IsAlive);
			}
			Assert.IsNotNull(soft.WeakTarget);
			Assert.IsNotNull(soft.RawTarget);
			Assert.IsTrue(soft.IsAlive);
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

			str = string.Format("Testing {0} Weak References...", count.ToString("#,###"));
			var std = executor(str, () =>
			{
				for (long i = 0; i < count; i++)
				{
					GC.Collect();
					GC.WaitForPendingFinalizers();

					var obj = new Person(i.ToString());
					var tmp = new WeakReference(obj);
					obj = null;
				}
			});

			str = string.Format("Testing {0} Soft References...", count.ToString("#,###"));
			var neo = executor(str, () =>
			{
				for (long i = 0; i < count; i++)
				{
					GC.Collect();
					GC.WaitForPendingFinalizers();

					var obj = new Person(i.ToString());
					var tmp = new SoftReference(obj);
					obj = null;
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
