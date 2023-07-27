using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Rendering;
using Fusion;
using Fusion.Photon.Realtime;

namespace TPSBR
{
	public interface IGlobalService
	{
		void Initialize();
		void Tick();
		void Deinitialize();
	}

	public static class Global
	{
		// PUBLIC MEMBERS

		public static GlobalSettings   Settings          { get; private set; }
		public static RuntimeSettings  RuntimeSettings   { get; private set; }
		public static PlayerService    PlayerService     { get; private set; }
		public static Networking       Networking        { get; private set; }
		public static MultiplayManager MultiplayManager  { get; private set; }

		// PRIVATE MEMBERS

		private static bool _isInitialized;
		private static List<IGlobalService> _globalServices = new List<IGlobalService>(16);

		// PUBLIC METHODS

		public static void Quit()
		{
			Deinitialize();

#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
#else
			Application.Quit();
#endif
		}

		// PRIVATE METHODS

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void InitializeSubSystem()
		{
			if (Application.isBatchMode == true)
			{
				UnityEngine.AudioListener.volume = 0.0f;
				PlayerLoopUtility.RemovePlayerLoopSystems(typeof(PostLateUpdate.UpdateAudio));
			}

#if UNITY_EDITOR
			if (Application.isPlaying == false)
				return;
#endif
			if (PlayerLoopUtility.HasPlayerLoopSystem(typeof(Global)) == false)
			{
				PlayerLoopUtility.AddPlayerLoopSystem(typeof(Global), typeof(Update.ScriptRunBehaviourUpdate), BeforeUpdate, AfterUpdate);
			}

			Application.quitting -= OnApplicationQuit;
			Application.quitting += OnApplicationQuit;

			_isInitialized = true;
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void InitializeBeforeSceneLoad()
		{
			Initialize();

			// You can pause network services here

			if (ApplicationSettings.IsBatchServer == true)
			{
				Application.targetFrameRate = NetworkProjectConfig.Global.Simulation.TickRate;
			}

			if (ApplicationSettings.HasFrameRate == true)
			{
				Application.targetFrameRate = ApplicationSettings.FrameRate;
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void InitializeAfterSceneLoad()
		{
			// You can unpause network services here
		}

		private static void Initialize()
		{
			if (_isInitialized == false)
				return;

			if (typeof(DebugManager).GetField("m_DebugActions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(DebugManager.instance) == null)
			{
				typeof(DebugManager).GetMethod("RegisterInputs", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(DebugManager.instance, null);
				typeof(DebugManager).GetMethod("RegisterActions", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(DebugManager.instance, null);
			}

			GlobalSettings[] globalSettings = Resources.LoadAll<GlobalSettings>("");
			Settings = globalSettings.Length > 0 ? Object.Instantiate(globalSettings[0]) : null;

			RuntimeSettings = new RuntimeSettings();
			RuntimeSettings.Initialize(Settings);

			PrepareGlobalServices();

			Networking = CreateStaticObject<Networking>();

			if (ApplicationSettings.UseMultiplay == true && ApplicationSettings.IsServer == true)
			{
				MultiplayManager = CreateStaticObject<MultiplayManager>();
			}

			_isInitialized = true;
		}

		private static void Deinitialize()
		{
			if (_isInitialized == false)
				return;

			for (int i = _globalServices.Count - 1; i >= 0; i--)
			{
				var service = _globalServices[i];
				if (service != null)
				{
					service.Deinitialize();
				}
			}

			_isInitialized = false;
		}

		private static void OnApplicationQuit()
		{
			Deinitialize();
		}

		private static void BeforeUpdate()
		{
			for (int i = 0; i < _globalServices.Count; i++)
			{
				_globalServices[i].Tick();
			}
		}

		private static void AfterUpdate()
		{
			if (Application.isPlaying == false)
			{
				PlayerLoopUtility.RemovePlayerLoopSystems(typeof(Global));
			}
		}

		private static void PrepareGlobalServices()
		{
			PlayerService = new PlayerService();

			_globalServices.Add(PlayerService);

			for (int i = 0; i < _globalServices.Count; i++)
			{
				_globalServices[i].Initialize();
			}
		}

		private static T CreateStaticObject<T>() where T : Component
		{
			GameObject gameObject = new GameObject(typeof(T).Name);
			Object.DontDestroyOnLoad(gameObject);

			return gameObject.AddComponent<T>();
		}
	}
}
