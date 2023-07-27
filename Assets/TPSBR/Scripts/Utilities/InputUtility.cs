namespace TPSBR
{
	using System;
	using UnityEngine;

	public static partial class InputUtility
	{
		// CONSTANTS

		private const float INCH_TO_CM = 2.54f;

		// PUBLIC METHODS

		public static Vector2 ProcessLookRotationDelta(FrameRecord[] frameRecords, Vector2 lookRotationDelta, float lookRotationSensitivity, float lookResponsivity)
		{
			lookRotationDelta *= lookRotationSensitivity;

			// If the look rotation responsivity is enabled, calculate average delta instead.
			if (lookResponsivity > 0.0f)
			{
				// Kill any rotation in opposite direction for instant direction flip.
				CleanLookRotationDeltaHistory(frameRecords, lookRotationDelta);

				FrameRecord frameRecord = new FrameRecord(Time.unscaledDeltaTime, lookRotationDelta);

				// Shift history with frame records.
				Array.Copy(frameRecords, 0, frameRecords, 1, frameRecords.Length - 1);

				// Store current frame to history.
				frameRecords[0] = frameRecord;

				float   accumulatedDeltaTime         = default;
				Vector2 accumulatedLookRotationDelta = default;

				// Iterate over all frame records.
				for (int i = 0; i < frameRecords.Length; ++i)
				{
					frameRecord = frameRecords[i];

					// Accumualte delta time and look rotation delta until we pass responsivity threshold.
					accumulatedDeltaTime         += frameRecord.DeltaTime;
					accumulatedLookRotationDelta += frameRecord.DeltaTime * frameRecord.LookRotationDelta;

					if (accumulatedDeltaTime > lookResponsivity)
					{
						// To have exact responsivity time window length, we have to remove delta overshoot from last accumulation.

						float overshootDeltaTime = accumulatedDeltaTime - lookResponsivity;

						accumulatedDeltaTime         -= overshootDeltaTime;
						accumulatedLookRotationDelta -= overshootDeltaTime * frameRecord.LookRotationDelta;

						break;
					}
				}

				// Normalize acucmulated look rotation delta and calculate size for current frame.
				lookRotationDelta = accumulatedLookRotationDelta / accumulatedDeltaTime;
			}

			return lookRotationDelta;
		}

		public static float PixelsToCentimeters(float pixels)
		{
			return (pixels * INCH_TO_CM) / Screen.dpi;
		}

		public static Vector2 PixelsToCentimeters(Vector2 pixels)
		{
			return (pixels * INCH_TO_CM) / Screen.dpi;
		}

		// PRIVATE METHODS

		private static void CleanLookRotationDeltaHistory(FrameRecord[] frameRecords, Vector2 lookRotationDelta)
		{
			int count = frameRecords.Length;

			// Iterate over all records and clear rotation with opposite direction, giving instant responsivity when direction flips.
			// Each axis is processed separately.

			if (lookRotationDelta.x < 0.0f) { for (int i = 0; i < count; ++i) { if (frameRecords[i].LookRotationDelta.x > 0.0f) { frameRecords[i].LookRotationDelta.x = 0.0f; } } }
			if (lookRotationDelta.x > 0.0f) { for (int i = 0; i < count; ++i) { if (frameRecords[i].LookRotationDelta.x < 0.0f) { frameRecords[i].LookRotationDelta.x = 0.0f; } } }
			if (lookRotationDelta.y < 0.0f) { for (int i = 0; i < count; ++i) { if (frameRecords[i].LookRotationDelta.y > 0.0f) { frameRecords[i].LookRotationDelta.y = 0.0f; } } }
			if (lookRotationDelta.y > 0.0f) { for (int i = 0; i < count; ++i) { if (frameRecords[i].LookRotationDelta.y < 0.0f) { frameRecords[i].LookRotationDelta.y = 0.0f; } } }
		}
	}

	public struct FrameRecord
	{
		public float   DeltaTime;
		public Vector2 LookRotationDelta;

		public FrameRecord(float deltaTime, Vector2 lookRotationDelta)
		{
			DeltaTime         = deltaTime;
			LookRotationDelta = lookRotationDelta;
		}
	}
}
