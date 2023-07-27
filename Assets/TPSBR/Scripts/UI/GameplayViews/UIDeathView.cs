namespace TPSBR.UI
{
	using UnityEngine;
	using UnityEngine.InputSystem;
	using TMPro;

	public class UIDeathView : UIView
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private Transform       _respawnGroup;
		[SerializeField]
		private TextMeshProUGUI _respawnTime;

		[SerializeField]
		private Transform       _spectatorGroup;

		// UIView INTERAFCE

		protected override void OnOpen()
		{
			base.OnOpen();

			Refresh();
		}

		protected override void OnTick()
		{
			base.OnTick();

			Refresh();
		}

		private void Refresh()
		{
			if (Context.Runner == null || Context.Runner.Exists(Context.GameplayMode.Object) == false)
				return;

			var player = Context.NetworkGame.GetPlayer(Context.LocalPlayerRef);
			var statistics = player != null ? player.Statistics : default;

			if (statistics.IsEliminated == false)
			{
				_respawnGroup.SetActive(true);
				_respawnTime.text = $"{statistics.RespawnTimer.RemainingTime(Context.Runner):F1} s";

				_spectatorGroup.SetActive(false);
			}
			else
			{
				_respawnGroup.SetActive(false);
				_spectatorGroup.SetActive(true);

				if (Keyboard.current.xKey.wasPressedThisFrame == true)
				{
					Context.GameplayMode.ChangeSpectatorTarget(true);
				}
				else if (Keyboard.current.zKey.wasPressedThisFrame == true)
				{
					Context.GameplayMode.ChangeSpectatorTarget(false);
				}
			}
		}
	}
}
