using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace TPSBR.UI
{
	public class UIButton : Button
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private bool       _playClickSound = true;
		[SerializeField]
		private AudioSetup _customClickSound;

		private UIWidget   _parent;

		private static List<UIWidget> _tempWidgetList = new List<UIWidget>(16);

		// PUBLIC METHODS

		public void PlayClickSound()
		{
			if (_playClickSound == false)
				return;

			if (_parent == null)
			{
				_tempWidgetList.Clear();

				GetComponentsInParent(true, _tempWidgetList);

				_parent = _tempWidgetList.Count > 0 ? _tempWidgetList[0] : null;
				_tempWidgetList.Clear();
			}

			if (_customClickSound.Clips.Length > 0)
			{
				_parent.PlaySound(_customClickSound);
			}
			else
			{
				_parent.PlayClickSound();
			}
		}

		// MONOBEHAVIOR

		protected override void Awake()
		{
			base.Awake();

			onClick.AddListener(OnClick);

			if (transition == Transition.Animation)
			{
				var buttonAnimator = animator;
				if (buttonAnimator != null)
				{
					buttonAnimator.keepAnimatorStateOnDisable = true;
				}
			}
		}

		protected override void OnDestroy()
		{
			onClick.RemoveListener(OnClick);

			base.OnDestroy();
		}

		// PRIVATE METHODS

		private void OnClick()
		{
			PlayClickSound();
		}
	}
}
