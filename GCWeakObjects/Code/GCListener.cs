#undef DEBUG

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
	/// Represents an object that is notified each time a GC happens, or when after a number
	/// of GC cycles a given notification criteria is met.
	/// </summary>
	public abstract class GCListener
	{
#if DEBUG
		static int _LastId = 0;
		int _Id = ++_LastId;
#endif
		bool _Finalized = false; object _SyncRoot = new object();
		long _GCCurrentPulses = 0;
		long _GCCycles = 0; long _LastGCCycle = 0;
		long _GCTicks = 0; long _LastGCTick = DateTime.Now.Ticks;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		protected GCListener()
		{
			new Signaller(this);
		}

		/// <summary>
		/// Finalizes this instance.
		/// </summary>
		~GCListener()
		{
			_Finalized = true;
		}

		/// <summary>
		/// Invoked after each GC to validate if the notification criteria is met and,
		/// eventually, to invoke the notification procedure.
		/// </summary>
		void OnValidateGCNotification()
		{
			if (_Finalized) return;
			_GCCurrentPulses++;
#if DEBUG
			DebugEx.WriteLine("\t\t- GCValidate:{0}, Pulses:{1}, {2}, {3} = {4}",
				_Id, _GCCurrentPulses,
				_GCCycles,
				_GCTicks,
				this);
#endif
			bool criteria = false;
			bool taken = false;

			try
			{
				Monitor.TryEnter(SyncRoot, ref taken);
				if (taken)
				{
					if (_GCCycles > 0) // Cycles criteria used...
					{
						var used = _GCCurrentPulses - _LastGCCycle; if (used >= _GCCycles)
						{
							OnGCNotification(); _LastGCCycle = _GCCurrentPulses;
							return;
						}
						criteria = true;
					}

					if (_GCTicks > 0) // Ticks criteria used...
					{
						DateTime now = DateTime.Now;
						var used = now.Ticks - _LastGCTick; if (used >= _GCTicks)
						{
							OnGCNotification(); _LastGCTick = now.Ticks;
							return;
						}
						criteria = true;
					}

					if (!criteria) OnGCNotification(); // If no criteria is used fire each GC...
				}
			}
			finally { if (taken) Monitor.Exit(SyncRoot); }
		}

		/// <summary>
		/// The object that can be used to synchronize access to this instance.
		/// </summary>
		public virtual object SyncRoot
		{
			get { return _SyncRoot; }
		}

		/// <summary>
		/// Invoked each time that a GC has happened and the given notificaction criteria is
		/// met. This method is invoked potentially a huge number of times, and from a high
		/// priority finalizer thread so these facts have to be taken into consideration when
		/// implementing this method.
		/// </summary>
		protected abstract void OnGCNotification();

		/// <summary>
		/// The current number of GC pulses this instance is aware of.
		/// </summary>
		public long GCCurrentPulses
		{
			[DebuggerStepThrough]
			get { return _GCCurrentPulses; }
		}

		/// <summary>
		/// Gets or sets the number of GC cycles that needs to happen before a new GC
		/// notification is fired.
		/// </summary>
		public long GCCycles
		{
			[DebuggerStepThrough]
			get { return _GCCycles; }
			set
			{
				if (value < 0) throw new ArgumentNullException("GC Cycles '{0}' must be cero or bigger.".FormatWith(value));
				_GCCycles = value;
			}
		}

		/// <summary>
		/// Gets or sets the number of ticks that needs to happen before a new GC notification
		/// is fired.
		/// </summary>
		public long GCTicks
		{
			[DebuggerStepThrough]
			get { return _GCTicks; }
			set
			{
				if (value < 0) throw new ArgumentNullException("GC Time Ticks '{0}' must be cero or bigger.".FormatWith(value));
				_GCTicks = value;
			}
		}

		// ================================================
		/// <summary>
		/// Used to inform its owner that a GC has happened.
		/// </summary>
		sealed class Signaller
		{
			GCListener _Owner = null;

			/// <summary>
			/// Initializes a new instance.
			/// </summary>
			/// <param name="owner"></param>
			internal Signaller(GCListener owner)
			{
				_Owner = owner;
			}

			/// <summary>
			/// Finalizes this instance informing its owning one that a GC has happened.
			/// </summary>
			~Signaller()
			{
				if (_Owner._Finalized) return;
				_Owner.OnValidateGCNotification();
				new Signaller(_Owner);
			}
		}
	}
}
