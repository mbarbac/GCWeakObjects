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
	/// Represents a dictionary where each key is associated with a bucket of an arbitrary
	/// number of values, and where the values are kept as soft references that are
	/// automatically removed and collected when not used any longer, and the keys are also
	/// removed when their associated bucket is empty. Use the properties inherited from
	/// <see cref="GCListener"/> to set the appropriate expiration criteria.
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	public class SoftValueBucketDictionary<TKey, TValue>
		: GCListener
		where TValue : class
	{
		IEqualityComparer<TValue> _ValuesComparer = null;
		Dictionary<TKey, Bucket> _Dict = null;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		public SoftValueBucketDictionary()
			: this(new CustomizableComparer<TKey>()) { }

		/// <summary>
		/// Initializes a new instance, that uses the given comparer for the keys in this
		/// collection.
		/// </summary>
		public SoftValueBucketDictionary(
			IEqualityComparer<TKey> keysComparer)
			: this(keysComparer, new CustomizableComparer<TValue>()) { }

		/// <summary>
		/// Initializes a new instance, that uses the given comparer for the values in this
		/// collection.
		/// </summary>
		public SoftValueBucketDictionary(
			IEqualityComparer<TValue> valuesComparer)
			: this(new CustomizableComparer<TKey>(), valuesComparer) { }

		/// <summary>
		/// Initializes a new instance, that uses the given comparers for the keys and values
		/// in this collection.
		/// </summary>
		/// <param name="keysComparer"></param>
		/// <param name="valuesComparer"></param>
		public SoftValueBucketDictionary(
			IEqualityComparer<TKey> keysComparer,
			IEqualityComparer<TValue> valuesComparer)
		{
			if (keysComparer == null) throw new ArgumentNullException("keysComparer");
			_Dict = new Dictionary<TKey, Bucket>(keysComparer);

			if (valuesComparer == null) throw new ArgumentNullException("valuesComparer");
			_ValuesComparer = valuesComparer;
		}

		/// <summary>
		/// Invoked each time that a GC has happened and the given notificaction criteria is
		/// met. This method is invoked potentially a huge number of times, and from a high
		/// priority finalizer thread so these facts have to be taken into consideration when
		/// implementing this method.
		/// </summary>
		protected override void OnGCNotification()
		{
			DebugEx.WriteLine("\t- OnGCNotification({0})", this);

			var keys = new List<TKey>();
			foreach (var bucket in _Dict.Values) if (bucket.SoftValues.Count == 0) keys.Add(bucket.Key);
			keys.ForEach(key => { _Dict.Remove(key); });
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			var numValues = 0;
			lock (SyncRoot)
			{
				foreach (var bucket in _Dict.Values) numValues += bucket.SoftValues.RawCount;
			}

			return string.Format("Keys:{0}, Values:{1}/{2}",
				KeysCount,
				ValuesCount, numValues);
		}

		/// <summary>
		/// The object that can be used to synchronize access to this collection.
		/// </summary>
		public override object SyncRoot
		{
			[DebuggerStepThrough]
			get { return ((ICollection)_Dict).SyncRoot; }
		}

		/// <summary>
		/// The number of valid keys in this collection.
		/// </summary>
		public int KeysCount
		{
			get { lock (SyncRoot) { return _Dict.Count; } }
		}

		/// <summary>
		/// The number of valid values in this collection.
		/// </summary>
		public int ValuesCount
		{
			get
			{
				lock (SyncRoot)
				{
					var count = 0; _Dict.Values.ForEach(bucket =>
					{
						count += bucket.SoftValues.Count;
					});
					return count;
				}
			}
		}

		/// <summary>
		/// The collection of valid keys in this instance.
		/// </summary>
		public IEnumerable<TKey> Keys
		{
			get
			{
				lock (SyncRoot)
				{
					foreach (var key in _Dict.Keys) yield return key;
				}
			}
		}

		/// <summary>
		/// The collection of valid keys in this instance.
		/// </summary>
		public IEnumerable<TValue> Values
		{
			get
			{
				lock (SyncRoot)
				{
					foreach (var bucket in _Dict.Values)
						foreach (var value in bucket.SoftValues) yield return value;
				}
			}
		}

		/// <summary>
		/// Returns the soft list of values associated with the given key, or null if such
		/// cannot be found.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public SoftList<TValue> FindList(TKey key)
		{
			if (key == null) throw new ArgumentNullException("key");
			lock (SyncRoot)
			{
				Bucket bucket = null; if (_Dict.TryGetValue(key, out bucket))
				{
					if (bucket.SoftValues.Count == 0) return null;
					return bucket.SoftValues;
				}
				return null;
			}
		}

		/// <summary>
		/// Adds the given value into the bucket associated with the given key. Returns true
		/// if the value is efectively added, or false if it was already present in the
		/// bucket.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public bool Add(TKey key, TValue value)
		{
			if (key == null) throw new ArgumentNullException("key");
			if (value == null) throw new ArgumentNullException("value");
			lock (SyncRoot)
			{
				Bucket bucket = null; if (!_Dict.TryGetValue(key, out bucket))
				{
					bucket = new Bucket(this, key);
					_Dict[key] = bucket;
				}
				lock (bucket.SoftValues.SyncRoot)
				{
					if (bucket.SoftValues.Contains(value)) return false;
					bucket.SoftValues.Add(value);
					return true;
				}
			}
		}

		/// <summary>
		/// Removes the given value from the bucket whose key is given.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public bool Remove(TKey key, TValue value)
		{
			if (key == null) throw new ArgumentNullException("key");
			if (value == null) throw new ArgumentNullException("value");
			lock (SyncRoot)
			{
				Bucket bucket = null; if (_Dict.TryGetValue(key, out bucket))
				{
					return bucket.SoftValues.Remove(value);
				}
				return false;
			}
		}

		/// <summary>
		/// Removes all ocurrences of the value from this collection. Returns the number of
		/// ocurrences removed.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public int RemoveAll(TValue value)
		{
			if (value == null) throw new ArgumentNullException("value");
			lock (SyncRoot)
			{
				int count = 0; foreach (var bucket in _Dict.Values)
				{
					count += bucket.SoftValues.RemoveAll(value);
				}
				return count;
			}
		}

		/// <summary>
		/// Clears this collection.
		/// </summary>
		public void Clear()
		{
			lock (SyncRoot) { _Dict.Clear(); }
		}

		// ================================================
		/// <summary>
		/// Represents a bucket in the dictionary associated with the given key.
		/// </summary>
		public class Bucket
		{
			/// <summary>
			/// Initializes a new instance, using the inherited GC Listener properties as
			/// appropriate.
			/// </summary>
			/// <param name="owner"></param>
			/// <param name="key"></param>
			internal Bucket(SoftValueBucketDictionary<TKey, TValue> owner, TKey key)
			{
				Key = key;
				SoftValues = new SoftList<TValue>(owner._ValuesComparer);
				SoftValues.GCCycles = owner.GCCycles;
				SoftValues.GCTicks = owner.GCTicks;
			}

			/// <summary>
			/// Returns the string representation of this instance.
			/// </summary>
			/// <returns></returns>
			public override string ToString()
			{
				return string.Format("{0} => {1}", Key, SoftValues);
			}

			/// <summary>
			/// The key of this bucket.
			/// </summary>
			public TKey Key { get; }

			/// <summary>
			/// The soft list that maintains the values in this bucket.
			/// </summary>
			public SoftList<TValue> SoftValues { get; }
		}
	}
}
