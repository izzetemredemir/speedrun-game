namespace Plugins.Outline
{
	using System;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.Rendering.Universal;

	[DisallowMultipleRendererFeature]
	public sealed class OutlineFeature : ScriptableRendererFeature
	{
		public OutlineResources Resources => _resources;

		[SerializeField]
		private OutlineResources _resources;

		private static bool          _isInitialized;
		private static OutlinePass[] _outlinePasses;

		public override void Create()
		{
			if (_isInitialized == true)
				return;

			List<OutlinePass> outlinePasses = new List<OutlinePass>();

			foreach (int renderPass in Enum.GetValues(typeof(RenderPassEvent)))
			{
				OutlinePass outlinePass = new OutlinePass(this);
				outlinePass.renderPassEvent = (RenderPassEvent)renderPass;

				outlinePasses.Add(outlinePass);
			}

			_outlinePasses = outlinePasses.ToArray();

			_isInitialized = true;
		}

		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			if (_isInitialized == false)
				return;

			for (int i = 0; i < _outlinePasses.Length; ++i)
			{
				OutlinePass outlinePass = _outlinePasses[i];
				if (outlinePass.IsActive == true)
				{
					outlinePass.Setup(renderer);
					renderer.EnqueuePass(outlinePass);
				}
			}
		}

		public static bool HasObject(GameObject gameObject, IOutlineSettings settings)
		{
			if (_isInitialized == false)
				return false;

			for (int i = 0; i < _outlinePasses.Length; ++i)
			{
				OutlinePass outlinePass = _outlinePasses[i];
				if (outlinePass.renderPassEvent == settings.Pass)
				{
					return outlinePass.HasObject(gameObject);
				}
			}

			return false;
		}

		public static bool RegisterObject(GameObject gameObject, IOutlineSettings settings)
		{
			if (_isInitialized == false)
				return false;
			if (gameObject == null)
				return false;
			if (settings == null)
				return false;

			for (int i = 0; i < _outlinePasses.Length; ++i)
			{
				OutlinePass outlinePass = _outlinePasses[i];
				if (outlinePass.renderPassEvent == settings.Pass)
				{
					if (outlinePass.HasObject(gameObject) == true)
						return true;

					List<Renderer> renderers = new List<Renderer>();
					if (settings.UpdateMode == EOutlineUpdateMode.Self)
					{
						Renderer renderer = gameObject.GetComponent<Renderer>();
						if (renderer != null && gameObject.GetComponentInParent<IgnoreOutline>() == null)
						{
							renderers.Add(renderer);
						}
					}
					else
					{
						gameObject.GetComponentsInChildren<Renderer>(true, renderers);

						for (int j = renderers.Count - 1; j >= 0; --j)
						{
							if (renderers[j].GetComponentInParent<IgnoreOutline>() != null)
							{
								renderers.RemoveAt(j);
							}
						}
					}

					outlinePass.RegisterObject(gameObject, renderers, settings, false);
					return true;
				}
			}

			return false;
		}

		public static bool RegisterObject(GameObject gameObject, Renderer renderer, IOutlineSettings settings)
		{
			if (_isInitialized == false)
				return false;
			if (gameObject == null)
				return false;
			if (renderer == null)
				return false;
			if (settings == null)
				return false;

			for (int i = 0; i < _outlinePasses.Length; ++i)
			{
				OutlinePass outlinePass = _outlinePasses[i];
				if (outlinePass.renderPassEvent == settings.Pass)
				{
					if (outlinePass.HasObject(gameObject) == true)
						return true;

					List<Renderer> renderers = new List<Renderer>() { renderer };

					outlinePass.RegisterObject(gameObject, renderers, settings, false);
					return true;
				}
			}

			return false;
		}

		public static bool RegisterObject(GameObject gameObject, List<Renderer> renderers, IOutlineSettings settings)
		{
			if (_isInitialized == false)
				return false;
			if (gameObject == null)
				return false;
			if (renderers == null)
				return false;
			if (settings == null)
				return false;

			for (int i = 0; i < _outlinePasses.Length; ++i)
			{
				OutlinePass outlinePass = _outlinePasses[i];
				if (outlinePass.renderPassEvent == settings.Pass)
				{
					outlinePass.RegisterObject(gameObject, renderers, settings, true);
					return true;
				}
			}

			return false;
		}

		public static void UnregisterObject(GameObject gameObject)
		{
			if (_isInitialized == false)
				return;
			if (gameObject == null)
				return;

			for (int i = 0; i < _outlinePasses.Length; ++i)
			{
				_outlinePasses[i].UnregisterObject(gameObject);
			}
		}
	}
}
