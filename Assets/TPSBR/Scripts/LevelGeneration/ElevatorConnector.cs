using UnityEngine;
using Fusion;

namespace TPSBR
{
	public class ElevatorConnector : Elevator, IBlockConnector
	{
		// PUBLIC MEMBERS

		public bool IsHalfSide => true;
		public float MinHeight => _minHeight;
		public float MaxHeight => _maxHeight;
		public bool NeedsNetworkSpawn => true;

		// PRIVATE MEMBERS

		[SerializeField]
		private bool _isHalfSide;
		[SerializeField]
		private float _minHeight = 0;
		[SerializeField]
		private float _maxHeight = 1;
		[SerializeField]
		private MeshRenderer[] _renderers;

		[Networked]
		private sbyte _areaID { get; set; }

		// PUBLIC METHODS

		public void SetMaterial(int areaID, Material material)
		{
			_areaID = (sbyte)areaID;

			if (material == null)
				return;

			for (int i = 0; i < _renderers.Length; i++)
			{
				_renderers[i].material = material;
			}
		}

		public void SetHeight(float height)
		{
			OverrideHeight(-height);
		}

		// NetworkBehaviour INTERFACE

		public override void Spawned()
		{
			base.Spawned();

			if (Object.HasStateAuthority == false)
			{
				if (_areaID >= 0)
				{
					SetMaterial(_areaID, Context.NetworkGame.LevelGenerator.Areas[_areaID].Material);
				}
			}
		}
	}
}
