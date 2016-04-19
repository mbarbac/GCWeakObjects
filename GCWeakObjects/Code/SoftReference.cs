#undef DEBUG

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Kerosene.Tools
{
	// ====================================================
	/// <summary>
	/// Represents a soft reference to a given target that, when it is not used by any other
	/// element of the executing program, expires after the next GC collection, of when the
	/// given GC listener criteria is met.
	/// </summary>
	public class SoftReference : GCListener
	{
		object _RawTarget = null;
		WeakReference _WeakReference = null;
		
		/// <summary>
		/// Initializes a new soft reference that refers to the given target.
		/// By default this instance expires after the next GC cycle. To modify this criteria
		/// use the GC listener <see cref="GCListener.GCCycles"/> and <see cref="GCListener.GCTicks"/>
		/// properties inherited by this instance.
		/// </summary>
		/// <param name="target"></param>
		public SoftReference(object target)
		{
			if (target == null) throw new ArgumentNullException("target");
			_RawTarget = target;
			_WeakReference = new WeakReference(target);
		}

		/// <summary>
		/// Invoked each time that a GC has happened and the given notificaction criteria is
		/// met. This method is invoked potentially a huge number of times, and from a high
		/// priority finalizer thread, so these facts have to be taken into consideration when
		/// implementing this method.
		/// </summary>
		protected override void OnGCNotification()
		{
			DebugEx.WriteLine("\t- OnGCNotification({0})", this);

			if (_RawTarget == null && _WeakReference.IsAlive) _WeakReference.Target = null;
			_RawTarget = null;
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			if (_RawTarget != null) sb.Append("Hard:");
			else if (_WeakReference.Target != null) sb.Append("Weak:");
			sb.AppendFormat("[{0}]", WeakTarget ?? "-");
			return sb.ToString();
		}

		/// <summary>
		/// The target this instance refers to, or null if it has been collected.
		/// This property forces the target to survive for at least the next expiration cycle.
		/// </summary>
		public object Target
		{
			[DebuggerStepThrough]
			get { return (_RawTarget = _WeakReference.Target); }
		}

		/// <summary>
		/// Whether the target this instance refers to has been collected or not.
		/// This property does not force the target to survive any longer if it is not used.
		/// </summary>
		public bool IsAlive
		{
			[DebuggerStepThrough]
			get { return _WeakReference.IsAlive; }
		}

		/// <summary>
		/// The raw reference to the underlying target maintained by this instance. This
		/// property is null either when the target is collected, or when an expiration
		/// cycle has made its reference to be lost.
		/// </summary>
		public object RawTarget
		{
			[DebuggerStepThrough]
			get { return _RawTarget; }
		}

		/// <summary>
		/// The target this instance refers to, or null if it has been collected.
		/// This property does not force the target to survive any longer if it is not used.
		/// </summary>
		public object WeakTarget
		{
			[DebuggerStepThrough]
			get { return _WeakReference.Target; }
		}

		/// <summary>
		/// The actual weak reference this instance uses.
		/// </summary>
		public WeakReference WeakReference
		{
			[DebuggerStepThrough]
			get { return _WeakReference; }
		}
	}
}
