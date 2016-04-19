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
	/// Represents a dictionary whose values are kept as soft references that are automatically
	/// removed and collected when not used any longer. Use the properties inherited from
	/// <see cref="GCListener"/> to set the appropriate expiration criteria.
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	public class SoftValueDictionary<TKey, TValue>
		: GCListener
		, IDictionary<TKey, TValue>
		where TValue : class
	{
		IEqualityComparer<TKey> _KeysComparer = null;
		IEqualityComparer<TValue> _ValuesComparer = null;
		Dictionary<TKey, SoftReference> _Dict = null;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		public SoftValueDictionary()
			: this(new CustomizableComparer<TKey>()) { }

		/// <summary>
		/// Initializes a new instance, that uses the given comparer for the keys in this
		/// collection.
		/// </summary>
		public SoftValueDictionary(
			IEqualityComparer<TKey> keysComparer)
			: this(keysComparer, new CustomizableComparer<TValue>()) { }

		/// <summary>
		/// Initializes a new instance, that uses the given comparer for the values in this
		/// collection.
		/// </summary>
		public SoftValueDictionary(
			IEqualityComparer<TValue> valuesComparer)
			: this(new CustomizableComparer<TKey>(), valuesComparer) { }

		/// <summary>
		/// Initializes a new instance, that uses the given comparers for the keys and values
		/// in this collection.
		/// </summary>
		/// <param name="keysComparer"></param>
		/// <param name="valuesComparer"></param>
		public SoftValueDictionary(
			IEqualityComparer<TKey> keysComparer,
			IEqualityComparer<TValue> valuesComparer)
		{
			if (keysComparer == null) throw new ArgumentNullException("keysComparer");
			_KeysComparer = keysComparer;

			if (valuesComparer == null) throw new ArgumentNullException("valuesComparer");
			_ValuesComparer = valuesComparer;

			_Dict = new Dictionary<TKey, SoftReference>(_KeysComparer);
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
			foreach (var kvp in _Dict) if (!kvp.Value.IsAlive) keys.Add(kvp.Key);
			keys.ForEach(key => { _Dict.Remove(key); });
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
				var count = 0; foreach (var entry in _Dict.Values) if (entry.IsAlive) count++;
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
					foreach(var kvp in _Dict)
					{
						var entry = kvp.Value;
						if (!entry.IsAlive) continue;
						var value = (TValue)entry.Target; if (value == null) continue;

						yield return new KeyValuePair<TKey, TValue>(kvp.Key, value);
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
			get { return _Dict.Keys; }
		}
		ICollection<TKey> IDictionary<TKey, TValue>.Keys
		{
			get { return _Dict.Keys; }
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
			lock (SyncRoot)
			{
				SoftReference entry = null; if (_Dict.TryGetValue(key, out entry))
				{
					if (!entry.IsAlive) return false;
					var value = entry.Target; if (value == null) return false;

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
				foreach(var entry in _Dict.Values)
				{
					if (!entry.IsAlive) continue;
					var target = (TValue)entry.WeakTarget; if (target == null) continue;

					if (_ValuesComparer.Equals(value, target))
					{
						target = (TValue)entry.Target; if (target == null) continue;
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
			if (value == null) throw new ArgumentNullException("value");
			lock (SyncRoot)
			{
				foreach (var entry in _Dict.Values)
				{
					if (!entry.IsAlive) continue;
					var vx = (TValue)entry.WeakTarget; if (vx == null) continue;

					if (_ValuesComparer.Equals(value, vx))
					{
						vx = (TValue)entry.Target; if (vx == null) continue;
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
				lock (SyncRoot)
				{
					SoftReference entry = null; _Dict.TryGetValue(key, out entry);

					if (entry == null) throw new KeyNotFoundException("Key:'{0}'".FormatWith(key));
					if (!entry.IsAlive) throw new KeyNotFoundException("Key:'{0}'".FormatWith(key));

					var value = (TValue)entry.Target;
					return value;
				}
		}
			set
			{
				if (value == null) throw new ArgumentNullException("value");
				lock (SyncRoot)
				{
					var entry = new SoftReference(value);
					entry.GCCycles = this.GCCycles;
					entry.GCTicks = this.GCTicks;

					_Dict[key] = entry;
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
			value = null;
			lock (SyncRoot)
			{
				SoftReference entry = null; _Dict.TryGetValue(key, out entry);

				if (entry == null) return false;
				if (!entry.IsAlive) return false;

				value = (TValue)entry.Target;
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
			if (value == null) throw new ArgumentNullException("value");
			lock (SyncRoot)
			{
				var entry = new SoftReference(value);
				entry.GCCycles = this.GCCycles;
				entry.GCTicks = this.GCTicks;

				_Dict.Add(key, entry);
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
				return _Dict.Remove(key);
			}
		}

		/// <summary>
		/// Clears this collection.
		/// </summary>
		public void Clear()
		{
			lock (SyncRoot) { _Dict.Clear(); }
		}
	}
}
