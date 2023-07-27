using DG.Tweening;
using UnityEngine;
using TMPro;

namespace TPSBR.UI
{
	public class UICrosshair : UIWidget
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private RectTransform _resizingGroup;
		[SerializeField]
		private CanvasGroup _crosshairRootGroup;
		[SerializeField]
		private CanvasGroup _unarmedGroup;
		[SerializeField]
		private CanvasGroup _armedGroup;
		[SerializeField]
		private GameObject _sniperScope;
		[SerializeField]
		private UIBehaviour _undesiredFirePosition;
		[SerializeField]
		private float _undesiredFireCrosshairAlpha = 0.6f;

		[Header("Audio")]
		[SerializeField]
		private AudioSetup _hitPerformed;
		[SerializeField]
		private AudioSetup _criticalHitPerformed;

		[Header("Hit Feedback")]
		[SerializeField]
		private CanvasGroup _hitGroup;
		[SerializeField]
		private CanvasGroup _criticalHitGroup;
		[SerializeField]
		private CanvasGroup _fatalHitGroup;
		[SerializeField]
		private float _hitGroupDelay = 0.15f;
		[SerializeField]
		private float _hitGroupFadeInDuration = 0.1f;
		[SerializeField]
		private float _hitGroupFadeOutDuration = 0.8f;

		[Header("Dispersion")]
		[SerializeField]
		private float _sizeDispersion0 = 24f;
		[SerializeField]
		private float _sizeDispersion50 = 1000f;
		[SerializeField]
		private float _changeSpeed = 20f;

		private float _defaultSize;
		private int _lastSoundFrame;

		private Vector2 _targetSize;

		// PUBLIC METHODS

		public void HitPerformed(HitData hitData)
		{
			PlayEffect(hitData.IsCritical == true ? _criticalHitPerformed : _hitPerformed);

			var hitGroup = hitData.IsFatal == true ? _fatalHitGroup : (hitData.IsCritical == true ? _criticalHitGroup : _hitGroup);
			DOTween.Kill(hitGroup);

			hitGroup.DOFade(1f, _hitGroupFadeInDuration).SetDelay(_hitGroupDelay);
			hitGroup.DOFade(0f, _hitGroupFadeOutDuration).SetDelay(_hitGroupDelay + _hitGroupFadeInDuration + 0.1f);
		}

		public void UpdateCrosshair(Agent agent)
		{
			var weapon = agent.Weapons.CurrentWeapon;
			float size = _defaultSize;

			bool weaponValid = weapon != null && weapon.Object.IsValid;

			if (weaponValid == true && weapon is FirearmWeapon firearmWeapon)
			{
				size = Mathf.Lerp(_sizeDispersion0, _sizeDispersion50, firearmWeapon.TotalDispersion / 50f);
			}

			_targetSize = new Vector2(size, size);
			_resizingGroup.sizeDelta = Vector2.Lerp(_resizingGroup.sizeDelta, _targetSize, Time.deltaTime * _changeSpeed);

			bool showScope = weaponValid == true && weapon.HitType == EHitType.Sniper && agent.Character.CharacterController.Data.Aim == true;

			_armedGroup.SetVisibility(showScope == false && weaponValid);
			_unarmedGroup.SetVisibility(weaponValid == false);

			_sniperScope.SetActive(showScope);

			bool showUndesiredFirePosition = weaponValid == true && agent.Weapons.UndesiredTargetPoint;
			_undesiredFirePosition.SetActive(showUndesiredFirePosition);

			if (showUndesiredFirePosition == true)
			{
				_undesiredFirePosition.transform.position = Context.Camera.Camera.WorldToScreenPoint(agent.Weapons.TargetPoint);
			}

			_crosshairRootGroup.alpha = Mathf.Lerp(_crosshairRootGroup.alpha, showUndesiredFirePosition == true ? _undesiredFireCrosshairAlpha : 1f, Time.deltaTime * 8f);
		}

		// MONOBEHAVIOUR

		private void Awake()
		{
			_defaultSize = _resizingGroup.sizeDelta.x;
		}

		// PRIVATE MEMBERS

		private void PlayEffect(AudioSetup setup)
		{
			if (Time.frameCount == _lastSoundFrame)
				return; // Play only one sound per frame

			SceneUI.PlaySound(setup);
			_lastSoundFrame = Time.frameCount;
		}
	}
}
