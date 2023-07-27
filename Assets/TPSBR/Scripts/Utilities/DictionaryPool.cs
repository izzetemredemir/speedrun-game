namespace TPSBR
{
	using System.Collections.Generic;
	using System.Runtime.CompilerServices;

	public partial class DictionaryPool<K, V>
	{
		// CONSTANTS

		private const int POOL_CAPACITY = 4;

		// PUBLIC MEMBERS

		public static readonly DictionaryPool<K, V> Shared = new DictionaryPool<K, V>();

		// PRIVATE MEMBERS

		private List<Dictionary<K, V>> _pool = new List<Dictionary<K, V>>(POOL_CAPACITY);

		// PUBLIC METHODS

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Dictionary<K, V> Get()
		{
			lock (_pool)
			{
				var poolCount = _pool.Count;

				if (poolCount == 0)
				{
					return new Dictionary<K, V>();
				}

				var lastIndex      = poolCount - 1;
				var lastDictionary = _pool[lastIndex];

				_pool.RemoveAt(lastIndex);

				return lastDictionary;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Return(Dictionary<K, V> dictionary)
		{
			if (dictionary == null)
				return;

			dictionary.Clear();

			lock (_pool)
			{
				_pool.Add(dictionary);
			}
		}
	}

	public static class DictionaryPool
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Dictionary<K, V> Get<K, V>()
		{
			return DictionaryPool<K, V>.Shared.Get();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Return<K, V>(Dictionary<K, V> dictionary)
		{
			DictionaryPool<K, V>.Shared.Return(dictionary);
		}
	}
}
