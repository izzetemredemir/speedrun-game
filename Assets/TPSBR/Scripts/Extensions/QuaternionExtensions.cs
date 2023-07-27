namespace TPSBR
{
	using System.Runtime.CompilerServices;
	using UnityEngine;

	public static class QuaternionExtensions
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool ComponentEquals(this Quaternion r1, Quaternion r2)
		{
			return r1.x == r2.x && r1.y == r2.y && r1.z == r2.z && r1.w == r2.w;
		}
	}
}
