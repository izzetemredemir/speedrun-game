using UnityEngine;

namespace TPSBR.UI
{
	public class UIJetpack : UIBehaviour
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private UIValue _fuelSmall;
		[SerializeField]
		private UIValue _fuelLarge;
		[SerializeField]
		private float _lowFuelTreshold = 50f;
		[SerializeField]
		private Color _lowFuelColor = Color.red;
		[SerializeField]
		private GameObject _lowFuelGroup;
		[SerializeField]
		private GameObject _noFuelGroup;

		private bool _isExpanded;
		private Animation _expandAnimation;

		private Color _defaultColor;

		// PUBLIC METHODS

		public void UpdateJetpack(Jetpack jetpack)
		{
			if (jetpack.Fuel <= 0 && _isExpanded == false && _expandAnimation.isPlaying == false)
			{
				CanvasGroup.SetVisibility(false);
				return;
			}

			CanvasGroup.SetVisibility(true);

			_fuelSmall.SetValue(jetpack.Fuel, jetpack.MaxFuel);
			_fuelLarge.SetValue(jetpack.Fuel, jetpack.MaxFuel);

			if (jetpack.IsActive != _isExpanded)
			{
				if (_isExpanded == false)
				{
					_expandAnimation.PlayForward();
				}
				else
				{
					_expandAnimation.PlayBackward();
				}

				_isExpanded = jetpack.IsActive;
			}

			bool lowFuel = jetpack.Fuel > 0 && jetpack.Fuel < _lowFuelTreshold;

			_fuelLarge.SetFillColor(lowFuel == true ? _lowFuelColor : _defaultColor);

			_lowFuelGroup.SetActive(lowFuel);
			_noFuelGroup.SetActive(jetpack.Fuel <= 0);
		}

		// MONOBEHAVIOUR

		protected void Awake()
		{
			_expandAnimation = GetComponent<Animation>();
			_defaultColor = _fuelLarge.Fill.color;
		}
	}
}
