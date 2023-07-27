namespace TPSBR
{
	using System.Collections.Generic;
	using UnityEngine;

	public static partial class GameObjectExtensions
	{
		// PUBLIC METHODS

		public static T GetComponentNoAlloc<T>(this GameObject gameObject) where T : class
		{
			return GameObjectExtensions<T>.GetComponentNoAlloc(gameObject);
		}

		public static void SetActiveSafe(this GameObject gameObject, bool value)
		{
			if (gameObject == null)
				return;

			if (gameObject.activeSelf == value)
				return;

			gameObject.SetActive(value);
		}
	}

	public static partial class GameObjectExtensions<T> where T : class
	{
		// PRIVATE MEMBERS

		private static List<T> _components = new List<T>();

		// PUBLIC METHODS

		public static T GetComponentNoAlloc(GameObject gameObject)
		{
			_components.Clear();

			gameObject.GetComponents(_components);

			if (_components.Count > 0)
			{
				T component = _components[0];

				_components.Clear();

				return component;
			}

			return null;
		}
	}
}
