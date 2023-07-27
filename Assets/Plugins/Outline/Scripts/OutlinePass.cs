namespace Plugins.Outline
{
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.Rendering;
	using UnityEngine.Rendering.Universal;

	public sealed class OutlinePass : ScriptableRenderPass
	{
		public bool IsActive => _objects.Count > 0;

		private OutlineFeature      _feature;
		private ScriptableRenderer  _renderer;
		private CommandBuffer       _commandBuffer;
		private List<OutlineObject> _objects = new List<OutlineObject>();

		private static readonly List<Material>        _materials          = new List<Material>();
		private static readonly MaterialPropertyBlock _materialProperties = new MaterialPropertyBlock();

		public OutlinePass(OutlineFeature feature)
		{
			_feature = feature;
		}

		public void Setup(ScriptableRenderer renderer)
		{
			_renderer = renderer;
		}

		public bool HasObject(GameObject gameObject)
		{
			for (int i = 0; i < _objects.Count; ++i)
			{
				if (object.ReferenceEquals(_objects[i].GameObject, gameObject) == true)
					return true;
			}

			return false;
		}

		public void RegisterObject(GameObject gameObject, List<Renderer> renderers, IOutlineSettings settings, bool checkExisting)
		{
			if (checkExisting == true)
			{
				for (int i = 0; i < _objects.Count; ++i)
				{
					if (object.ReferenceEquals(_objects[i].GameObject, gameObject) == true)
						return;
				}
			}

			_objects.Add(new OutlineObject(gameObject, renderers, settings));
		}

		public void UnregisterObject(GameObject gameObject)
		{
			for (int i = 0; i < _objects.Count; ++i)
			{
				if (object.ReferenceEquals(_objects[i].GameObject, gameObject) == true)
				{
					_objects.RemoveAt(i);
					return;
				}
			}
		}

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			if (_objects.Count == 0)
				return;
			if (_feature.Resources == null)
				return;

			RenderTextureDescriptor renderTextureDescriptor = renderingData.cameraData.cameraTargetDescriptor;
			renderTextureDescriptor.shadowSamplingMode = ShadowSamplingMode.None;
			renderTextureDescriptor.depthBufferBits    = 0;
			renderTextureDescriptor.colorFormat        = RenderTextureFormat.R8;
			renderTextureDescriptor.msaaSamples        = 1;

			_commandBuffer = CommandBufferPool.Get("Outline");
			_commandBuffer.GetTemporaryRT(OutlineConstants.MaskTexId, renderTextureDescriptor, FilterMode.Bilinear);
			_commandBuffer.GetTemporaryRT(OutlineConstants.TempTexId, renderTextureDescriptor, FilterMode.Bilinear);

			for (int i = 0; i < _objects.Count; i++)
			{
				OutlineObject outlineObject = _objects[i];

				if (outlineObject.Settings.UpdateMode != EOutlineUpdateMode.None)
				{
					outlineObject.Renderers.Clear();

					if (outlineObject.Settings.UpdateMode == EOutlineUpdateMode.Self)
					{
						Renderer renderer = outlineObject.GameObject.GetComponent<Renderer>();
						if (renderer != null && outlineObject.GameObject.GetComponentInParent<IgnoreOutline>() == null)
						{
							outlineObject.Renderers.Add(renderer);
						}
					}
					else
					{
						outlineObject.GameObject.GetComponentsInChildren<Renderer>(true, outlineObject.Renderers);

						for (int j = outlineObject.Renderers.Count - 1; j >= 0; --j)
						{
							if (outlineObject.Renderers[j].GetComponentInParent<IgnoreOutline>() != null)
							{
								outlineObject.Renderers.RemoveAt(j);
							}
						}
					}
				}

				if (outlineObject.Renderers.Count == 0)
					continue;

				ClearRenderTarget(false);

				RenderObject(outlineObject);
				RenderOutline(outlineObject);
			}

			_commandBuffer.ReleaseTemporaryRT(OutlineConstants.TempTexId);
			_commandBuffer.ReleaseTemporaryRT(OutlineConstants.MaskTexId);

			context.ExecuteCommandBuffer(_commandBuffer);
			CommandBufferPool.Release(_commandBuffer);
		}

		private void ClearRenderTarget(bool useDepth)
		{
			if (useDepth)
			{
#if UNITY_2023_1_OR_NEWER
				_commandBuffer.SetRenderTarget(OutlineConstants.MaskTex, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, _renderer.cameraDepthTargetHandle, RenderBufferLoadAction.Load, RenderBufferStoreAction.DontCare);
#else
				_commandBuffer.SetRenderTarget(OutlineConstants.MaskTex, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, _renderer.cameraDepthTarget, RenderBufferLoadAction.Load, RenderBufferStoreAction.DontCare);
#endif
			}
			else
			{
				_commandBuffer.SetRenderTarget(OutlineConstants.MaskTex, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
			}

			_commandBuffer.ClearRenderTarget(false, true, Color.clear);
		}

		private void RenderObject(OutlineObject outlineObject)
		{
			for (int i = 0; i < outlineObject.Renderers.Count; ++i)
			{
				Renderer renderer = outlineObject.Renderers[i];

				if (renderer == null || renderer.enabled == false || renderer.isVisible == false || renderer.gameObject.activeInHierarchy == false)
					continue;

				renderer.GetSharedMaterials(_materials);

				if (_materials.Count > 0)
				{
					for (int j = 0; j < _materials.Count; ++j)
					{
						_commandBuffer.DrawRenderer(renderer, _feature.Resources.RenderMaterial, j, OutlineConstants.RenderShaderDefaultPassId);
					}
				}
				else
				{
					_commandBuffer.DrawRenderer(renderer, _feature.Resources.RenderMaterial, 0, OutlineConstants.RenderShaderDefaultPassId);
				}
			}
		}

		public void RenderOutline(OutlineObject outlineObject)
		{
			_materialProperties.SetFloat(OutlineConstants.WidthId, outlineObject.Settings.Width);
			_materialProperties.SetColor(OutlineConstants.ColorId, outlineObject.Settings.Color);
			_materialProperties.SetFloat(OutlineConstants.IntensityId, outlineObject.Settings.Intensity);

			_commandBuffer.SetGlobalFloatArray(OutlineConstants.SamplesId, OutlineResources.GetSamples(outlineObject.Settings.Width));

			_commandBuffer.SetRenderTarget(OutlineConstants.TempTex, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
			Blit(OutlineConstants.MaskTex, OutlineConstants.OutlineShaderHPassId, _feature.Resources.OutlineMaterial, _materialProperties);

			_commandBuffer.SetRenderTarget(_renderer.cameraColorTarget, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
			Blit(OutlineConstants.TempTex, OutlineConstants.OutlineShaderVPassId, _feature.Resources.OutlineMaterial, _materialProperties);
		}

		private void Blit(RenderTargetIdentifier src, int shaderPass, Material material, MaterialPropertyBlock materialProperties)
		{
			_commandBuffer.SetGlobalTexture(OutlineConstants.MainTexId, src);
			_commandBuffer.DrawProcedural(Matrix4x4.identity, material, shaderPass, MeshTopology.Triangles, 3, 1, materialProperties);
		}
	}
}
