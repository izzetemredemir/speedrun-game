using UnityEngine;

namespace TPSBR.UI
{
	using Fusion;

    public class UIShrinkingArea : UIBehaviour
    {
		// PRIVATE MEMBERS

		[SerializeField]
		private GameObject _remainingProgressGroup;
		[SerializeField]
		private GameObject _remainingTimeGroup;
		[SerializeField]
		private GameObject _areaShrinkingGroup;

		[SerializeField]
		private UIValue _remainingProgressValue;
		[SerializeField]
		private UIValue _remainingProgressTimeValue;
		[SerializeField]
		private UIValue _remainingTimeValue;

		[SerializeField]
		private AudioEffect _shrinkingSound;

		// PUBLIC METHODS

		public void UpdateArea(NetworkRunner runner, ShrinkingArea area)
		{
			var remainingTimeValue = area.NextShrinking.RemainingTime(runner);
			float remainingTime = remainingTimeValue.HasValue == true ? remainingTimeValue.Value : 0f;

			if (area.IsShrinking == true)
			{
				_areaShrinkingGroup.SetActive(true);
				_remainingProgressGroup.SetActive(false);
				_remainingTimeGroup.SetActive(false);

				_shrinkingSound.Play(EForceBehaviour.ForceDifferentSetup);
			}
			else if (area.IsAnnounced == true)
			{
				_shrinkingSound.Stop();

				_areaShrinkingGroup.SetActive(false);
				_remainingProgressGroup.SetActive(true);
				_remainingTimeGroup.SetActive(true);

				_remainingProgressValue.SetValue(area.ShrinkDelay - remainingTime, area.ShrinkDelay);
				_remainingProgressTimeValue.SetValue(remainingTime);
				_remainingTimeValue.SetValue(remainingTime);
			}
			else
			{
				_shrinkingSound.Stop();

				_areaShrinkingGroup.SetActive(false);
				_remainingProgressGroup.SetActive(remainingTime > 0f);
				_remainingTimeGroup.SetActive(false);

				_remainingProgressValue.SetValue(area.ShrinkDelay - remainingTime, area.ShrinkDelay);
				_remainingProgressTimeValue.SetValue(remainingTime);
			}
		}
	}
}
