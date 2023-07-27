using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using DG.Tweening;

namespace TPSBR.UI
{
	public struct GameplayEventData
	{
		public Color Color;
		public string Name;
		public string Description;
		public AudioSetup Sound;
	}

	public class UIGameplayEvents : UIWidget
	{
		// PUBLIC METHODS

		public bool EventIsActive => _eventRoutine != null;

		// PRIVATE MEMBERS

		[SerializeField]
		private CanvasGroup _eventGroup;
		[SerializeField]
		private TextMeshProUGUI _name;
		[SerializeField]
		private TextMeshProUGUI _description;
		[SerializeField]
		private CanvasGroup _descriptionGroup;

		[Header("Animation")]
		[SerializeField]
		private float _minVisibilityTime = 0.5f;
		[SerializeField]
		private float _maxVisibilityTime = 3f;
		[SerializeField]
		private float _fadeOutTime = 0.2f;

		private Coroutine _eventRoutine;
		private List<GameplayEventData> _pendingEvents = new List<GameplayEventData>(8);

		// PUBLIC METHODS

		public void ShowEvent(GameplayEventData data, bool force = false, bool clearPending = false)
		{
			if (force == true)
			{
				HideEvent(clearPending);
			}
			else if (clearPending == true)
			{
				_pendingEvents.Clear();
			}

			_pendingEvents.Add(data);
		}

		public void HideEvent(bool clearPending = false)
		{
			if (_eventRoutine != null)
			{
				StopCoroutine(_eventRoutine);
				_eventRoutine = null;
			}

			_eventGroup.SetActive(false);

			if (clearPending == true)
			{
				_pendingEvents.Clear();
			}
		}

		// UIWidget INTERFACE

		protected override void OnVisible()
		{
			HideEvent();
		}

		protected override void OnTick()
		{
			if (EventIsActive == false && _pendingEvents.Count > 0)
			{
				var data = _pendingEvents[0];
				_pendingEvents.RemoveAt(0);

				_eventRoutine = StartCoroutine(ShowEvent_Coroutine(data));
			}
		}

		// PRIVATE METHODS

		private IEnumerator ShowEvent_Coroutine(GameplayEventData data)
		{
			DOTween.Kill(_eventGroup);

			PlaySound(data.Sound);

			_eventGroup.SetActive(true);
			_name.text = data.Name;
			_name.color = data.Color;

			if (data.Description.HasValue() == true)
			{
				_description.text = data.Description;
				_descriptionGroup.SetVisibility(true);
			}
			else
			{
				_descriptionGroup.SetVisibility(false);
			}

			float elapsedTime = 0f;

			while (elapsedTime < GetEventVisibilityTime())
			{
				elapsedTime += Time.deltaTime;
				yield return null;
			}

			_eventGroup.DOFade(0f, _fadeOutTime);

			yield return new WaitForSeconds(_fadeOutTime);

			HideEvent();
		}

		private float GetEventVisibilityTime()
		{
			return _pendingEvents.Count > 0 ? _minVisibilityTime : _maxVisibilityTime;
		}
	}
}
