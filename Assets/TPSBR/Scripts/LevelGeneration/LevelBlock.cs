using Fusion.KCC;
using System.Collections.Generic;
using UnityEngine;

namespace TPSBR
{
	public class LevelBlock : CoreBehaviour
	{
		// PUBLIC MEMBERS

		public bool AlwaysSpawnBoxes => _alwaysSpawnBoxes;
		public Transform[] ItemBoxPositions => _itemBoxPositions;
		public bool AllowMirror => _allowMirror;

		// PRIVATE MEMBERS

		[SerializeField]
		private SpawnPoint[] _spawnPoints;
		[SerializeField]
		private MeshRenderer[] _renderers;
		[SerializeField]
		private Transform[] _itemBoxPositions;
		[SerializeField]
		private bool _alwaysSpawnBoxes;
		[SerializeField]
		private int _minSpawnedBoxes;
		[SerializeField]
		private int _maxSpawnedBoxes;
		[SerializeField]
		private int[] _additionalHeights = new int[8];
		[SerializeField]
		private bool _allowMirror = true;

		// PUBLIC METHODS

		public void SetMaterial(Material material)
		{
			for (int i = 0; i < _renderers.Length; i++)
			{
				_renderers[i].material = material;
			}
		}

		public void EnableSpawnPoints(bool value)
		{
			for (int i = 0; i < _spawnPoints.Length; i++)
			{
				_spawnPoints[i].SpawnEnabled = value;
			}
		}

		public int GetRandomItemBoxPositions(List<Transform> boxPositions)
		{
			int maxPositions = _itemBoxPositions.Length;

			if (maxPositions == 0)
				return 0;

			boxPositions.AddRange(_itemBoxPositions);

			if (_maxSpawnedBoxes == 0)
				return maxPositions; // All positions

			boxPositions.Shuffle();
			return Random.Range(_minSpawnedBoxes, Mathf.Min(_maxSpawnedBoxes, maxPositions) + 1);
		}

		public (int Left, int Right) GetAdditionalHeights(Vector2Int side)
		{
			// There are two hights (left, right) for each block side
			// See OnDrawGizmos method to check how heights are indexed

			int startIndex = 0;

			if (side == Vector2Int.right)
			{
				startIndex = 2;
			}
			else if (side == Vector2Int.down)
			{
				startIndex = 4;
			}
			else if (side == Vector2Int.left)
			{
				startIndex = 6;
			}

			float rotation = (360f + transform.rotation.eulerAngles.y) % 360f; // Make sure rotation is not negative
			int indexShift = (16 - Mathf.RoundToInt(rotation / 90f) * 2) % 8;
			startIndex = (startIndex + indexShift) % 8;

			if (transform.localScale.x > 0f)
				return (_additionalHeights[startIndex], _additionalHeights[startIndex + 1]);

			// For flipped blocks we need to flip the index and go back in the array
			return (_additionalHeights[(8 - startIndex + 1) % 8], _additionalHeights[(8 - startIndex) % 8]);
		}

		// MONOBEHAVIOUR

		private Vector3[] _drawPositions = new Vector3[]
		{
			new Vector3(-6f, 0f, 12f),
			new Vector3(6f, 0f, 12f),
			new Vector3(12f, 0f, 6f),
			new Vector3(12f, 0f, -6f),
			new Vector3(6f, 0f, -12f),
			new Vector3(-6f, 0f, -12f),
			new Vector3(-12f, 0f, -6f),
			new Vector3(-12f, 0f, 6f),
		};

		private void OnDrawGizmos()
		{
			if (Application.isPlaying == true)
				return;

			var previousColor = Gizmos.color;
			Gizmos.color = Color.blue;

			for (int i = 0; i < _additionalHeights.Length; i++)
			{
				int additionaHeight = _additionalHeights[i];

				if (additionaHeight < 0)
					continue;

				var position = transform.position + _drawPositions[i];
				position.y += additionaHeight;

				Gizmos.DrawWireSphere(position, 0.5f);
			}

			Gizmos.color = previousColor;
		}
	}
}
