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
	/// Represents a dictionary where both keys and values are kept as soft references that
	/// are automatically removed and collected when not used any longer. Use the properties
	/// inherited from <see cref="GCListener"/> to set the appropriate expiration criteria.
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	public class SoftDictionary<TKey, TValue>
		: GCListener
		, IDictionary<TKey, TValue>
		where TKey : class
		where TValue : class
	{
		IEqualityComparer<TKey> _KeysComparer = null;
		IEqualityComparer<TValue> _ValuesComparer = null;
		Dictionary<int, Entry> _Dict = new Dictionary<int, Entry>();

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		public SoftDictionary()
			: this(new CustomizableComparer<TKey>()) { }

		/// <summary>
		/// Initializes a new instance, that uses the given comparer for the keys in this
		/// collection.
		/// </summary>
		public SoftDictionary(
			IEqualityComparer<TKey> keysComparer)
			: this(keysComparer, new CustomizableComparer<TValue>()) { }

		/// <summary>
		/// Initializes a new instance, that uses the given comparer for the values in this
		/// collection.
		/// </summary>
		public SoftDictionary(
			IEqualityComparer<TValue> valuesComparer)
			: this(new CustomizableComparer<TKey>(), valuesComparer) { }

		/// <summary>
		/// Initializes a new instance, that uses the given comparers for the keys and values
		/// in this collection.
		/// </summary>
		/// <param name="keysComparer"></param>
		/// <param name="valuesComparer"></param>
		public SoftDictionary(
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
			foreach (var entry in _Dict.Values) if (!entry.IsAlive) hashes.Add(entry.HashCode);
			hashes.ForEach(hash => { _Dict.Remove(hash); });
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return string.Format("Count:{0}/{1}", Count, RawCount);
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
		/// Whether this is a read-only collection or not.
		/// </summary>
		bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
		{
			get { return false; }
		}

		/// <summary>
		/// The count of raw entries in this collection.
		/// </summary>
		internal int RawCount
		{
			get { lock (SyncRoot) { return _Dict.Count; } }
		}

		/// <summary>
		/// The count of valid key and value pairs in this collection.
		/// </summary>
		public int Count
		{
			get
			{
				var count = 0; _Dict.Values.ForEach(entry => { if (entry.IsAlive) count++; });
				return count;
			}
		}

		/// <summary>
		/// Enumerates the valid key and value pairs in this collection.
		/// This property blocks this collection while it is being enumerated.
		/// </summary>
		IEnumerable<KeyValuePair<TKey, TValue>> Items
		{
			get
			{
				lock (SyncRoot)
				{
					foreach (var entry in _Dict.Values)
					{
						if (!entry.IsAlive) continue;
						var key = (TKey)entry.SoftKey.Target; if (key == null) continue;
						var value = (TValue)entry.SoftValue.Target; if (value == null) continue;

						yield return new KeyValuePair<TKey, TValue>(key, value);
					}
				}
			}
		}

		/// <summary>
		/// Returns an enumerator for the valid key and value pairs in this collection.
		/// </summary>
		/// <returns></returns>
		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			return Items.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		/// <summary>
		/// The collection of valid keys in this instance.
		/// </summary>
		public IEnumerable<TKey> Keys
		{
			get { return Items.Select(x => x.Key); }
		}
		ICollection<TKey> IDictionary<TKey, TValue>.Keys
		{
			get { return this.Keys.ToList(); }
		}

		/// <summary>
		/// The collection of valid keys in this instance.
		/// </summary>
		public IEnumerable<TValue> Values
		{
			get { return Items.Select(x => x.Value); }
		}
		ICollection<TValue> IDictionary<TKey, TValue>.Values
		{
			get { return this.Values.ToList(); }
		}

		/// <summary>
		/// Whether this collection contains the given key, or not.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public bool ContainsKey(TKey key)
		{
			if (key == null) throw new ArgumentNullException("key");
			lock (SyncRoot)
			{
				var hash = _KeysComparer.GetHashCode(key);
				Entry entry = null; if (_Dict.TryGetValue(hash, out entry))
				{
					if (!entry.IsAlive) return false;
					var target = (TKey)entry.SoftKey.Target; if (target == null) return false;
					var value = (TValue)entry.SoftValue.Target; if (value == null) return false;

					if (!_KeysComparer.Equals(key, target)) return false;
					return true;
				}
				return false;
			}
		}

		/// <summary>
		/// Whether this collection contains the given key, or not.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public bool ContainsValue(TValue value)
		{
			if (value == null) throw new ArgumentNullException("value");
			lock (SyncRoot)
			{
				foreach (var entry in _Dict.Values)
				{
					if (!entry.IsAlive) continue;
					var key = (TKey)entry.SoftKey.WeakTarget; if (key == null) continue;
					var target = (TValue)entry.SoftValue.WeakTarget; if (target == null) continue;

					if (_ValuesComparer.Equals(value, target))
					{
						key = (TKey)entry.SoftKey.Target; if (key == null) continue;
						target = (TValue)entry.SoftValue.Target; if (target == null) continue;
						return true;
					}
				}
				return false;
			}
		}

		/// <summary>
		/// Whether this collection contains the given pair, or not.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public bool Contains(TKey key, TValue value)
		{
			if (key == null) throw new ArgumentNullException("key");
			if (value == null) throw new ArgumentNullException("value");
			lock (SyncRoot)
			{
				foreach (var entry in _Dict.Values)
				{
					if (!entry.IsAlive) continue;
					var kx = (TKey)entry.SoftKey.WeakTarget; if (kx == null) continue;
					var vx = (TValue)entry.SoftValue.WeakTarget; if (vx == null) continue;

					var hash = _KeysComparer.GetHashCode(key);
					if (entry.HashCode != hash) continue;

					if (_ValuesComparer.Equals(value, vx))
					{
						kx = (TKey)entry.SoftKey.Target; if (kx == null) continue;
						vx = (TValue)entry.SoftKey.Target; if (vx == null) continue;
						return true;
					}
				}
				return false;
			}
		}

		/// <summary>
		/// Whether this collection contains the given pair, or not.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public bool Contains(KeyValuePair<TKey, TValue> item)
		{
			return Contains(item.Key, item.Value);
		}

		/// <summary>
		/// Returns a list containing the valid key and value pairs in this collection.
		/// </summary>
		/// <returns></returns>
		public List<KeyValuePair<TKey, TValue>> ToList()
		{
			return Items.ToList();
		}

		/// <summary>
		/// Copies the valid key and value pairs in this collection into the given array.
		/// </summary>
		/// <param name="array"></param>
		public void CopyTo(KeyValuePair<TKey, TValue>[] array)
		{
			var list = ToList();
			list.CopyTo(array);
		}

		/// <summary>
		/// Copies the valid key and value pairs in this collection into the given array,
		/// starting at its given index.
		/// </summary>
		/// <param name="array"></param>
		/// <param name="arrayIndex"></param>
		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			var list = ToList();
			list.CopyTo(array, arrayIndex);
		}

		/// <summary>
		/// Gets or sets the value associated with the given index.
		/// <para>
		/// The getter returns the requested value, or null if the key is found but the value
		/// has been collected.</para>
		/// <para>
		/// The setter throws an exception if the given key or value are null.</para>
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public TValue this[TKey key]
		{
			get
			{
				if (key == null) throw new ArgumentNullException("key");
				lock (SyncRoot)
				{
					var hash = _KeysComparer.GetHashCode(key);
					Entry entry = null; _Dict.TryGetValue(hash, out entry);

					if (entry == null) throw new KeyNotFoundException("Key:'{0}'".FormatWith(key));
					if (!entry.IsAlive) throw new KeyNotFoundException("Key:'{0}'".FormatWith(key));
					var target = entry.SoftKey.Target; if (target == null) throw new KeyNotFoundException("Key:'{0}'".FormatWith(key));

					var value = (TValue)entry.SoftValue.Target;
					return value;
				}
			}
			set
			{
				if (key == null) throw new ArgumentNullException("key");
				if (value == null) throw new ArgumentNullException("value");
				lock (SyncRoot)
				{
					var entry = new Entry(this, key, value);
					_Dict[entry.HashCode] = entry;
				}
			}
		}

		/// <summary>
		/// Tries to get the value of the pair whose key is given. Returns true if such a
		/// valid pair was found, or false otherwise.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public bool TryGetValue(TKey key, out TValue value)
		{
			if (key == null) throw new ArgumentNullException("key");
			value = null;
			lock (SyncRoot)
			{
				var hash = _KeysComparer.GetHashCode(key);
				Entry entry = null; _Dict.TryGetValue(hash, out entry);

				if (entry == null) return false;
				if (!entry.IsAlive) return false;
				var target = entry.SoftKey.Target; if (target == null) return false;

				value = (TValue)entry.SoftValue.Target;
				return value != null;
			}
		}

		/// <summary>
		/// Adds the given value into the bucket associated with the given key.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public void Add(TKey key, TValue value)
		{
			if (key == null) throw new ArgumentNullException("key");
			if (value == null) throw new ArgumentNullException("value");
			lock (SyncRoot)
			{
				var hash = _KeysComparer.GetHashCode(key);
				Entry entry = null; if (_Dict.TryGetValue(hash, out entry))
				{
					if (entry.SoftKey.IsAlive)
						throw new ArgumentException("Duplicate key:'{0}'.".FormatWith(key));
				}
				entry = new Entry(this, key, value);
				_Dict[entry.HashCode] = entry;
			}
		}
		void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
		{
			this.Add(item.Key, item.Value);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public bool Remove(KeyValuePair<TKey, TValue> item)
		{
			if (!Contains(item)) return false;
			return Remove(item.Key);
		}

		/// <summary>
		/// Removes the pair of the given key from this collection.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public bool Remove(TKey key)
		{
			if (key == null) throw new ArgumentNullException("key");
			lock (SyncRoot)
			{
				var hash = _KeysComparer.GetHashCode(key);
				Entry entry = null; _Dict.TryGetValue(hash, out entry);

				if (entry != null) return _Dict.Remove(entry.HashCode);
				return false;
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
		/// Represents an entry in the dictionary for its associated key and value.
		/// </summary>
		public class Entry
		{
			/// <summary>
			/// Initializes a new instance, capturing the key's hash code, and using the
			/// inherited GC Listener properties as appropriate.
			/// </summary>
			/// <param name="owner"></param>
			/// <param name="key"></param>
			/// <param name="value"></param>
			internal Entry(SoftDictionary<TKey, TValue> owner, TKey key, TValue value)
			{
				HashCode = owner._KeysComparer.GetHashCode(key);

				SoftKey = new SoftReference(key);
				SoftKey.GCCycles = owner.GCCycles;
				SoftKey.GCTicks = owner.GCTicks;

				SoftValue = new SoftReference(value);
				SoftValue.GCCycles = owner.GCCycles;
				SoftValue.GCTicks = owner.GCTicks;
			}

			/// <summary>
			/// Returns the string representation of this instance.
			/// </summary>
			/// <returns></returns>
			public override string ToString()
			{
				return string.Format("{0} => {1}", SoftKey, SoftValue);
			}

			/// <summary>
			/// The hash code used for this entry.
			/// </summary>
			public int HashCode { get; }

			/// <summary>
			/// Whether the key and value of this entry can be considered alive.
			/// </summary>
			public bool IsAlive
			{
				get { return SoftKey.IsAlive && SoftValue.IsAlive; }
			}

			/// <summary>
			/// The soft reference to the key of this entry.
			/// </summary>
			public SoftReference SoftKey { get; }

			/// <summary>
			/// The soft reference to the value of this entry.
			/// </summary>
			public SoftReference SoftValue { get; }
		}
	}
}
