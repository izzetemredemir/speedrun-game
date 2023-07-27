using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace TPSBR
{
	public class PlayerService : IGlobalService
	{
		// PUBLIC MEMBERS

		public Action<PlayerData> PlayerDataChanged;

		public PlayerData PlayerData { get; private set; }

		// IGlobalService INTERFACE

		async void IGlobalService.Initialize()
		{
			PlayerData = LoadPlayer();

			try
			{
				PlayerData.UnityID = await GetUnityID();
			}
			catch (Exception exception)
			{
				PlayerData.UnityID = default;
				Debug.LogException(exception);
				Debug.LogWarning("Exception raised when initializing Unity Services. Please check if a Unity Project ID is linked in project settings.");
			}

			PlayerData.Lock();
			SavePlayer();
		}

		void IGlobalService.Tick()
		{
			if (PlayerData.IsDirty == true)
			{
				SavePlayer();
				PlayerData.ClearDirty();

				PlayerDataChanged?.Invoke(PlayerData);
			}
		}

		void IGlobalService.Deinitialize()
		{
			PlayerData.Unlock();
			SavePlayer();

			PlayerDataChanged = null;
		}

		// PRIVATE METHODS

		private PlayerData LoadPlayer()
		{
			var baseUserID = GetUserID();
			var userID = baseUserID;

			var playerData = PersistentStorage.GetObject<PlayerData>($"PlayerData-{userID}");

			if (Application.isMobilePlatform == false || Application.isEditor == true)
			{
				int clientIndex = 1;
				while (playerData != null && playerData.IsLocked() == true)
				{
					// We are probably running multiple clients, let's create unique player data for each one

					userID = $"{baseUserID}.{clientIndex}";
					playerData = PersistentStorage.GetObject<PlayerData>($"PlayerData-{userID}");

					clientIndex++;
				}
			}

			if (playerData == null)
			{
				playerData = new PlayerData(userID);
				playerData.AgentID = Global.Settings.Agent.GetRandomAgentSetup().ID;
			};

			return playerData;
		}

		private void SavePlayer()
		{
			PersistentStorage.SetObject($"PlayerData-{PlayerData.UserID}", PlayerData, true);
		}

		private string GetUserID()
		{
			var userID = SystemInfo.deviceUniqueIdentifier;

			if (ApplicationSettings.UseRandomDeviceID == true)
			{
				userID = Guid.NewGuid().ToString();
			}
			if (ApplicationSettings.HasCustomDeviceID == true)
			{
				userID = ApplicationSettings.CustomDeviceID;
			}

#if UNITY_EDITOR
			userID = $"{userID}_{Application.dataPath.GetHashCode()}";
#endif

			return userID;
		}

		private async Task<string> GetUnityID()
		{
#if UNITY_EDITOR
			if (UnityEditor.CloudProjectSettings.projectId.HasValue() == false)
				return default;
#endif

			if (UnityServices.State == ServicesInitializationState.Uninitialized)
			{
				await UnityServices.InitializeAsync();
			}

			if (AuthenticationService.Instance.IsAuthorized == false)
			{
				AuthenticationService.Instance.ClearSessionToken();

				await AuthenticationService.Instance.SignInAnonymouslyAsync();
			}

			return AuthenticationService.Instance.PlayerId;
		}
	}
}
