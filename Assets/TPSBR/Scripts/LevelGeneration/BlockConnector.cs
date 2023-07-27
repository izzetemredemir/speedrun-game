using Fusion;
using UnityEngine;

namespace TPSBR
{
	public class BlockConnector : MonoBehaviour, IBlockConnector
	{
		// PUBLIC MEMBERS

		public bool  IsHalfSide        => _isHalfSide;
		public float MinHeight         => _minHeight;
		public float MaxHeight         => _maxHeight;
		public bool  NeedsNetworkSpawn => false;

		// PRIVATE MEMBERS

		[SerializeField]
		private bool _isHalfSide;
		[SerializeField]
		private float _minHeight = 0;
		[SerializeField]
		private float _maxHeight = 1;
		[SerializeField]
		private MeshRenderer[] _renderers;

		// PUBLIC METHODS

		public void SetMaterial(int areaID, Material material)
		{
			if (material == null)
				return;

			for (int i = 0; i < _renderers.Length; i++)
			{
				_renderers[i].material = material;
			}
		}

		public void SetHeight(float height)
		{
		}
	}
}
