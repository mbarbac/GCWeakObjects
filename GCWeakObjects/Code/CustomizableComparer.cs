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
	/// Provides a default equality comparer implementation whose comparison and hash code
	/// generation capabilities can be customized and modified as needed.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class CustomizableComparer<T> : IEqualityComparer<T>
	{
		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		public CustomizableComparer() { }

		/// <summary>
		/// If not null the delegate to use to compare the two given objects. If null then
		/// a default one is used.
		/// </summary>
		public Func<T, T, bool> OnEqualsDelegate { get; set; }

		/// <summary>
		/// If not null the delegate to use to obtain the hash code of the given objectt. If
		/// null then a default one is used.
		/// </summary>
		public Func<T, int> OnGetHashCodeDelegate { get; set; }

		/// <summary>
		/// Determines whether the given objects can be considered equal or not.
		/// </summary>
		/// <param name="objA"></param>
		/// <param name="objB"></param>
		/// <returns></returns>
		public bool Equals(T objA, T objB)
		{
			if (OnEqualsDelegate != null) return OnEqualsDelegate(objA, objB);

			var type = typeof(T); if (type.IsValueType)
			{
				return objA.Equals(objB);
			}
			if (type == typeof(string))
			{
				var strA = (string)(object)objA;
				var strB = (string)(object)objB;
				return strA == strB;
			}
			return ReferenceEquals(objA, objB);
		}

		/// <summary>
		/// Returns a hash code for the given object.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public int GetHashCode(T obj)
		{
			return OnGetHashCodeDelegate != null
				? OnGetHashCodeDelegate(obj)
				: obj.GetHashCode();
		}
	}
}
