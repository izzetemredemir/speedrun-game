using System;
using System.Collections.Generic;
using UnityEngine;

namespace TPSBR.UI
{
	public class SceneUI : SceneService, IBackHandler
	{
		// PUBLIC MEMBERS

		public Canvas Canvas   { get; private set; }
		public Camera UICamera { get; private set; }

		// PRIVATE MEMBERS

		[SerializeField]
		private UIView[] _defaultViews;
		[SerializeField]
		private AudioEffect[] _audioEffects;
		[SerializeField]
		private AudioSetup _clickSound;

		private ScreenOrientation _lastScreenOrientation;

		// SceneUI INTERFACE

		protected UIView[] _views;

		protected virtual void OnInitializeInternal()   { }
		protected virtual void OnDeinitializeInternal() { }
		protected virtual void OnTickInternal()         { }
		protected virtual bool OnBackAction()           { return false; }
		protected virtual void OnViewOpened(UIView view) { }
		protected virtual void OnViewClosed(UIView view) { }

		// PUBLIC METHODS

		public T Get<T>() where T : UIView
		{
			if (_views == null)
				return null;

			for (int i = 0; i < _views.Length; ++i)
			{
				T view = _views[i] as T;

				if (view != null)
					return view;
			}

			return null;
		}

		public T Open<T>() where T : UIView
		{
			if (_views == null)
				return null;

			for (int i = 0; i < _views.Length; ++i)
			{
				T view = _views[i] as T;
				if (view != null)
				{
					OpenView(view);
					return view;
				}
			}

			return null;
		}

		public void Open(UIView view)
		{
			if (_views == null)
				return;

			int index = Array.IndexOf(_views, view);

			if (index < 0)
			{
				Debug.LogError($"Cannot find view {view.name}");
				return;
			}

			OpenView(view);
		}

		/*
		public T OpenWithBackView<T>(UIView backView) where T : UICloseView
		{
			T view = Open<T>();

			if (view is UICloseView closeView)
			{
				closeView.BackView = backView;
			}

			return view;
		}
		*/

		public T Close<T>() where T : UIView
		{
			if (_views == null)
				return null;

			for (int i = 0; i < _views.Length; ++i)
			{
				T view = _views[i] as T;
				if (view != null)
				{
					view.Close();
					return view;
				}
			}

			return null;
		}

		public void Close(UIView view)
		{
			if (_views == null)
				return;

			int index = Array.IndexOf(_views, view);

			if (index < 0)
			{
				Debug.LogError($"Cannot find view {view.name}");
				return;
			}

			CloseView(view);
		}

		public T Toggle<T>() where T : UIView
		{
			if (_views == null)
				return null;

			for (int i = 0; i < _views.Length; ++i)
			{
				T view = _views[i] as T;
				if (view != null)
				{
					if (view.IsOpen == true)
					{
						CloseView(view);
					}
					else
					{
						OpenView(view);
					}

					return view;
				}
			}

			return null;
		}

		public bool IsOpen<T>() where T : UIView
		{
			if (_views == null)
				return false;

			for (int i = 0; i < _views.Length; ++i)
			{
				T view = _views[i] as T;
				if (view != null)
				{
					return view.IsOpen;
				}
			}

			return false;
		}

		public bool IsTopView(UIView view, bool interactableOnly = false)
		{
			if (view.IsOpen == false)
				return false;

			if (_views == null)
				return false;

			int highestPriority = -1;

			for (int i = 0; i < _views.Length; ++i)
			{
				var otherView = _views[i];

				if (otherView == view)
					continue;

				if (otherView.IsOpen == false)
					continue;

				if (interactableOnly == true && otherView.IsInteractable == false)
					continue;

				highestPriority = Math.Max(highestPriority, otherView.Priority);
			}

			return view.Priority > highestPriority;
		}

		public void CloseAll()
		{
			if (_views == null)
				return;

			for (int i = 0; i < _views.Length; ++i)
			{
				CloseView(_views[i]);
			}
		}

		public void GetAll<T>(List<T> list)
		{
			if (_views == null)
				return;

			for (int i = 0; i < _views.Length; ++i)
			{
				if (_views[i] is T element)
				{
					list.Add(element);
				}
			}
		}

		public bool PlaySound(AudioSetup effectSetup, EForceBehaviour force = EForceBehaviour.None)
		{
			return _audioEffects.PlaySound(effectSetup, force);
		}

		public bool PlayClickSound()
		{
			return PlaySound(_clickSound);
		}

		// IBackHandler INTERFACE

		int  IBackHandler.Priority       => -1;
		bool IBackHandler.IsActive       => true;
		bool IBackHandler.OnBackAction() { return OnBackAction(); }

		// GameService INTERFACE

		protected override sealed void OnInitialize()
		{
			Canvas   = GetComponent<Canvas>();
			UICamera = Canvas.worldCamera;
			_views   = GetComponentsInChildren<UIView>(true);

			for (int i = 0; i < _views.Length; ++i)
			{
				UIView view = _views[i];

				view.Initialize(this, null);
				view.SetPriority(i);

				view.gameObject.SetActive(false);
			}

			OnInitializeInternal();

			UpdateScreenOrientation();
		}

		protected override sealed void OnDeinitialize()
		{
			OnDeinitializeInternal();

			if (_views != null)
			{
				for (int i = 0; i < _views.Length; ++i)
				{
					_views[i].Deinitialize();
				}

				_views = null;
			}
		}

		protected override void OnActivate()
		{
			if (ApplicationSettings.IsStrippedBatch == true)
			{
				Canvas.enabled = false;
				return;
			}

			base.OnActivate();

			Canvas.enabled = true;

			for (int i = 0, count = _defaultViews.SafeCount(); i < count; i++)
			{
				Open(_defaultViews[i]);
			}
		}

		protected override void OnDeactivate()
		{
			if (ApplicationSettings.IsStrippedBatch == true)
				return;

			base.OnDeactivate();

			for (int i = 0, count = _views.SafeCount(); i < count; i++)
			{
				Close(_views[i]);
			}

			if (Canvas != null)
			{
				Canvas.enabled = false;
			}
		}

		protected override sealed void OnTick()
		{
			if (ApplicationSettings.IsStrippedBatch == true)
				return;

			UpdateScreenOrientation();

			if (_views != null)
			{
				for (int i = 0; i < _views.Length; ++i)
				{
					UIView view = _views[i];
					if (view.IsOpen == true)
					{
						view.Tick();
					}
				}
			}

			OnTickInternal();
		}

		// PRIVATE MEMBERS

		private void UpdateScreenOrientation()
		{
			if (_lastScreenOrientation == Screen.orientation)
				return;

			if (_views != null)
			{
				for (int i = 0; i < _views.Length; ++i)
				{
					_views[i].UpdateSafeArea();
				}

				_lastScreenOrientation = Screen.orientation;
			}
		}

		private void OpenView(UIView view)
		{
			if (view == null)
				return;

			if (view.IsOpen == true)
				return;

			view.Open_Internal();

			OnViewOpened(view);
		}

		private void CloseView(UIView view)
		{
			if (view == null)
				return;

			if (view.IsOpen == false)
				return;

			view.Close_Internal();

			OnViewClosed(view);
		}
	}
}
