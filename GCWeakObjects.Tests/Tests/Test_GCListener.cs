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
	public class Test_GCListener
	{
		/// <summary>
		/// An example of a GC listerner.
		/// </summary>
		public class Listener : GCListener
		{
			public int Signals { get; private set; }
			public Listener() : base() { }
			public override string ToString() { return string.Format("Pulses={0}, Signals={1}", GCCurrentPulses, Signals); }
			protected override void OnGCNotification() { Signals += 1; }
		}

		// ================================================

		//[OnlyThisMethod]
		[TestMethod]
		public void Notified_Each_Cycle()
		{
			var obj = new Listener();
			var max = 10; for (int i = 0; i < max; i++)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
				ConsoleEx.WriteLine("- Iteration: {0} ==> {1}", i, obj);
				Assert.IsTrue(obj.GCCurrentPulses == obj.Signals);
			}
		}

		//[OnlyThisMethod]
		[TestMethod]
		public void Notified_After_Many_Cycles()
		{
			var cycles = 4;
			var obj = new Listener() { GCCycles = cycles };

			var max = cycles * 2;
			ConsoleEx.WriteLine("- Executing... please wait for {0} cycles...", cycles);
			for (int i = 0; i < max; i++)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
				ConsoleEx.WriteLine("- Iteration: {0} ==> {1}", i, obj);
				if (obj.Signals != 0) break;
			}

			Assert.IsTrue(obj.Signals == 1);
			Assert.IsTrue(obj.GCCurrentPulses == cycles);
		}

		//[OnlyThisMethod]
		[TestMethod]
		public void Notified_After_Time()
		{
			var span = TimeSpan.FromSeconds(3);
			var obj = new Listener() { GCTicks = span.Ticks };

			ConsoleEx.WriteLine("- Executing... please wait for {0}", span);
			var start = DateTime.Now;
			while ((DateTime.Now.Ticks - start.Ticks) < (span.Ticks * 2))
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
				if (obj.Signals != 0) break;
			}
			var finish = DateTime.Now;
			var used = finish - start;
			ConsoleEx.WriteLine("- Used: {0}", used);

			Assert.IsTrue(obj.Signals == 1);
			Assert.IsTrue(used.Ticks > (span.Ticks * 0.95)); // 5% allowance
			Assert.IsTrue(used.Ticks < (span.Ticks * 1.1)); // 10% allowance
		}
	}
}
