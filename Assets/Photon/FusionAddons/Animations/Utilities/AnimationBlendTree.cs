namespace Fusion.Animations
{
	using UnityEngine;

	public sealed class AnimationBlendTree
	{
		// PUBLIC MEMBERS

		public float   Size    => _size;
		public float[] Weights => _weights;

		// PRIVATE MEMBERS

		private int         _count;
		private float       _size;
		private float       _scale;
		private float[]     _weights;
		private Vector2[]   _basePositions;
		private float[]     _baseMagnitudes;
		private Vector2[]   _scaledPositions;
		private float[]     _scaledMagnitudes;
		private Vector2[][] _scaledPolarDistances;
		private float[][]   _inverseAverageMagnitudes;

		// CONSTRUCTORS

		public AnimationBlendTree(Vector2[] positions)
		{
			_count = positions.Length;

			_weights                  = new float[_count];
			_basePositions            = new Vector2[_count];
			_baseMagnitudes           = new float[_count];
			_scaledPositions          = new Vector2[_count];
			_scaledMagnitudes         = new float[_count];
			_scaledPolarDistances     = new Vector2[_count][];
			_inverseAverageMagnitudes = new float[_count][];

			for (int i = 0; i < _count; ++i)
			{
				_scaledPolarDistances[i]     = new Vector2[_count];
				_inverseAverageMagnitudes[i] = new float[_count];
			}

			for (int i = 0; i < _count; ++i)
			{
				_basePositions[i]  = positions[i];
				_baseMagnitudes[i] = positions[i].magnitude;

				_size = Mathf.Max(_size, _baseMagnitudes[i]);
			}

			_scale = 1.0f;

			PrecalculateWeights();
		}

		// PUBLIC METHODS

		public void SetPositions(Vector2[] positions)
		{
			_count = positions.Length;

			_weights                  = new float[_count];
			_basePositions            = new Vector2[_count];
			_baseMagnitudes           = new float[_count];
			_scaledPositions          = new Vector2[_count];
			_scaledMagnitudes         = new float[_count];
			_scaledPolarDistances     = new Vector2[_count][];
			_inverseAverageMagnitudes = new float[_count][];

			for (int i = 0; i < _count; ++i)
			{
				_scaledPolarDistances[i]     = new Vector2[_count];
				_inverseAverageMagnitudes[i] = new float[_count];
			}

			for (int i = 0; i < _count; ++i)
			{
				_basePositions[i]  = positions[i];
				_baseMagnitudes[i] = positions[i].magnitude;
			}

			PrecalculateWeights();
		}

		public void SetScale(float scale)
		{
			_scale = scale;

			PrecalculateWeights();
		}

		public void CalculateWeights(Vector2 position)
		{
			float positionMagnitude = position.magnitude;
			float accumulatedWeight = 0.0f;

			for (int i = 0; i < _count; ++i)
			{
				float     weight                   = 1.0f;
				float     positionAngle            = GetAngleFast(_scaledPositions[i], position);
				float     positionPolarDistance    = positionMagnitude - _scaledMagnitudes[i];
				Vector2[] scaledPolarDistances     = _scaledPolarDistances[i];
				float[]   inverseAverageMagnitudes = _inverseAverageMagnitudes[i];

				for (int j = 0; j < _count; ++j)
				{
					if (i != j)
					{
						Vector2 scaledPolarDistanceAtoB = scaledPolarDistances[j];
						Vector2 scaledPolarDistanceAtoP = new Vector2(positionPolarDistance * inverseAverageMagnitudes[j], positionAngle);

						float desiredWeight = 1.0f - scaledPolarDistanceAtoB.x * scaledPolarDistanceAtoP.x - scaledPolarDistanceAtoB.y * scaledPolarDistanceAtoP.y;
						if (desiredWeight < weight)
						{
							weight = desiredWeight;
						}
					}
				}

				if (weight < 0.0f)
				{
					weight = 0.0f;
				}

				_weights[i] = weight;

				accumulatedWeight += weight;
			}

			if (accumulatedWeight > 0.0f)
			{
				float accumulatedWeightInverse = 1.0f / accumulatedWeight;

				for (int i = 0; i < _count; ++i)
				{
					_weights[i] *= accumulatedWeightInverse;
				}
			}
		}

		// PRIVATE METHODS

		private void PrecalculateWeights()
		{
			for (int i = 0; i < _count; ++i)
			{
				_scaledPositions[i]  = _basePositions[i]  * _scale;
				_scaledMagnitudes[i] = _baseMagnitudes[i] * _scale;
			}

			for (int i = 0; i < _count; ++i)
			{
				Vector2   scaledPositionA           = _scaledPositions[i];
				float     scaledMagnitudeA          = _scaledMagnitudes[i];
				Vector2[] scaledPolarDistancesA     = _scaledPolarDistances[i];
				float[]   inverseAverageMagnitudesA = _inverseAverageMagnitudes[i];

				for (int j = 0; j < _count; ++j)
				{
					Vector2   scaledPositionB           = _scaledPositions[j];
					float     scaledMagnitudeB          = _scaledMagnitudes[j];
					Vector2[] scaledPolarDistancesB     = _scaledPolarDistances[j];
					float[]   inverseAverageMagnitudesB = _inverseAverageMagnitudes[j];

					float averageMagnitude        = (scaledMagnitudeA + scaledMagnitudeB) * 0.5f;
					float inverseAverageMagnitude = 1.0f / averageMagnitude;

					float angle         = GetAngle(scaledPositionA, scaledPositionB);
					float polarDistance = scaledMagnitudeB - scaledMagnitudeA;

					Vector2 scaledPolarDistanceAtoB = new Vector2(polarDistance * inverseAverageMagnitude, angle);

					scaledPolarDistanceAtoB /= scaledPolarDistanceAtoB.sqrMagnitude;

					scaledPolarDistancesA[j] = scaledPolarDistanceAtoB;
					scaledPolarDistancesB[i] = -scaledPolarDistanceAtoB;

					inverseAverageMagnitudesA[j] = inverseAverageMagnitude;
					inverseAverageMagnitudesB[i] = inverseAverageMagnitude;
				}
			}
		}

		private static float GetAngle(Vector2 a, Vector2 b)
		{
			if ((a.x == 0 && a.y == 0) || (b.x == 0 && b.y == 0))
				return 0.0f;

			float x = a.x * b.x + a.y * b.y;
			float y = a.x * b.y - a.y * b.x;

			return Mathf.Atan2(y, x);
		}

		private static float GetAngleFast(Vector2 a, Vector2 b)
		{
			if ((a.x == 0 && a.y == 0) || (b.x == 0 && b.y == 0))
				return 0.0f;

			float x = a.x * b.x + a.y * b.y;
			float y = a.x * b.y - a.y * b.x;

			return FastAtan2(y, x);
		}

		private static float FastAtan(float x)
		{
			return (0.97239411f - 0.19194795f * x * x) * x;
		}

		private static float FastAtan2(float y, float x)
		{
			if (x != 0.0f)
			{
				float absX = x >= 0.0f ? x : -x;
				float absY = y >= 0.0f ? y : -y;

				if (absX > absY)
				{
					if (x > 0.0f)
						return FastAtan(y / x);
					if (y >= 0.0f)
						return FastAtan(y / x) + Mathf.PI;

					return FastAtan(y / x) - Mathf.PI;
				}
				else
				{
					if (y > 0.0f)
						return -FastAtan(x / y) + Mathf.PI * 0.5f;

					return -FastAtan(x / y) - Mathf.PI * 0.5f;
				}
			}
			else
			{
				if (y > 0.0f)
					return Mathf.PI * 0.5f;
				if (y < 0.0f)
					return -Mathf.PI * 0.5f;
			}

			return 0.0f;
		}
	}
}
