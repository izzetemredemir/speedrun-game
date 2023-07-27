using UnityEngine;
using Fusion;
using TMPro;

namespace TPSBR.UI
{
	public class UIGameplayView : UIView
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private UIPlayer _player;
		[SerializeField]
		private UICrosshair _crosshair;
		[SerializeField]
		private UIHealth _health;
		[SerializeField]
		private UIWeapons _weapons;
		[SerializeField]
		private UIGameplayInteractions _interactions;
		[SerializeField]
		private UIAgentEffects _effects;
		[SerializeField]
		private UIGameplayEvents _events;
		[SerializeField]
		private UIShrinkingArea _shrinkingArea;
		[SerializeField]
		private UIKillFeed _killFeed;
		[SerializeField]
		private UIBehaviour _spectatingGroup;
		[SerializeField]
		private TextMeshProUGUI _spectatingText;
		[SerializeField]
		private UIHitDamageIndicator _hitDamage;
		[SerializeField]
		private UIJetpack _jetpack;
		[SerializeField]
		private UIButton _menuButton;

		[Header("Gameplay Modes")]
		[SerializeField]
		private UIBattleRoyale _battleRoyale;

		[Header("Events Setup")]
		[SerializeField]
		private Color _enemyKilledColor = Color.red;
		[SerializeField]
		private Color _playerDeathColor = Color.yellow;
		[SerializeField]
		private AudioSetup _enemyKilledSound;
		[SerializeField]
		private AudioSetup _playerDeathSound;
		[SerializeField]
		private Color _interactionFailedColor = Color.white;
		[SerializeField]
		private AudioSetup _interactionFailedSound;

		private Agent              _localAgent;
		private NetworkBehaviourId _localAgentId;

		// UIView INTERFACE

		protected override void OnInitialize()
		{
			base.OnInitialize();

			ClearLocalAgent();

			Context.GameplayMode.OnAgentDeath       += OnAgentDeath;
			Context.GameplayMode.OnPlayerEliminated += OnPlayerEliminated;
			Context.GameplayMode.OnPlayerJoinedGame += OnPlayerJoined;
			Context.GameplayMode.OnPlayerLeftGame   += OnPlayerLeft;

			if (Context.Announcer != null)
			{
				Context.Announcer.Announce += OnAnnounce;
			}

			_battleRoyale.SetActive(Context.GameplayMode is BattleRoyaleGameplayMode);

			if ((Application.isMobilePlatform == false || Application.isEditor == true) && Context.Settings.SimulateMobileInput == false)
			{
				_menuButton.SetActive(false);
			}

			if (_menuButton != null)
			{
				_menuButton.onClick.AddListener(OnMenuButton);
			}
		}

		protected override void OnDeinitialize()
		{
			base.OnDeinitialize();

			Context.GameplayMode.OnAgentDeath       -= OnAgentDeath;
			Context.GameplayMode.OnPlayerEliminated -= OnPlayerEliminated;
			Context.GameplayMode.OnPlayerJoinedGame -= OnPlayerJoined;
			Context.GameplayMode.OnPlayerLeftGame   -= OnPlayerLeft;

			if (Context.Announcer != null)
			{
				Context.Announcer.Announce -= OnAnnounce;
			}

			if (_menuButton != null)
			{
				_menuButton.onClick.RemoveListener(OnMenuButton);
			}
		}

		protected override void OnTick()
		{
			base.OnTick();

			if (Context.Runner == null || Context.Runner.IsRunning == false)
				return;
			if (Context.GameplayMode == null || Context.Runner.Exists(Context.GameplayMode.Object) == false)
				return;

			if (_localAgent != Context.ObservedAgent || (Context.ObservedAgent != null && _localAgentId != Context.ObservedAgent.Id))
			{
				if (Context.ObservedAgent == null)
				{
					ClearLocalAgent();
				}
				else
				{
					var player = Context.NetworkGame.GetPlayer(Context.ObservedPlayerRef);
					if (player == null)
					{
						ClearLocalAgent();
					}
					else
					{
						SetLocalAgent(SceneUI.Context.ObservedAgent, player, Context.LocalPlayerRef == Context.ObservedPlayerRef);
					}
				}
			}

			var shrinkingArea = Context.GameplayMode.ShrinkingArea;
			if (shrinkingArea != null && shrinkingArea.IsActive == true && shrinkingArea.IsPaused == false)
			{
				_shrinkingArea.SetActive(true);
				_shrinkingArea.UpdateArea(Context.Runner, shrinkingArea);
			}
			else
			{
				_shrinkingArea.SetActive(false);
			}

			if (_localAgent == null)
				return;

			_health.UpdateHealth(_localAgent.Health);
			_effects.UpdateEffects(_localAgent);
			_weapons.UpdateWeapons(_localAgent.Weapons, _localAgent.AgentInput);
			_crosshair.UpdateCrosshair(_localAgent);
			_interactions.UpdateInteractions(Context, _localAgent.Weapons);
			_jetpack.UpdateJetpack(_localAgent.Jetpack);
		}

		// PRIVATE MEMBERS

		private void OnHitPerformed(HitData hitData)
		{
			_crosshair.HitPerformed(hitData);
			_hitDamage.HitPerformed(hitData);
		}

		private void OnAgentDeath(KillData killData)
		{
			var victimPlayer = Context.NetworkGame.GetPlayer(killData.VictimRef);
			var killerPlayer = Context.NetworkGame.GetPlayer(killData.KillerRef);

			_killFeed.ShowFeed(new KillFeedData
			{
				Killer        = killerPlayer != null ? killerPlayer.Nickname : "",
				Victim        = victimPlayer != null ? victimPlayer.Nickname : "",
				IsHeadshot    = killData.Headshot,
				DamageType    = killData.HitType,
				VictimIsLocal = killData.VictimRef != PlayerRef.None && killData.VictimRef == Context.LocalPlayerRef,
				KillerIsLocal = killData.KillerRef != PlayerRef.None && killData.KillerRef == Context.LocalPlayerRef,
			});

			if (killData.VictimRef == Context.ObservedPlayerRef)
			{
				bool eliminated = victimPlayer != null ? victimPlayer.Statistics.IsEliminated : false;

				_events.ShowEvent(new GameplayEventData
				{
					Name        = eliminated == true ? "YOU WERE ELIMINATED" : "YOU WERE KILLED",
					Description = killerPlayer != null ? $"Eliminated by {killerPlayer.Nickname}" : "",
					Color       = _playerDeathColor,
					Sound       = _playerDeathSound,
				});
			}
			else if (killData.KillerRef == Context.ObservedPlayerRef)
			{
				bool eliminated = killerPlayer != null ? killerPlayer.Statistics.IsEliminated : false;

				_events.ShowEvent(new GameplayEventData
				{
					Name        = eliminated == true ? "ENEMY ELIMINATED" : "ENEMY KILLED",
					Description = victimPlayer != null ? victimPlayer.Nickname : "",
					Color       = _enemyKilledColor,
					Sound       = _enemyKilledSound,
				});
			}
		}

		private void OnPlayerEliminated(PlayerRef playerRef)
		{
			var player = Context.NetworkGame.GetPlayer(playerRef);
			if (player == null)
				return;

			_killFeed.ShowFeed(new EliminationFeedData
			{
				Nickname = player.Nickname,
			});
		}

		private void OnPlayerJoined(PlayerRef playerRef)
		{
			var player = Context.NetworkGame.GetPlayer(playerRef);
			if (player == null)
				return;

			_killFeed.ShowFeed(new JoinedLeftFeedData
			{
				Joined   = true,
				Nickname = player.Nickname,
			});
		}

		private void OnPlayerLeft(string nickname)
		{
			_killFeed.ShowFeed(new JoinedLeftFeedData
			{
				Joined   = false,
				Nickname = nickname,
			});
		}

		private void OnAnnounce(AnnouncementData announcement)
		{
			if (announcement.FeedMessage.HasValue() == false)
				return;

			_killFeed.ShowFeed(new AnnouncementFeedData
			{
				Announcement = announcement.FeedMessage,
				Color = announcement.Color,
			});
		}

		private void OnMenuButton()
		{
			Context.Input.TrigggerBackAction();
		}

		private void SetLocalAgent(Agent agent, Player player, bool isLocalPlayer)
		{
			if (_localAgent != null)
			{
				_localAgent.Health.HitPerformed -= OnHitPerformed;
				_localAgent.Health.HitTaken -= OnHitTaken;

				_localAgent.Weapons.InteractionFailed -= OnInteractionFailed;
			}

			_localAgent   = agent;
			_localAgentId = agent.Id;

			_health.SetActive(true);
			_crosshair.SetActive(true);
			_interactions.SetActive(true);
			_effects.SetActive(true);
			_spectatingGroup.SetActive(isLocalPlayer == false);
			_jetpack.SetActive(true);
			_weapons.SetActive(true);

			_player.SetData(Context, player);

			if (isLocalPlayer == false)
			{
				_spectatingText.text = player.Nickname;
			}

			agent.Health.HitPerformed += OnHitPerformed;
			agent.Health.HitTaken += OnHitTaken;
			agent.Weapons.InteractionFailed += OnInteractionFailed;
		}

		private void ClearLocalAgent()
		{
			_health.SetActive(false);
			_weapons.SetActive(false);
			_crosshair.SetActive(false);
			_interactions.SetActive(false);
			_effects.SetActive(false);
			_spectatingGroup.SetActive(false);
			_jetpack.SetActive(false);

			if (_localAgent != null)
			{
				_localAgent.Health.HitPerformed -= OnHitPerformed;
				_localAgent.Health.HitTaken -= OnHitTaken;
				_localAgent.Weapons.InteractionFailed -= OnInteractionFailed;

				_localAgent   = null;
				_localAgentId = default;
			}
		}

		private void OnHitTaken(HitData hitData)
		{
			_effects.OnHitTaken(hitData);
		}

		private void OnInteractionFailed(string reason)
		{
			_events.ShowEvent(new GameplayEventData
			{
				Name        = string.Empty,
				Description = reason,
				Color       = _interactionFailedColor,
				Sound       = _interactionFailedSound,
			}, false, true);
		}

	}
}
