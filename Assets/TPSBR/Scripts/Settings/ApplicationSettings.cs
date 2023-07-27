using UnityEngine;
using Fusion.Photon.Realtime;

namespace TPSBR
{
	public static class ApplicationSettings
	{
		// PUBLIC MEMBERS

		public static readonly bool   IsHost;
		public static readonly bool   IsServer;
		public static readonly bool   IsClient;
		public static readonly bool   IsDeathmatch;
		public static readonly bool   IsElimination;
		public static readonly bool   IsBattleRoyale;
		public static readonly bool   IsQuickPlay;
		public static readonly bool   HasRegion;
		public static readonly string Region;
		public static readonly bool   HasExtraPeers;
		public static readonly int    ExtraPeers;
		public static readonly bool   HasServerName;
		public static readonly string ServerName;
		public static readonly bool   HasMaxPlayers;
		public static readonly int    MaxPlayers;
		public static readonly bool   HasSessionName;
		public static readonly string SessionName;
		public static readonly bool   HasCustomLobby;
		public static readonly string CustomLobby;
		public static readonly bool   HasCustomScene;
		public static readonly string CustomScene;
		public static readonly bool   HasIPAddress;
		public static readonly string IPAddress;
		public static readonly bool   HasPort;
		public static readonly int    Port;
		public static readonly bool   UseMultiplay;
		public static readonly bool   UseMatchmaking;
		public static readonly bool   UseBackfill;
		public static readonly bool   UseSQP;
		public static readonly bool   HasQueueName;
		public static readonly string QueueName;
		public static readonly bool   IsModerator;
		public static readonly bool   IsPublicBuild;
		public static readonly bool   IsBatchServer;
		public static readonly bool   IsStrippedBatch;
		public static readonly bool   RecordSession;
		public static readonly bool   HasFrameRate;
		public static readonly int    FrameRate;
		public static readonly bool   UseRandomDeviceID;
		public static readonly bool   HasCustomDeviceID;
		public static readonly string CustomDeviceID;
		public static readonly bool   GenerateInput;

		// CONSTRUCTORS

		static ApplicationSettings()
		{
			IsHost            = ApplicationUtility.HasCommandLineArgument("-host");
			IsServer          = ApplicationUtility.HasCommandLineArgument("-dedicatedServer");
			IsClient          = ApplicationUtility.HasCommandLineArgument("-client");
			IsDeathmatch      = ApplicationUtility.HasCommandLineArgument("-deathmatch");
			IsElimination     = ApplicationUtility.HasCommandLineArgument("-elimination");
			IsBattleRoyale    = ApplicationUtility.HasCommandLineArgument("-battleRoyale");
			IsQuickPlay       = ApplicationUtility.HasCommandLineArgument("-quickPlay");
			HasRegion         = ApplicationUtility.GetCommandLineArgument("-region", out Region);
			HasExtraPeers     = ApplicationUtility.GetCommandLineArgument("-extraPeers", out ExtraPeers);
			HasServerName     = ApplicationUtility.GetCommandLineArgument("-serverName", out ServerName);
			HasMaxPlayers     = ApplicationUtility.GetCommandLineArgument("-maxPlayers", out MaxPlayers);
			HasSessionName    = ApplicationUtility.GetCommandLineArgument("-sessionName", out SessionName);
			HasCustomLobby    = ApplicationUtility.GetCommandLineArgument("-lobby", out CustomLobby);
			HasCustomScene    = ApplicationUtility.GetCommandLineArgument("-scene", out CustomScene);
			HasIPAddress      = ApplicationUtility.GetCommandLineArgument("-ip", out IPAddress);
			HasPort           = ApplicationUtility.GetCommandLineArgument("-port", out Port);
			UseMultiplay      = ApplicationUtility.HasCommandLineArgument("-multiplay");
			UseMatchmaking    = ApplicationUtility.HasCommandLineArgument("-matchmaking");
			UseBackfill       = ApplicationUtility.HasCommandLineArgument("-backfill");
			UseSQP            = ApplicationUtility.HasCommandLineArgument("-sqp");
			HasQueueName      = ApplicationUtility.GetCommandLineArgument("-queueName", out QueueName);
			IsModerator       = Application.isEditor == true || ApplicationUtility.HasCommandLineArgument("-moderator");
			IsPublicBuild     = PhotonAppSettings.Instance.AppSettings.AppVersion.ToLowerInvariant().Contains("-public");
			IsBatchServer     = Application.isBatchMode == true && IsServer == true;
			IsStrippedBatch   = Application.isBatchMode == true && ApplicationUtility.HasCommandLineArgument("-stripped") && IsClient == true;
			RecordSession     = ApplicationUtility.HasCommandLineArgument("-recordSession");
			HasFrameRate      = ApplicationUtility.GetCommandLineArgument("-fps", out FrameRate);
			UseRandomDeviceID = ApplicationUtility.HasCommandLineArgument("-randomDeviceID");
			HasCustomDeviceID = ApplicationUtility.GetCommandLineArgument("-deviceID", out CustomDeviceID);
			GenerateInput     = ApplicationUtility.HasCommandLineArgument("-generateInput");
		}
	}
}
