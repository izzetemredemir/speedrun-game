using DG.Tweening;
using TMPro;
using UnityEngine;

namespace TPSBR.UI
{
	public class UIAgentEffects : UIWidget
	{
		// PRIVATE METHODS

		[SerializeField]
		private CanvasGroup _hitGroup;
		[SerializeField]
		private CanvasGroup _criticalHitGroup;
		[SerializeField]
		private UIBehaviour _deathGroup;
		[SerializeField]
		private float _criticalHitDamageThreshold = 20f;
		[SerializeField]
		private UIBehaviour _lowHealthGroup;
		[SerializeField]
		private float _lowHealthTreshold = 20f;
		[SerializeField]
		private TextMeshProUGUI _heal;
		[SerializeField]
		private TextMeshProUGUI _shield;
		[SerializeField]
		private CanvasGroup _eyesFlashOverlay;

		[Header("Animation")]
		[SerializeField]
		private float _hitFadeInDuratio = 0.1f;
		[SerializeField]
		private float _hitFadeOutDuration = 0.7f;

		[Header("Audio")]
		[SerializeField]
		private AudioSetup _hitSound;
		[SerializeField]
		private AudioSetup _criticalHitSound;
		[SerializeField]
		private AudioSetup _deathSound;

		private UIHitDirection _hitDirection;

		// PUBLIC METHODS

		public void OnHitTaken(HitData hit)
		{
			if (hit.Amount <= 0)
				return;

			if (hit.Action == EHitAction.Damage)
			{
				if (hit.IsCritical == true || hit.Amount > _criticalHitDamageThreshold)
				{
					ShowHit(_criticalHitGroup, 1f);
					PlaySound(_criticalHitSound, EForceBehaviour.ForceAny);
				}
				else
				{
					float alpha = Mathf.Lerp(0, 1f, hit.Amount / _criticalHitDamageThreshold);
					ShowHit(_hitGroup, alpha);
					PlaySound(_hitSound, EForceBehaviour.ForceAny);
				}

				_hitDirection.ShowHit(hit);

				if (hit.IsFatal == true)
				{
					_deathGroup.SetActive(true);
					PlaySound(_deathSound, EForceBehaviour.ForceAny);
				}
			}
			else if (hit.Action == EHitAction.Heal)
			{
				_heal.SetActive(false);
				_heal.SetActive(true);

				_heal.text = $"+{(int)hit.Amount} HP";
			}
			else if (hit.Action == EHitAction.Shield)
			{
				_shield.SetActive(false);
				_shield.SetActive(true);

				_shield.text = $"+{(int)hit.Amount} Shield";
			}
		}

		public void UpdateEffects(Agent agent)
		{
			_deathGroup.SetActive(agent.Health.IsAlive == false);
			_hitDirection.UpdateDirection(agent);

			_lowHealthGroup.SetActive(agent.Health.IsAlive == true && agent.Health.CurrentHealth < _lowHealthTreshold);
			_eyesFlashOverlay.alpha = agent.Senses.EyesFlashValue;
		}

		// MONOBEHAVIOUR

		protected override void OnInitialize()
		{
			base.OnInitialize();

			_hitDirection = GetComponentInChildren<UIHitDirection>(true);
		}

		protected override void OnVisible()
		{
			base.OnVisible();

			_hitGroup.alpha = 0f;
			_criticalHitGroup.alpha = 0f;
			_eyesFlashOverlay.alpha = 0f;

			_deathGroup.SetActive(false);
			_heal.SetActive(false);
			_shield.SetActive(false);
		}

		// PRIVATE METHODS

		private void ShowHit(CanvasGroup group, float targetAlpha)
		{
			DOTween.Kill(group);

			group.DOFade(targetAlpha, _hitFadeInDuratio);
			group.DOFade(0f, _hitFadeOutDuration).SetDelay(_hitFadeInDuratio);
		}
	}
}
