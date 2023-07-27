namespace TPSBR
{
	using UnityEngine;

	public static partial class ComponentExtensions
	{
		// PUBLIC METHODS

		public static T GetComponentNoAlloc<T>(this Component component) where T : class
		{
			return GameObjectExtensions<T>.GetComponentNoAlloc(component.gameObject);
		}

		public static void SetActive(this Component component, bool value)
		{
			if (component == null)
				return;

			if (component.gameObject.activeSelf == value)
				return;

			component.gameObject.SetActive(value);
		}
	}
}
