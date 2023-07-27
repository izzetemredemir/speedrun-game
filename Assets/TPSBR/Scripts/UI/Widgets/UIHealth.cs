using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TPSBR.UI
{
	public class UIHealth : UIBehaviour
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private TextMeshProUGUI _healthText;
		[SerializeField]
		private Image           _healthProgress;
		[SerializeField]
		private TextMeshProUGUI _maxHealthText;
		[SerializeField]
		private Image           _healthIcon;
		[SerializeField]
		private Image           _healthIcon2;
		[SerializeField]
		private float           _healthAnimationDuration = 0.2f;
		[SerializeField]
		private Color[]         _healthColors;
		[SerializeField]
		private Animation       _criticalAnimation;
		[SerializeField]
		private float           _criticalThreshold = 0.2f;

		[SerializeField]
		private Image           _shieldIcon;
		[SerializeField]
		private TextMeshProUGUI _shieldText;
		[SerializeField]
		private Color           _shieldInactiveColor = Color.gray;
		[SerializeField]
		private Image           _shieldProgress;
		[SerializeField]
		private TextMeshProUGUI _maxShieldText;

		private int _lastHealth = -1;
		private int _lastMaxHealth = -1;

		private int _lastShield = -1;
		private int _lastMaxShield = -1;

		private Health _health;
		private Color  _shieldColor;

		// PUBLIC METHODS

		public void UpdateHealth(Health health)
		{
			if (_health != health)
			{
				_health        = health;
				_lastHealth    = -1;
				_lastMaxHealth = -1;
				_lastShield    = -1;
				_lastMaxShield = -1;
			}

			// HEALTH

			int currentHealth = Mathf.RoundToInt(health.CurrentHealth);

			if (currentHealth == 0 && health.CurrentHealth > 0)
			{
				// Do not show zero if not necessary
				currentHealth = 1;
			}

			int maxHealth = (int)health.MaxHealth;

			if (currentHealth != _lastHealth)
			{
				DOTween.Kill(_healthProgress);

				float progress = currentHealth / health.MaxHealth;
				_healthProgress.DOFillAmount(progress, _healthAnimationDuration);

				_healthText.text = currentHealth.ToString();

				UpdateHealthColor(health);
				ShowCriticalAnimation(progress);

				_lastHealth = currentHealth;
			}

			if (maxHealth != _lastMaxHealth)
			{
				if (_maxHealthText != null)
				{
					_maxHealthText.text = $"{maxHealth}";
				}

				_lastMaxHealth = maxHealth;
			}

			// SHIELD

			int currentShield = (int)health.CurrentShield;
			int maxShield = (int)health.MaxShield;

			if (currentShield != _lastShield)
			{
				DOTween.Kill(_shieldProgress);

				float progress = currentShield / health.MaxShield;
				_shieldProgress.DOFillAmount(progress, _healthAnimationDuration);

				_shieldText.text = currentShield.ToString();
				_shieldText.color = currentShield > 0 ? _shieldColor : _shieldInactiveColor;
				_shieldIcon.color = currentShield > 0 ? _shieldColor : _shieldInactiveColor;

				UpdateHealthColor(health);

				_lastShield = currentShield;
			}

			if (maxShield != _lastMaxShield)
			{
				if (_maxShieldText != null)
				{
					_maxShieldText.text = $"{maxShield}";
				}

				_lastMaxShield = maxShield;
			}
		}

		// MONOBEHAVIOUR

		protected void Awake()
		{
			_shieldColor = _shieldIcon.color;
		}

		protected void OnEnable()
		{
			_criticalAnimation.SampleStart();
		}

		// PRIVATE MEMBERS

		private void UpdateHealthColor(Health health)
		{
			var healthColor = GetHealthColor(health.CurrentHealth / health.MaxHealth);
			_healthText.color = healthColor;
			_healthIcon.color = healthColor;
			_healthIcon2.color = healthColor;
			_healthProgress.color = healthColor;
		}

		private Color GetHealthColor(float healthProgress)
		{
			float preciseIndex = MathUtility.Map(0f, 1f, 0f, _healthColors.Length - 1, healthProgress);

			int fromIndex = (int)preciseIndex;
			int toIndex = Mathf.Clamp(fromIndex + 1, 0, _healthColors.Length - 1);

			return Color.Lerp(_healthColors[fromIndex], _healthColors[toIndex], preciseIndex - fromIndex);
		}

		private void ShowCriticalAnimation(float healthProgress)
		{
			if (healthProgress > _criticalThreshold)
			{
				if (_criticalAnimation.isPlaying == true)
				{
					_criticalAnimation.SampleStart();
				}

				return;
			}

			if (_criticalAnimation.isPlaying == false)
			{
				_criticalAnimation.Play();
			}
		}
	}
}
