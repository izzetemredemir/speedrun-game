namespace TPSBR
{
	using System.Collections.Generic;
	using System.Runtime.CompilerServices;

	public partial class Pool<T> where T : new()
	{
		// CONSTANTS

		private const int POOL_CAPACITY = 4;

		// PUBLIC MEMBERS

		public static readonly Pool<T> Shared = new Pool<T>();

		// PRIVATE MEMBERS

		private List<T> _pool = new List<T>(POOL_CAPACITY);

		// PUBLIC METHODS

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T Get()
		{
			bool found = false;
			T    item  = default;

			lock (_pool)
			{
				int index = _pool.Count - 1;
				if (index >= 0)
				{
					found = true;
					item  = _pool[index];

					_pool.RemoveBySwap(index);
				}
			}

			if (found == false)
			{
				item = new T();
			}

			return item;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Return(T item)
		{
			if (item == null)
				return;

			lock (_pool)
			{
				_pool.Add(item);
			}
		}
	}

	public static class Pool
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T Get<T>() where T : new()
		{
			return Pool<T>.Shared.Get();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Return<T>(T item) where T : new()
		{
			Pool<T>.Shared.Return(item);
		}
	}
}
