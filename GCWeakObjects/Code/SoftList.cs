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
	/// Represents a list whose elements are kept as soft references that are automatically
	/// removed and collected when not used any longer. Use the properties inherited from
	/// <see cref="GCListener"/> to set the appropriate expiration criteria.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class SoftList<T> : GCListener, IList<T> where T : class
	{
		List<SoftReference> _List = new List<SoftReference>();
		IEqualityComparer<T> _Comparer = null;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		public SoftList() : this(new CustomizableComparer<T>()) { }

		/// <summary>
		/// Initializes a new instance that uses the given comparer with the items in this
		/// collection.
		/// </summary>
		/// <param name="comparer"></param>
		public SoftList(IEqualityComparer<T> comparer)
		{
			if (comparer == null) throw new ArgumentNullException("comparer");
			_Comparer = comparer;
		}

		/// <summary>
		/// Initializes a new instance populated with the items in the given range.
		/// </summary>
		/// <param name="range"></param>
		public SoftList(IEnumerable<T> range) : this(range, new CustomizableComparer<T>()) { }

		/// <summary>
		/// Initializes a new instance populated with the items in the given range, and that
		/// uses the given comparer with the items in this collection.
		/// </summary>
		/// <param name="range"></param>
		/// <param name="comparer"></param>
		public SoftList(IEnumerable<T> range, IEqualityComparer<T> comparer) : this(comparer)
		{
			AddRange(range);
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

			var entries = new List<SoftReference>();
			foreach (var entry in _List) if (!entry.IsAlive) entries.Add(entry);
			entries.ForEach(entry => { _List.Remove(entry); });
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			var countEntries = 0;
			lock (SyncRoot) { countEntries = _List.Count; }

			return string.Format("Count:{0}/{1}", Count, countEntries);
		}

		/// <summary>
		/// The object that can be used to synchronize access to this collection.
		/// </summary>
		public override object SyncRoot
		{
			[DebuggerStepThrough]
			get { return ((ICollection)_List).SyncRoot; }
		}

		/// <summary>
		/// Whether this is a read-only collection or not.
		/// </summary>
		bool ICollection<T>.IsReadOnly
		{
			[DebuggerStepThrough]
			get { return false; }
		}

		/// <summary>
		/// The number of valid items currently in this collection.
		/// </summary>
		public int Count
		{
			get
			{
				var count = 0;
				lock (SyncRoot) { foreach (var entry in _List) if (entry.IsAlive) count++; }
				return count;
			}
		}

		/// <summary>
		/// The number of raw items currently in this collection.
		/// </summary>
		internal int RawCount
		{
			get
			{
				var count = 0;
				lock (SyncRoot) { foreach (var entry in _List) count++; }
				return count;
			}
		}

		/// <summary>
		/// Returns a new enumerator for the valid entries in instance that, when enumerated,
		/// blocks this collection.
		/// </summary>
		/// <returns></returns>
		public IEnumerator<T> GetEnumerator()
		{
			lock (SyncRoot)
			{
				foreach (var entry in _List)
				{
					if (!entry.IsAlive) continue;
					if (entry.WeakTarget == null) continue;

					var target = (T)entry.Target; if (target == null) continue;
					yield return target;
				}
			}
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		/// <summary>
		/// Whether this collection contains the given item or not.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public bool Contains(T item)
		{
			return Find(x => _Comparer.Equals(x, item)) != null;
		}

		/// <summary>
		/// Returns the first item in this collection that matches the given predicate, or
		/// null if no item can be found.
		/// </summary>
		/// <param name="predicate"></param>
		/// <returns></returns>
		public T Find(Predicate<T> predicate)
		{
			if (predicate == null) throw new ArgumentNullException("predicate");
			lock (SyncRoot)
			{
				foreach (var entry in _List)
				{
					if (!entry.IsAlive) continue;

					var weak = (T)entry.WeakTarget; if (weak == null) continue;
					if (!predicate(weak)) continue;

					var target = (T)entry.Target; if (target == null) continue;
					return target;
				}
				return null;
			}
		}

		/// <summary>
		/// Returns the last item in this collection that matches the given predicate, or
		/// null if no item can be found.
		/// </summary>
		/// <param name="predicate"></param>
		/// <returns></returns>
		public T FindLast(Predicate<T> predicate)
		{
			if (predicate == null) throw new ArgumentNullException("predicate");
			T last = null; lock (SyncRoot)
			{
				foreach (var entry in _List)
				{
					if (!entry.IsAlive) continue;

					var weak = (T)entry.WeakTarget; if (weak == null) continue;
					if (!predicate(weak)) continue;

					var target = (T)entry.Target; if (target == null) continue;
					last = target;
				}
				return last;
			}
		}

		/// <summary>
		/// Returns all the items in this collection that matches the given predicate, or
		/// null if no item can be found.
		/// </summary>
		/// <param name="predicate"></param>
		/// <returns></returns>
		public IEnumerable<T> FindAll(Predicate<T> predicate)
		{
			if (predicate == null) throw new ArgumentNullException("predicate");
			lock (SyncRoot)
			{
				foreach (var entry in _List)
				{
					if (!entry.IsAlive) continue;

					var weak = (T)entry.WeakTarget; if (weak == null) continue;
					if (!predicate(weak)) continue;

					var target = (T)entry.Target; if (target == null) continue;
					yield return target;
				}
			}
		}

		/// <summary>
		/// Returns the first index at which the given item is stored, or -1 if it does not
		/// belong to this collection.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public int IndexOf(T item)
		{
			if (item == null) throw new ArgumentNullException("item");
			lock (SyncRoot)
			{
				int i = 0; foreach (var entry in _List)
				{
					if (!entry.IsAlive) continue;
					var weak = (T)entry.WeakTarget; if (weak == null) { i++; continue; }
					if (!_Comparer.Equals(item, weak)) { i++; continue; }

					var target = (T)entry.Target; if (target == null) { i++; continue; }
					return i;
				}
				return -1;
			}
		}

		/// <summary>
		/// Returns the last index at which the given item is stored, or -1 if it does not
		/// belong to this collection.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public int LastIndexOf(T item)
		{
			if (item == null) throw new ArgumentNullException("item");
			int last = -1; lock (SyncRoot)
			{
				int i = 0; foreach (var entry in _List)
				{
					if (!entry.IsAlive) continue;
					var weak = (T)entry.WeakTarget; if (weak == null) { i++; continue; }
					if (!_Comparer.Equals(item, weak)) { i++; continue; }

					var target = (T)entry.Target; if (target == null) { i++; continue; }
					last = i;
				}
				return last;
			}
		}

		/// <summary>
		/// Gets or sets the value associated with the given index.
		/// <para>The getter might return null if the item at that index is collected but not
		/// yet removed.</para>
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public T this[int index]
		{
			get
			{
				lock (SyncRoot)
				{
					var entry = _List[index];
					if (!entry.IsAlive) return null;

					var target = (T)entry.Target;
					return target;
				}
			}
			set
			{
				if (value == null) throw new ArgumentNullException("value");
				lock (SyncRoot)
				{
					var entry = new SoftReference(value)
					{
						GCCycles = this.GCCycles,
						GCTicks = this.GCTicks
					};
					_List[index] = entry;
				}
			}
		}

		/// <summary>
		/// Returns a list with the valid items in this collection.
		/// </summary>
		/// <returns></returns>
		public List<T> ToList()
		{
			var list = new List<T>();
			var iter = GetEnumerator(); while (iter.MoveNext()) list.Add(iter.Current);
			return list;
		}

		/// <summary>
		/// Returns an array with the valid items in this collection.
		/// </summary>
		/// <returns></returns>
		public T[] ToArray()
		{
			return this.ToList().ToArray();
		}

		/// <summary>
		/// Copies the valid targets in this collection into the given array.
		/// </summary>
		/// <param name="array"></param>
		public void CopyTo(T[] array)
		{
			var list = ToList();
			list.CopyTo(array);
		}

		/// <summary>
		/// Copies the valid targets in this collection into the given array, starting at
		/// the given index.
		/// </summary>
		/// <param name="array"></param>
		/// <param name="arrayIndex"></param>
		public void CopyTo(T[] array, int arrayIndex)
		{
			var list = ToList();
			list.CopyTo(array, arrayIndex);
		}

		/// <summary>
		/// Reverses the order of the elements in this collection.
		/// </summary>
		public void Reverse()
		{
			lock (SyncRoot) { _List.Reverse(); }
		}

		/// <summary>
		/// Adds the given target at the end of this collection.
		/// </summary>
		/// <param name="item"></param>
		public void Add(T item)
		{
			if (item == null) throw new ArgumentNullException("item");
			lock (SyncRoot)
			{
				var entry = new SoftReference(item);
				entry.GCCycles = this.GCCycles;
				entry.GCTicks = this.GCTicks;

				_List.Add(entry);
			}
		}

		/// <summary>
		/// Inserts the given target at the index on this collection.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="item"></param>
		public void Insert(int index, T item)
		{
			if (item == null) throw new ArgumentNullException("item");
			lock (SyncRoot)
			{
				var entry = new SoftReference(item);
				entry.GCCycles = this.GCCycles;
				entry.GCTicks = this.GCTicks;

				_List.Insert(index, entry);
			}
		}

		/// <summary>
		/// Adds the given range of items into this collection.
		/// </summary>
		/// <param name="range"></param>
		public void AddRange(IEnumerable<T> range)
		{
			if (range == null) throw new ArgumentNullException("range");
			lock (SyncRoot)
			{
				int i = 0; foreach (var item in range)
				{
					if (item == null) throw new ArgumentException("Item #{0} is null.".FormatWith(i));

					var entry = new SoftReference(item);
					entry.GCCycles = this.GCCycles;
					entry.GCTicks = this.GCTicks;

					_List.Add(entry);
					i++;
				}
			}
		}

		/// <summary>
		/// Removes the first ocurrence of the given item from this collection.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public bool Remove(T item)
		{
			if (item == null) throw new ArgumentNullException("item");
			lock (SyncRoot)
			{
				foreach (var entry in _List)
				{
					if (!entry.IsAlive) continue;

					var weak = (T)entry.WeakTarget; if (weak == null) continue;
					if (!_Comparer.Equals(item, weak)) continue;

					return _List.Remove(entry);
				}
				return false;
			}
		}

		/// <summary>
		/// Removes all the ocurrences of the given item from this collection. Returns the
		/// number of ocurrences removed.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public int RemoveAll(T item)
		{
			if (item == null) throw new ArgumentNullException("item");
			lock (SyncRoot)
			{
				var count = 0; foreach (var entry in _List)
				{
					if (!entry.IsAlive) continue;

					var weak = (T)entry.WeakTarget; if (weak == null) continue;
					if (!_Comparer.Equals(item, weak)) continue;

					if (_List.Remove(entry)) count++;
				}
				return count;
			}
		}

		/// <summary>
		/// Removes from this collection the entry at the given index.
		/// Note that this method does not make any adjustments in the index and so the entry
		/// removed may refer to either a valid or an invalid item.
		/// </summary>
		/// <param name="index"></param>
		public void RemoveAt(int index)
		{
			lock (SyncRoot) { _List.RemoveAt(index); }
		}

		/// <summary>
		/// Trims this collection.
		/// </summary>
		public void TrimExcess()
		{
			lock (SyncRoot) { _List.TrimExcess(); }
		}

		/// <summary>
		/// Clears this collection.
		/// </summary>
		public void Clear()
		{
			lock (SyncRoot) { _List.Clear(); }
		}
	}
}
