namespace TPSBR
{
	using System.Collections.Generic;
	using UnityEngine;

	public partial class GizmoUtility : MonoBehaviour
	{
		// PRIVATE MEMBERS

		private static GameObject       _gizmoUtility;
		private static List<TimedGizmo> _gizmos = new List<TimedGizmo>();

		// PUBLIC METHODS

		public static void DrawLine(Vector3 startPosition, Vector3 endPosition, Color color, float duration = 0.0f)
		{
			if (Application.isPlaying == false || duration <= 0.0f)
			{
				Debug.DrawLine(startPosition, endPosition, color);
				return;
			}

			_gizmos.Add(new TimedGizmo(startPosition, endPosition, color, duration));

			if (_gizmoUtility == null)
			{
				_gizmoUtility = new GameObject("GizmoUtility");
				_gizmoUtility.hideFlags = HideFlags.HideAndDontSave;
				_gizmoUtility.AddComponent<GizmoUtility>();
				GameObject.DontDestroyOnLoad(_gizmoUtility);
			}
		}

		// PRIVATE METHODS

		private void LateUpdate()
		{
			float deltaTime = Time.deltaTime;

			for (int i = _gizmos.Count - 1; i >= 0; --i)
			{
				TimedGizmo gizmo = _gizmos[i];
				Debug.DrawLine(gizmo.StartPosition, gizmo.EndPosition, gizmo.Color);
				gizmo.Duration -= deltaTime;
				if (gizmo.Duration <= 0.0f)
				{
					_gizmos.RemoveAt(i);
				}
			}
		}

		// DATA STRUCTURES

		private sealed class TimedGizmo
		{
			public Vector3 StartPosition;
			public Vector3 EndPosition;
			public Color   Color;
			public float   Duration;

			public TimedGizmo(Vector3 startPosition, Vector3 endPosition, Color color, float duration)
			{
				StartPosition = startPosition;
				EndPosition   = endPosition;
				Color         = color;
				Duration      = duration;
			}
		}
	}
}
