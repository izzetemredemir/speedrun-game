using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevel;

namespace TPSBR
{
	public static partial class PlayerLoopUtility
	{
		// PUBLIC METHODS

		public static void SetDefaultPlayerLoopSystem()
		{
			PlayerLoop.SetPlayerLoop(PlayerLoop.GetDefaultPlayerLoop());
		}

		public static bool HasPlayerLoopSystem(Type playerLoopSystemType)
		{
			if (playerLoopSystemType == null)
				return false;

			PlayerLoopSystem loopSystem = PlayerLoop.GetCurrentPlayerLoop();
			for (int i = 0, subSystemCount = loopSystem.subSystemList.Length; i < subSystemCount; ++i)
			{
				PlayerLoopSystem subSystem = loopSystem.subSystemList[i];

				List<PlayerLoopSystem> subSubSystems = new List<PlayerLoopSystem>(subSystem.subSystemList);
				for (int j = 0; j < subSubSystems.Count; ++j)
				{
					if (subSubSystems[j].type == playerLoopSystemType)
						return true;
				}
			}

			return false;
		}

		public static bool AddPlayerLoopSystem(Type playerLoopSystemType, Type targetLoopSystemType, PlayerLoopSystem.UpdateFunction updateFunction, int position = -1)
		{
			if (playerLoopSystemType == null || targetLoopSystemType == null || updateFunction == null)
				return false;

			PlayerLoopSystem loopSystem = PlayerLoop.GetCurrentPlayerLoop();
			for (int i = 0, subSystemCount = loopSystem.subSystemList.Length; i < subSystemCount; ++i)
			{
				PlayerLoopSystem subSystem = loopSystem.subSystemList[i];
				if (subSystem.type == targetLoopSystemType)
				{
					PlayerLoopSystem targetSystem = new PlayerLoopSystem();
					targetSystem.type = playerLoopSystemType;
					targetSystem.updateDelegate = updateFunction;

					List<PlayerLoopSystem> subSubSystems = new List<PlayerLoopSystem>(subSystem.subSystemList);
					if (position >= 0)
					{
						if (position > subSubSystems.Count)
							throw new ArgumentOutOfRangeException(nameof(position));

						subSubSystems.Insert(position, targetSystem);
						//Debug.LogWarningFormat("Added Player Loop System: {0} to: {1} position: {2}/{3}", playerLoopSystemType.FullName, subSystem.type.FullName, position, subSubSystems.Count - 1);
					}
					else
					{
						subSubSystems.Add(targetSystem);
						//Debug.LogWarningFormat("Added Player Loop System: {0} to: {1} position: {2}/{2}", playerLoopSystemType.FullName, subSystem.type.FullName, subSubSystems.Count - 1);
					}

					subSystem.subSystemList = subSubSystems.ToArray();
					loopSystem.subSystemList[i] = subSystem;

					PlayerLoop.SetPlayerLoop(loopSystem);

					return true;
				}
			}

			Debug.LogErrorFormat("Failed to add Player Loop System: {0} to: {1}", playerLoopSystemType.FullName, targetLoopSystemType.FullName);

			return false;
		}

		public static bool AddPlayerLoopSystem(Type playerLoopSystemType, Type targetSubSystemType, PlayerLoopSystem.UpdateFunction updateFunctionBefore, PlayerLoopSystem.UpdateFunction updateFunctionAfter)
		{
			if (playerLoopSystemType == null || targetSubSystemType == null || (updateFunctionBefore == null && updateFunctionAfter == null))
				return false;

			PlayerLoopSystem loopSystem = PlayerLoop.GetCurrentPlayerLoop();
			for (int i = 0, subSystemCount = loopSystem.subSystemList.Length; i < subSystemCount; ++i)
			{
				PlayerLoopSystem subSystem = loopSystem.subSystemList[i];
				for (int j = 0, subSubSystemCount = subSystem.subSystemList.Length; j < subSubSystemCount; ++j)
				{
					PlayerLoopSystem subSubSystem = subSystem.subSystemList[j];
					if (subSubSystem.type == targetSubSystemType)
					{
						List<PlayerLoopSystem> subSubSystems = new List<PlayerLoopSystem>(subSystem.subSystemList);
						int currentPosition = j;

						if (updateFunctionBefore != null)
						{
							PlayerLoopSystem playerLoopSystem = new PlayerLoopSystem();
							playerLoopSystem.type = playerLoopSystemType;
							playerLoopSystem.updateDelegate = updateFunctionBefore;

							subSubSystems.Insert(currentPosition, playerLoopSystem);

							//Debug.LogWarningFormat("Added Player Loop System: {0} to: {1} before: {2}", playerLoopSystemType.FullName, subSystem.type.FullName, subSubSystem.type.FullName);

							++currentPosition;
						}

						if (updateFunctionAfter != null)
						{
							++currentPosition;

							PlayerLoopSystem playerLoopSystem = new PlayerLoopSystem();
							playerLoopSystem.type = playerLoopSystemType;
							playerLoopSystem.updateDelegate = updateFunctionAfter;

							subSubSystems.Insert(currentPosition, playerLoopSystem);

							//Debug.LogWarningFormat("Added Player Loop System: {0} to: {1} after: {2}", playerLoopSystemType.FullName, subSystem.type.FullName, subSubSystem.type.FullName);
						}

						subSystem.subSystemList = subSubSystems.ToArray();
						loopSystem.subSystemList[i] = subSystem;

						PlayerLoop.SetPlayerLoop(loopSystem);

						return true;
					}
				}
			}

			Debug.LogErrorFormat("Failed to add Player Loop System: {0}", playerLoopSystemType.FullName);

			return false;
		}

		public static bool RemovePlayerLoopSystems(Type playerLoopSystemType)
		{
			if (playerLoopSystemType == null)
				return false;

			bool setPlayerLoop = false;

			PlayerLoopSystem loopSystem = PlayerLoop.GetCurrentPlayerLoop();
			for (int i = 0, subSystemCount = loopSystem.subSystemList.Length; i < subSystemCount; ++i)
			{
				PlayerLoopSystem subSystem = loopSystem.subSystemList[i];
				if (subSystem.subSystemList == null)
					continue;

				bool removedFromSubSystem = false;

				List<PlayerLoopSystem> subSubSystems = new List<PlayerLoopSystem>(subSystem.subSystemList);
				for (int j = subSubSystems.Count - 1; j >= 0; --j)
				{
					if (subSubSystems[j].type == playerLoopSystemType)
					{
						subSubSystems.RemoveAt(j);
						removedFromSubSystem = true;
						//Debug.LogWarningFormat("Removed Loop System: {0} from: {1}", playerLoopSystemType.FullName, subSystem.type.FullName);
					}
				}

				if (removedFromSubSystem == true)
				{
					setPlayerLoop = true;

					subSystem.subSystemList = subSubSystems.ToArray();
					loopSystem.subSystemList[i] = subSystem;
				}
			}

			if (setPlayerLoop == true)
			{
				PlayerLoop.SetPlayerLoop(loopSystem);
			}

			return setPlayerLoop;
		}

		public static void DumpPlayerLoopSystems()
		{
			Debug.LogWarning("====================================================================================================");

			PlayerLoopSystem loopSystem = PlayerLoop.GetCurrentPlayerLoop();
			for (int i = 0, subSystemCount = loopSystem.subSystemList.Length; i < subSystemCount; ++i)
			{
				PlayerLoopSystem subSystem = loopSystem.subSystemList[i];

				Debug.LogWarning(subSystem.type.FullName);

				List<PlayerLoopSystem> subSubSystems = new List<PlayerLoopSystem>(subSystem.subSystemList);
				for (int j = 0; j < subSubSystems.Count; ++j)
				{
					Debug.Log("    " + subSubSystems[j].type.FullName);
				}
			}

			Debug.LogWarning("====================================================================================================");
		}
	}
}
