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
	/// number of values, and where the keys are kept as soft references that are removed
	/// and collected automatically when not used any longer. Use the properties
	/// inherited from <see cref="GCListener"/> to set the appropriate expiration criteria.
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	public class SoftKeyBucketDictionary<TKey, TValue>
		: GCListener
		where TKey : class
	{
		IEqualityComparer<TKey> _KeysComparer = null;
		IEqualityComparer<TValue> _ValuesComparer = null;
		Dictionary<int, Bucket> _Dict = new Dictionary<int, Bucket>();

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		public SoftKeyBucketDictionary()
			: this(new CustomizableComparer<TKey>()) { }

		/// <summary>
		/// Initializes a new instance, that uses the given comparer for the keys in this
		/// collection.
		/// </summary>
		public SoftKeyBucketDictionary(
			IEqualityComparer<TKey> keysComparer)
			: this(keysComparer, new CustomizableComparer<TValue>()) { }

		/// <summary>
		/// Initializes a new instance, that uses the given comparer for the values in this
		/// collection.
		/// </summary>
		public SoftKeyBucketDictionary(
			IEqualityComparer<TValue> valuesComparer)
			: this(new CustomizableComparer<TKey>(), valuesComparer) { }

		/// <summary>
		/// Initializes a new instance, that uses the given comparers for the keys and values
		/// in this collection.
		/// </summary>
		/// <param name="keysComparer"></param>
		/// <param name="valuesComparer"></param>
		public SoftKeyBucketDictionary(
			IEqualityComparer<TKey> keysComparer,
			IEqualityComparer<TValue> valuesComparer)
		{
			if (keysComparer == null) throw new ArgumentNullException("keysComparer");
			_KeysComparer = keysComparer;

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

			var hashes = new List<int>();
			foreach (var bucket in _Dict.Values) if (!bucket.SoftKey.IsAlive) hashes.Add(bucket.HashCode);
			hashes.ForEach(hash => { _Dict.Remove(hash); });
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			var numKeys = 0;
			lock (SyncRoot)
			{
				numKeys = _Dict.Count;
			}

			return string.Format("Keys:{0}/{1}, Values:{2}",
				KeysCount, numKeys,
				ValuesCount);
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
			get
			{
				lock (SyncRoot)
				{
					var count = 0; _Dict.Values.ForEach(bucket =>
				  {
					  if (bucket.SoftKey.IsAlive) count++;
				  });
					return count;
				}
			}
		}

		/// <summary>
		/// The number of valid values in this collection.
		/// </summary>
		public int ValuesCount
		{
			get
			{
				lock(SyncRoot)
				{
					var count = 0; _Dict.Values.ForEach(bucket => { count += bucket.Values.Count; });
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
					foreach (var bucket in _Dict.Values)
					{
						if (!bucket.SoftKey.IsAlive) continue;
						if (bucket.SoftKey.WeakTarget == null) continue;
						if (bucket.Values.Count == 0) continue;

						var key = (TKey)bucket.SoftKey.Target; if (key == null) continue;
						yield return key;
					}
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
					{
						if (!bucket.SoftKey.IsAlive) continue;
						if (bucket.SoftKey.WeakTarget == null) continue;
						var key = (TKey)bucket.SoftKey.Target; if (key == null) continue;

						lock(((ICollection)bucket.Values).SyncRoot)
							foreach (var value in bucket.Values) yield return value;
					}
				}
			}
		}

		/// <summary>
		/// Returns the soft list of values associated with the given key, or null if such
		/// cannot be found.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public List<TValue> FindList(TKey key)
		{
			if (key == null) throw new ArgumentNullException("key");
			lock (SyncRoot)
			{
				var hash = _KeysComparer.GetHashCode(key);
				Bucket bucket = null; if (_Dict.TryGetValue(hash, out bucket))
				{
					if (!bucket.SoftKey.IsAlive) return null;
					var target = (TKey)bucket.SoftKey.Target; if (target == null) return null;

					if (bucket.Values.Count == 0) return null;
					return bucket.Values;
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
				var hash = _KeysComparer.GetHashCode(key);
				Bucket bucket = null; if (!_Dict.TryGetValue(hash, out bucket))
				{
					bucket = new Bucket(this, key);
					_Dict[bucket.HashCode] = bucket;
				}
				var target = bucket.SoftKey.Target; if (target == null)
				{
					bucket = new Bucket(this, key);
					_Dict[bucket.HashCode] = bucket;
				}
				lock (((ICollection)bucket.Values).SyncRoot)
				{
					var values = bucket.Values;
					var temp = values.Find(x => _ValuesComparer.Equals(x, value));
					if (temp != null) return false;

					values.Add(value);
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
				var hash = _KeysComparer.GetHashCode(key);
				Bucket bucket = null; if (_Dict.TryGetValue(hash, out bucket))
				{
					var target = bucket.SoftKey.Target; if (target == null) return false;
					return bucket.Values.Remove(value);
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
					if (!bucket.SoftKey.IsAlive) continue;
					var weak = bucket.SoftKey.WeakTarget; if (weak == null) continue;
					var target = bucket.SoftKey.Target; if (target == null) continue;

					count += bucket.Values.RemoveAll(x => _ValuesComparer.Equals(x, value));
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
			/// Initializes a new instance, capturing the key's hash code, and using the
			/// inherited GC Listener properties as appropriate.
			/// </summary>
			/// <param name="owner"></param>
			/// <param name="key"></param>
			internal Bucket(SoftKeyBucketDictionary<TKey, TValue> owner, TKey key)
			{
				HashCode = owner._KeysComparer.GetHashCode(key);

				SoftKey = new SoftReference(key);
				SoftKey.GCCycles = owner.GCCycles;
				SoftKey.GCTicks = owner.GCTicks;

				Values = new List<TValue>();
			}

			/// <summary>
			/// Returns the string representation of this instance.
			/// </summary>
			/// <returns></returns>
			public override string ToString()
			{
				return string.Format("{0} => {1}", SoftKey, Values.Count);
			}

			/// <summary>
			/// The hash code used for this bucket.
			/// </summary>
			public int HashCode { get; }

			/// <summary>
			/// The soft reference to the key of this bucket.
			/// </summary>
			public SoftReference SoftKey { get; }

			/// <summary>
			/// The list that maintains the values in this bucket.
			/// </summary>
			public List<TValue> Values { get; }
		}
	}
}
