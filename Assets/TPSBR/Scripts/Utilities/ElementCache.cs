namespace TPSBR
{
	using System.Collections.Generic;
	using UnityEngine;

	public class ElementCache<T> where T : Component
	{
		// PUBLIC MEMBERS

		public T this[int index] { get { return GetElement(index); } }

		// PRIVATE MEMBERS

		private T       m_Prefab;
		private List<T> m_Elements;

		// C-TOR

		public ElementCache(T prefab, int initCount)
		{
			m_Prefab = prefab;
			m_Elements = new List<T>(initCount * 2);

			m_Prefab.SetActive(false);

			while (initCount >= m_Elements.Count)
			{
				CreateElement();
			}
		}

		// PUBLIC METHODS

		public T GetElement(int index)
		{
			while (index >= m_Elements.Count)
			{
				CreateElement();
			}

			var element = m_Elements[index];
			element.SetActive(true);
			return element;
		}

		public void HideAll(int startIndex = 0)
		{
			for (int idx = startIndex, count = m_Elements.Count; idx < count; idx++)
			{
				m_Elements[idx].SetActive(false);
			}
		}

		// PRIVATE METHODS

		private void CreateElement()
		{
			var instance = Object.Instantiate(m_Prefab, m_Prefab.transform.parent);
			m_Elements.Add(instance);
		}
	}
}
