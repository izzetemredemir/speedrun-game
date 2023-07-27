namespace TPSBR
{
	using System.Collections.Generic;
	using System.Runtime.CompilerServices;
	using UnityEngine;

	public static partial class IListExtensions
	{
		// PUBLIC METHODS

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int IndexOf<T>(this IList<T> list, T item)
		{
			return list.IndexOf(item);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Contains<T>(this IList<T> list, T item)
		{
			return list.Contains(item);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void AddUnique<T>(this IList<T> list, T item)
		{
			if (list.Contains(item) == false)
			{
				list.Add(item);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Clear<T>(this IList<T> list)
		{
			for (int i = 0, count = list.Count; i < count; ++i)
			{
				list[i] = default;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Swap<T>(this IList<T> list, int indexA, int indexB)
		{
			T value = list[indexA];
			list[indexA] = list[indexB];
			list[indexB] = value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Replace<T>(this IList<T> list, T item, T replacement)
		{
			int index = list.IndexOf(item);
			if (index >= 0)
			{
				list[index] = replacement;
				return true;
			}

			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int SafeCount<T>(this IList<T> list)
		{
			return list != null ? list.Count : 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T LastOrDefault<T>(this IList<T> list)
		{
			int count = list.SafeCount();

			if (count == 0)
				return default(T);

			return list[count - 1];
		}

		// O(1)
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool RemoveBySwap<T>(this IList<T> list, int index)
		{
			list[index] = list[list.Count - 1];
			list.RemoveAt(list.Count - 1);
			return true;
		}

		// O(n)
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool RemoveBySwap<T>(this IList<T> list, T item)
		{
			int index = list.IndexOf(item);
			if (index >= 0)
			{
				RemoveBySwap(list, index);
				return true;
			}

			return false;
		}

		// O(n)
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool RemoveBySwap<T>(this List<T> list, System.Predicate<T> predicate)
		{
			int index = list.FindIndex(predicate);
			if (index >= 0)
			{
				RemoveBySwap(list, index);
				return true;
			}

			return false;
		}

		public static T Find<T>(this IList<T> list, System.Predicate<T> condition)
		{
			int count = list.Count;

			for (int i = 0; i < count; i++)
			{
				if (condition.Invoke(list[i]) == true)
					return list[i];
			}

			return default;
		}

		public static T GetRandom<T>(this IList<T> list)
		{
			int count = list.Count;
			if (count == 0)
				return default;

			return list[Random.Range(0, count)];
		}

		public static void Shuffle<T>(this IList<T> list)
		{
			int n = list.Count - 1;

			while (n > 0)
			{
				int k = Random.Range(0, n);

				T value = list[k];

				list[k] = list[n];
				list[n] = value;

				--n;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T PopLast<T>(this IList<T> list)
		{
			int count = list.SafeCount();

			if (count == 0)
				return default(T);

			var element = list[count - 1];
			list.RemoveAt(count - 1);
			return element;
		}
	}
}
