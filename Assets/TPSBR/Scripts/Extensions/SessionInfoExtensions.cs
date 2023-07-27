using Fusion;

namespace TPSBR
{
	public static class SessionInfoExtensions
	{
		public static string GetDisplayName(this SessionInfo info)
		{
			if (info.Properties.TryGetValue(Networking.DISPLAY_NAME_KEY, out SessionProperty name) == true)
				return name;

			return info.Name;
		}

		public static bool HasMap(this SessionInfo info)
		{
			return info.Properties.ContainsKey(Networking.MAP_KEY);
		}

		public static MapSetup GetMapSetup(this SessionInfo info)
		{
			if (info.Properties.TryGetValue(Networking.MAP_KEY, out SessionProperty mapIndex) == false)
				return null;

			return mapIndex >= 0 ? Global.Settings.Map.Maps[mapIndex] : null;
		}

		public static EGameplayType GetGameplayType(this SessionInfo info)
		{
			if (info.Properties.TryGetValue(Networking.TYPE_KEY, out SessionProperty type) == false)
				return EGameplayType.None;

			return (EGameplayType)(int)type;
		}

		public static GameMode GetGameMode(this SessionInfo info)
		{
			if (info.Properties.TryGetValue(Networking.MODE_KEY, out SessionProperty mode) == false)
				return default;

			return (GameMode)(int)mode;
		}
	}
}
