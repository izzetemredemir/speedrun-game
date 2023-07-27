using System.Collections.Generic;
using UnityEngine;
using Fusion;

namespace TPSBR
{
	public struct SpawnData
	{
		public NetworkBehaviour Prefab;
		public Vector3          Position;
		public Quaternion       Rotation;

		public bool             IsConnector;
		public int              AreaID;
		public Material         Material;
		public float            Height;

		public SpawnData(NetworkBehaviour prefab, Vector3 position, Quaternion rotation)
		{
			Prefab = prefab;
			Position = position;
			Rotation = rotation;

			IsConnector = false;
			AreaID = -1;
			Material = null;
			Height = 0f;
		}
	}

	public interface IBlockConnector
	{
		public bool IsHalfSide { get; }
		public float MinHeight { get; }
		public float MaxHeight { get; }
		public bool NeedsNetworkSpawn { get; }

		public void SetMaterial(int areaID, Material material);
		public void SetHeight(float height);
	}

	public class LevelGenerator : CoreBehaviour
	{
		// PUBLIC MEMBERS

		public AreaSetup[]     Areas => _areas;

		public List<SpawnData> ObjectsToSpawn  => _objectsToSpawn;
		public Vector2Int      Dimensions      => Vector2Int.one * _size * _blockSize;
		public Vector3         Center          => new Vector3(_size * _blockSize * 0.5f - _blockSize * 0.5f, 0f, _size * _blockSize * 0.5f - _blockSize * 0.5f);
		public int             BlockSize       => _blockSize;

		// PRIVATE MEMBERS

		[Header("Surface")]
		[SerializeField]
		private int _blockSize = 26;
		[SerializeField]
		private int _maxHeight = 30;
		[SerializeField]
		private float _noiseScale = 0.9f;
		[SerializeField]
		private BlockSpawn _areaBlockSpawn;
		[SerializeField]
		private BlockSpawn _flatlandBlockSpawn;
		[SerializeField]
		private BlockSpawn _coastBlockSpawn;
		[SerializeField]
		private Transform _water;

		[Header("Areas")]
		[SerializeField]
		private AreaSetup[] _areas;
		[SerializeField, Tooltip("How much of the total space will be occupied by Areas if not overlapping")]
		private float _areaOccupancy = 0.5f;
		[SerializeField, Range(0f, 1f)]
		private float _randomizeAreaSize = 0.2f;
		[SerializeField, Range(0f, 1f)]
		private float _randomizeAreaHeight = 0.2f;
		[SerializeField]
		private Vector2 _flatlandInfluence = new Vector2(0f, 0.1f);
		[SerializeField]
		private Vector2 _coastInfluence = new Vector2(-0.03f, -0.01f);

		[Header("Connectors")]
		[SerializeField]
		private GameObject[] _connectorPrefabs;

		[Header("Object Spawn")]
		[SerializeField]
		private ItemBox _itemBoxPrefab;
		[SerializeField]
		private Vector2 _areaSpawnChance = new Vector2(0.1f, 0.8f);
		[SerializeField]
		private float _flatlandSpawnChance = 0.01f;

		private IBlockConnector[][] _sortedConnectorPrefabs;

		private BlockData[,] _blocks;
		private List<SpawnData> _objectsToSpawn = new List<SpawnData>(1024);

		private Vector3[] _rotations = { new Vector3(0f, 0f, 0f), new Vector3(0f, 90f, 0f), new Vector3(0f, 180f, 0f), new Vector3(0f, 270f, 0f)};

		private int _size;
		private int _areaCount;

		// PUBLIC METHODS

		public void Generate(int seed, int size, int areaCount)
		{
			_size = size;
			_areaCount = areaCount;

			Random.InitState(seed);

			_blocks = new BlockData[size, size];

			GenerateAreas();
			GenerateAreaConnections();
			SpawnBlocks();

			SortConnectorPrefabs();
			SpawnConnectors();

			_water.position = new Vector3(Center.x, _water.position.y, Center.z);
			_water.localScale = new Vector3(Dimensions.x, 1f, Dimensions.y);
		}

		// PRIVATE METHODS

		private LevelBlock GenerateRandomBlock(Vector3 position, BlockSpawn blockSpawn)
		{
			return GenerateBlock(blockSpawn.GetBlockPrefab(), position);
		}

		private LevelBlock GenerateBlock(LevelBlock blockPrefab, Vector3 position)
		{
			var rotation = Quaternion.Euler(_rotations[Random.Range(0, _rotations.Length)]);
			var block = Instantiate(blockPrefab, position, rotation, transform);

			if (block.AllowMirror == true && Random.value > 0.5f)
			{
				block.transform.localScale = new Vector3(-1f, 1f, 1f);
			}

			return block;
		}

		private void GenerateAreas()
		{
			float areaSize = (_size * _size * _areaOccupancy * _blockSize * _blockSize) / _areaCount;
			float areaRadius = Mathf.Sqrt(areaSize / Mathf.PI);

			float areaRadiusMin = Mathf.Max(_blockSize, areaRadius - _randomizeAreaSize * areaRadius);
			float areaRadiusMax = areaRadius + _randomizeAreaSize * areaRadius;

			for (int areaID = 0; areaID < _areaCount; areaID++)
			{
				var area = _areas[areaID];
				area.Radius = Random.Range(areaRadiusMin, areaRadiusMax);
				area.MaxHeight = Random.Range(Mathf.Max(1f, _maxHeight - _randomizeAreaHeight * _maxHeight), _maxHeight + _randomizeAreaHeight * _maxHeight);

				Debug.Log($"Area {areaID}, Radius {area.Radius}, Max Height {area.MaxHeight}");

				// Aditional radius for flatland(white) blocks
				float flatlandRadius = area.Radius > _blockSize * 3f ? area.Radius + _blockSize * 2f : area.Radius + _blockSize;

				int landRadiusBlocks = Mathf.RoundToInt(flatlandRadius / _blockSize);

				int centerAreaStart = Mathf.Min(landRadiusBlocks, Mathf.RoundToInt(_size * 0.5f - 1f));
				int centerAreaEnd = Mathf.Max(_size - landRadiusBlocks - 1, Mathf.RoundToInt(_size * 0.5f));

				area.Center = GetRandomAreaCenter(new Vector2Int(centerAreaStart, centerAreaStart), new Vector2Int(centerAreaEnd, centerAreaEnd));

				var start = area.Center - new Vector2Int(landRadiusBlocks, landRadiusBlocks);
				start.x = Mathf.Max(0, start.x);
				start.y = Mathf.Max(0, start.y);

				var end = area.Center + new Vector2Int(landRadiusBlocks, landRadiusBlocks);
				end.x = Mathf.Min(_size - 1, end.x);
				end.y = Mathf.Min(_size - 1, end.y);

				Vector2 areaCenter = area.Center;

				float sqrRadius = area.Radius * area.Radius;
				float sqrFlatlandRadius = flatlandRadius * flatlandRadius;

				for (int x = start.x; x <= end.x; x++)
				{
					for (int z = start.y; z <= end.y; z++)
					{
						var blockPosition = new Vector2(x, z);
						var direction = (blockPosition - areaCenter) * _blockSize;

						float sqrDistance = direction.sqrMagnitude;

						if (sqrDistance > sqrFlatlandRadius)
							continue;

						var block = _blocks[x, z];

						if (sqrDistance > sqrRadius)
						{
							block.IsFlatland = true;
							_blocks[x, z] = block;

							continue;
						}

						float distance = direction.magnitude;
						float influence = 1f - distance / area.Radius;

						if (influence > block.AreaInfluence)
						{
							block.AreaID = areaID;
							block.AreaInfluence = influence;
						}
						else
						{
							block.IsFlatland = true;
						}

						_blocks[x, z] = block;
					}
				}
			}
		}

		private void GenerateAreaConnections()
		{
			for (int areaID = 0; areaID < _areaCount; areaID++)
			{
				var areaCenter = _areas[areaID].Center;

				int targetAreaID = (areaID + 1) % _areaCount;
				var targetAreaCenter = _areas[targetAreaID].Center;

				var currentPosition = areaCenter;

				while (currentPosition != targetAreaCenter)
				{
					if (currentPosition.x != targetAreaCenter.x)
					{
						currentPosition.x += currentPosition.x < targetAreaCenter.x ? 1 : -1;
					}

					if (currentPosition.y != targetAreaCenter.y)
					{
						currentPosition.y += currentPosition.y < targetAreaCenter.y ? 1 : -1;
					}

					var block = _blocks[currentPosition.x, currentPosition.y];

					if (block.AreaID == targetAreaID)
						break; // Next area reached

					if (block.AreaInfluence <= 0f && block.IsFlatland == false)
					{
						block.IsFlatland = true;

						_blocks[currentPosition.x, currentPosition.y] = block;
					}
				}
			}
		}

		private void SpawnBlocks()
		{
			var boxSpawnPositions = ListPool.Get<Transform>(128);

			for (int x = 0; x < _size; x++)
			{
				for (int z = 0; z < _size; z++)
				{
					var block = _blocks[x, z];

					bool spawnBoxes = false;

					if (block.AreaInfluence > 0.999f) // Area center
					{
						float yNormalized = Mathf.PerlinNoise(x * _noiseScale, z * _noiseScale);
						float yPosition = Mathf.Round(yNormalized * _areas[block.AreaID].MaxHeight * block.AreaInfluence);

						block.Block = GenerateBlock(_areas[block.AreaID].CenterBlock, new Vector3(x * _blockSize, yPosition, z * _blockSize));
						spawnBoxes = true;
					}
					else if (block.AreaInfluence > 0f) // Area
					{
						float yNormalized = Mathf.PerlinNoise(x * _noiseScale, z * _noiseScale);
						float yPosition = Mathf.Round(yNormalized * _areas[block.AreaID].MaxHeight * block.AreaInfluence);

						block.Block = GenerateRandomBlock(new Vector3(x * _blockSize, yPosition, z * _blockSize), _areaBlockSpawn);

						float itemBoxChance = Mathf.Lerp(_areaSpawnChance.x, _areaSpawnChance.y, block.AreaInfluence);
						spawnBoxes = block.Block.AlwaysSpawnBoxes == true || Random.value < itemBoxChance;
					}
					else if (block.IsFlatland == true)
					{
						float yNormalized = Mathf.PerlinNoise(x * _noiseScale, z * _noiseScale);
						float yPosition = Mathf.Round(yNormalized * _maxHeight * Random.Range(_flatlandInfluence.x, _flatlandInfluence.y));

						block.Block = GenerateRandomBlock(new Vector3(x * _blockSize, yPosition, z * _blockSize), _flatlandBlockSpawn);

						spawnBoxes = block.Block.AlwaysSpawnBoxes == true || Random.value < _flatlandSpawnChance;
					}
					else if (IsCoastBlock(x, z) == true)
					{
						float yNormalized = Mathf.PerlinNoise(x * _noiseScale, z * _noiseScale);
						float yPosition = yNormalized * _maxHeight * Random.Range(_coastInfluence.x, _coastInfluence.y);

						block.Block = GenerateRandomBlock(new Vector3(x * _blockSize, yPosition, z * _blockSize), _coastBlockSpawn);
						block.Block.EnableSpawnPoints(false);

						spawnBoxes = block.Block.AlwaysSpawnBoxes;
					}

					if (block.AreaID >= 0)
					{
						block.Block.SetMaterial(_areas[block.AreaID].Material);
					}

					if (spawnBoxes == true)
					{
						boxSpawnPositions.Clear();
						int spawnCount = block.Block.GetRandomItemBoxPositions(boxSpawnPositions);

						for (int i = 0; i < spawnCount; i++)
						{
							var boxPosition = boxSpawnPositions[i];
							_objectsToSpawn.Add(new SpawnData(_itemBoxPrefab, boxPosition.position, boxPosition.rotation));
						}
					}

					_blocks[x, z] = block;
				}
			}

			ListPool.Return(boxSpawnPositions);
		}

		private void SortConnectorPrefabs()
		{
			if (_sortedConnectorPrefabs != null)
				return;

			int maxConnectors = _connectorPrefabs.Length;

			var connectorPrefabs = ListPool.Get<IBlockConnector>(maxConnectors);
			var connectors = ListPool.Get<IBlockConnector>(maxConnectors);

			for (int i = 0; i < maxConnectors; i++)
			{
				connectorPrefabs.Add(_connectorPrefabs[i].GetComponent<IBlockConnector>());
			}

			// Create array of possible connectors for every possible height
			int maxHeight = Mathf.RoundToInt(_maxHeight + _randomizeAreaHeight * _maxHeight);
			_sortedConnectorPrefabs = new IBlockConnector[maxHeight][];

			for (int i = 0; i < maxHeight; i++)
			{
				int height = i + 1;

				for (int j = 0; j < maxConnectors; j++)
				{
					var connector = connectorPrefabs[j];

					if (connector.MinHeight > height)
						continue;

					if (connector.MaxHeight < height)
						continue;

					connectors.Add(connector);
				}

				_sortedConnectorPrefabs[i] = connectors.ToArray();

				connectors.Clear();
			}

			ListPool.Return(connectorPrefabs);
			ListPool.Return(connectors);
		}

		private void SpawnConnectors()
		{
			for (int x = 0; x < _size; x++)
			{
				for (int z = 0; z < _size; z++)
				{
					var currentBlock = _blocks[x, z];

					if (x < _size - 1)
					{
						var rightBlock = _blocks[x + 1, z];
						PlaceConnector(new Vector2Int(x, z), currentBlock, new Vector2Int(x + 1, z), rightBlock);
					}

					if (z < _size - 1)
					{
						var topBlock = _blocks[x, z + 1];
						PlaceConnector(new Vector2Int(x, z), currentBlock, new Vector2Int(x, z + 1), topBlock);
					}
				}
			}
		}

		private bool PlaceConnector(Vector2Int aCoordinates, BlockData blockA, Vector2Int bCoordinates, BlockData blockB)
		{
			if (blockA.Block == null || blockB.Block == null)
				return false;

			var blockAPosition = blockA.Block.transform.position;
			var blockBPosition = blockB.Block.transform.position;

			int groundHeight = Mathf.RoundToInt(blockBPosition.y - blockAPosition.y);

			Vector2Int coordinatesDirection = bCoordinates - aCoordinates;
			var aHeights = blockA.Block.GetAdditionalHeights(coordinatesDirection);
			var bHeights = blockB.Block.GetAdditionalHeights(-coordinatesDirection);

			if (groundHeight == 0 && ((aHeights.Left == bHeights.Right && aHeights.Left >= 0) || (aHeights.Right == bHeights.Left && aHeights.Right >= 0)))
				return false; // Blocks have same height at least on one half of the side

			bool leftSideValid = aHeights.Left >= 0 && bHeights.Right >= 0;
			bool rightSideValid = aHeights.Right >= 0 && bHeights.Left >= 0;

			if (leftSideValid == false && rightSideValid == false)
				return false;

			int leftSideHeight = leftSideValid == true ? groundHeight - aHeights.Left + bHeights.Right : int.MaxValue;
			int rightSideHeight = rightSideValid == true ? groundHeight - aHeights.Right + bHeights.Left : int.MaxValue;

			bool useLeftSide = leftSideHeight != rightSideHeight ? Mathf.Abs(leftSideHeight) < Mathf.Abs(rightSideHeight) : Random.value > 0.5f;

			int height = useLeftSide == true ? leftSideHeight : rightSideHeight;
			bool fromAtoB = height < 0;

			// We need clear space on both halfs of the lower block to use full side connector
			bool canUseFullSide = fromAtoB == true ? bHeights.Left == bHeights.Right : aHeights.Left == aHeights.Right;

			var connectorPrefab = GetConnector(Mathf.Abs(height), canUseFullSide == false);
			if (connectorPrefab == null)
				return false;

			var direction = fromAtoB == true ? blockBPosition - blockAPosition : blockAPosition - blockBPosition;
			direction.y = 0f;

			var position = fromAtoB == true ? blockAPosition + direction * 0.5f : blockBPosition + direction * 0.5f;
			position.y += fromAtoB == true ? (useLeftSide == true ? aHeights.Left : aHeights.Right) : (useLeftSide == true ? bHeights.Right : bHeights.Left);

			var rotation = Quaternion.LookRotation(direction);
			var scale = Vector3.one;

			if (connectorPrefab.IsHalfSide == true)
			{
				var rightVector = -Vector3.Cross(direction, Vector3.up).normalized * _blockSize * 0.25f;

				if ((fromAtoB == true && useLeftSide == true) || (fromAtoB == false && useLeftSide == false))
				{
					// Left side from FromBlock perspective
					position -= rightVector;
					scale.x = -1f; // Flip it for left side
				}
				else
				{
					// Right side from FromBlock perspective
					position += rightVector;
				}
			}
			else if (leftSideHeight == rightSideHeight)
			{
				// For full side with same left-right height on both blocks switch orientation randomly
				scale.x = Random.value > 0.5f ? - 1f : 1f;
			}
			else
			{
				// One half of full side connector entrance is blocked, choose non blocked side (full side connectors are always oriented from right to left side)
				if ((fromAtoB == true && useLeftSide == true) || (fromAtoB == false && useLeftSide == false))
				{
					scale.x = -1;
				}
			}
			
			// Avoid Z-fighting issues
			position.y -= Random.Range(0f, 0.001f);
			position.x += Random.Range(-0.0005f, 0.0005f);
			position.z += Random.Range(-0.0005f, 0.0005f);

			var fromBlock = fromAtoB == true ? blockA : blockB;
			var material = fromBlock.AreaID >= 0 ? _areas[fromBlock.AreaID].Material : null;

			if (connectorPrefab.NeedsNetworkSpawn == false)
			{
				var connector = Instantiate(connectorPrefab as MonoBehaviour, position, rotation, transform);
				connector.transform.localScale = scale;

				var blockConnector = connector as IBlockConnector;
				blockConnector.SetMaterial(fromBlock.AreaID, material);
				blockConnector.SetHeight(Mathf.Abs(height));
			}
			else
			{
				var spawnData = new SpawnData((connectorPrefab as NetworkBehaviour), position, rotation);
				spawnData.IsConnector = true;
				spawnData.AreaID = fromBlock.AreaID;
				spawnData.Material = material;
				spawnData.Height = Mathf.Abs(height);

				_objectsToSpawn.Add(spawnData);
			}

			return true;
		}

		private IBlockConnector GetConnector(int height, bool halfSideOnly)
		{
			int index = height - 1;

			if (index < 0 || index >= _sortedConnectorPrefabs.GetLength(0))
				return null;

			var connectors = _sortedConnectorPrefabs[index];

			if (connectors.Length == 0)
				return null;

			int connectorCount = connectors.Length;
			int connectorIndex = Random.Range(0, connectorCount);
			var connector = connectors[connectorIndex];

			if (halfSideOnly == false || connector.IsHalfSide == true)
				return connector;

			for (int i = 1; i < connectorCount; i++)
			{
				connector = connectors[(connectorIndex + index) % connectorCount];
				if (connector.IsHalfSide == true)
					return connector;
			}

			return null;
		}

		private bool IsCoastBlock(int x, int z)
		{
			if (x < _size - 1)
			{
				if (_blocks[x + 1, z].IsFlatland == true)
					return true;

				if (z < _size - 1 && _blocks[x + 1, z + 1].IsFlatland == true)
					return true;

				if (z > 0 && _blocks[x + 1, z - 1].IsFlatland == true)
					return true;
			}

			if (x > 0)
			{
				if (_blocks[x - 1, z].IsFlatland == true)
					return true;

				if (z < _size - 1 && _blocks[x - 1, z + 1].IsFlatland == true)
					return true;

				if (z > 0 && _blocks[x - 1, z - 1].IsFlatland == true)
					return true;
			}

			if (z < _size - 1 && _blocks[x, z + 1].IsFlatland == true)
				return true;

			if (z > 0 && _blocks[x, z - 1].IsFlatland == true)
				return true;

			return false;
		}

		private Vector2Int GetRandomAreaCenter(Vector2Int rangeStart, Vector2Int rangeEnd)
		{
			Vector2Int center = Vector2Int.zero;

			// Just randomly try diffent centers to not overlap other areas
			for (int i = 0; i < 20; i++)
			{
				center = new Vector2Int(Random.Range(rangeStart.x, rangeEnd.x + 1), Random.Range(rangeStart.y, rangeEnd.y + 1));

				if (IsAreaCenter(center) == false)
					return center;
			}

			// Try neighbours
			int start = Random.Range(0, 8);
			for (int i = 0; i < 8; i++)
			{
				Vector2Int neighbour= center;
				int index = (i + start) % 8;

				switch (index)
				{
					case 0: neighbour.y += 1; break;
					case 1: neighbour.y += 1; neighbour.x += 1; break;
					case 2: neighbour.x += 1; break;
					case 3: neighbour.y -= 1; neighbour.x += 1; break;
					case 4: neighbour.y -= 1; break;
					case 5: neighbour.y -= 1; neighbour.x -= 1; break;
					case 6: neighbour.x -= 1; break;
					case 7: neighbour.y += 1; neighbour.x -= 1; break;
				}

				if (neighbour.x < 0 || neighbour.y < 0 || neighbour.x >= _size || neighbour.y >= _size)
					continue;

				if (IsAreaCenter(neighbour) == false)
					return neighbour;
			}

			Debug.LogWarning("Unique area center not found");

			return center;
		}

		private bool IsAreaCenter(Vector2Int position)
		{
			for (int areaID = 0; areaID < _areaCount; areaID++)
			{
				var area = _areas[areaID];

				if (area.Radius <= 0f) // Area (and areas after that) not initialized yet
					return false;

				if (area.Center == position)
					return true;
			}

			return false;
		}

		// HELPERS

		[System.Serializable]
		public class AreaSetup
		{
			public LevelBlock CenterBlock;
			public Material Material;

			public Vector2Int Center           { get; set; }
			public float      Radius           { get; set; }
			public float      MaxHeight        { get; set; }
		}

		[System.Serializable]
		private class BlockSpawn
		{
			public BlockSpawnItem[] Blocks;

			private int _totalProbability = -1;

			public LevelBlock GetBlockPrefab()
			{
				if (_totalProbability < 0)
				{
					_totalProbability = 0;

					for (int i = 0; i < Blocks.Length; i++)
					{
						_totalProbability += Blocks[i].Probability;
					}
				}

				if (_totalProbability == 0)
					return null;

				int targetProbability = Random.Range(0, _totalProbability);
				int currentProbability = 0;

				for (int i = 0; i < Blocks.Length; i++)
				{
					currentProbability += Blocks[i].Probability;

					if (targetProbability < currentProbability)
					{
						return Blocks[i].Block;
					}
				}

				return null;
			}
		}

		[System.Serializable]
		private class BlockSpawnItem
		{
			public LevelBlock Block;
			public int        Probability = 1;
		}

		public struct BlockData
		{
			public int        AreaID          { get { return _areaSet == true ? _areaID : -1; } set { _areaID = value; _areaSet = true; } }
			public float      AreaInfluence;
			public bool       IsFlatland;
			public LevelBlock Block;

			private int _areaID;
			private bool _areaSet;
		}
	}
}
