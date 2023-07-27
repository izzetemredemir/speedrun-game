namespace TPSBR
{
	using System.Runtime.CompilerServices;
	using UnityEngine;

	public static partial class MathUtility
	{
		// PUBLIC METHODS

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float ClampBetween(float value, float a, float b)
		{
			return a < b ? Mathf.Clamp(value, a, b) : Mathf.Clamp(value, b, a);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float ClampAngleTo180(float angle)
		{
			while (angle > 180.0f)
			{
				angle -= 360.0f;
			}
			while (angle < -180.0f)
			{
				angle += 360.0f;
			}

			return angle;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 RandomInsideUnitCircle()
		{
			float radius = Mathf.Sqrt(Random.value);
			float angle  = Random.value * 2.0f * Mathf.PI;

			return new Vector2(radius * Mathf.Cos(angle), radius * Mathf.Sin(angle));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 RandomInsideUnitCircleNonUniform()
		{
			float radius = Random.value;
			float angle  = Random.value * 2.0f * Mathf.PI;

			return new Vector2(radius * Mathf.Cos(angle), radius * Mathf.Sin(angle));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 RandomOnUnitCircle()
		{
			float angle = Random.value * 2.0f * Mathf.PI;
			return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Map(float inMin, float inMax, float outMin, float outMax, float value)
		{
			if (value <= inMin)
				return outMin;

			if (value >= inMax)
				return outMax;

			return (outMax - outMin) * ((value - inMin) / (inMax - inMin)) + outMin;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Map(Vector2 inRange, Vector2 outRange, float value)
		{
			return Map(inRange.x, inRange.y, outRange.x, outRange.y, value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool HasSameSign(float a, float b)
		{
			return a == b || (a > 0.0f && b > 0.0f) || (a < 0.0f && b < 0.0f);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float ExpApproximate(float x)
		{
			x = 1.0f + x * 0.00390625f;
			x *= x; x *= x; x *= x; x *= x;
			x *= x; x *= x; x *= x; x *= x;

			return x;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector3 ComponentMin(Vector3 a, Vector3 b)
		{
			if (b.x < a.x) { a.x = b.x; }
			if (b.y < a.y) { a.y = b.y; }
			if (b.z < a.z) { a.z = b.z; }

			return a;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector3 ComponentMax(Vector3 a, Vector3 b)
		{
			if (b.x > a.x) { a.x = b.x; }
			if (b.y > a.y) { a.y = b.y; }
			if (b.z > a.z) { a.z = b.z; }

			return a;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float EasyIn2(float t)
		{
			return t * t;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float EasyIn3(float t)
		{
			return t * t * t;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float EasyIn4(float t)
		{
			return t * t * t * t;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float EasyOut2(float t)
		{
			t = 1.0f - t;
			return 1.0f - t * t;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float EasyOut3(float t)
		{
			t = 1.0f - t;
			return 1.0f - t * t * t;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float EasyOut4(float t)
		{
			t = 1.0f - t;
			return 1.0f - t * t * t * t;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float EasyInOut2(float t)
		{
			t = t * 2.0f;
			if (t <= 1.0f)
			{
				t = 0.5f * t * t;
			}
			else
			{
				t = t - 1.0f;
				t = -0.5f * (t * (t - 2.0f) - 1.0f);
			}
			return t;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float EasyInOut3(float t)
		{
			t = t * 2.0f;
			if (t <= 1.0f)
			{
				t = 0.5f * t * t * t;
			}
			else
			{
				t = t - 2.0f;
				t = 0.5f * (t * t * t + 2.0f);
			}
			return t;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float EasyInOut4(float t)
		{
			t = t * 2.0f;
			if (t <= 1.0f)
			{
				t = 0.5f * t * t * t * t;
			}
			else
			{
				t = t - 2.0f;
				t = -0.5f * (t * t * t * t - 2.0f);
			}
			return t;
		}
	}
}
