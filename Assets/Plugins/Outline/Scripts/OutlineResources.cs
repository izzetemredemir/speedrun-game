namespace Plugins.Outline
{
	using System;
	using UnityEngine;

	[CreateAssetMenu(fileName = "OutlineResources", menuName = "Outline/Resources")]
	public sealed class OutlineResources : ScriptableObject
	{
		[SerializeField]
		private Shader _renderShader;
		[SerializeField]
		private Shader _outlineShader;

		private Material _renderMaterial;
		private Material _outlineMaterial;

		private static float[] _samples = new float[OutlineSettings.MaxWidth];

		public Material RenderMaterial
		{
			get
			{
				if (_renderMaterial == null)
				{
					_renderMaterial = new Material(_renderShader)
					{
						name = "Render",
						hideFlags = HideFlags.HideAndDontSave
					};
				}

				return _renderMaterial;
			}
		}

		public Material OutlineMaterial
		{
			get
			{
				if (_outlineMaterial == null)
				{
					_outlineMaterial = new Material(_outlineShader)
					{
						name = "Outline",
						hideFlags = HideFlags.HideAndDontSave
					};
				}

				return _outlineMaterial;
			}
		}

		public static float[] GetSamples(float width)
		{
			width = Mathf.Clamp(width, 0.0f, OutlineSettings.MaxWidth);

			float stdDev = width * 0.5f;
			for (int i = 0; i < OutlineSettings.MaxWidth; i++)
			{
				_samples[i] = Gauss(i, stdDev);
			}

			return _samples;
		}

		private static float Gauss(float x, float stdDev)
		{
			float stdDev2 = stdDev * stdDev * 2.0f;
			float a = 1.0f / Mathf.Sqrt(Mathf.PI * stdDev2);
			float gauss = a * Mathf.Pow((float)Math.E, -x * x / stdDev2);

			return gauss;
		}

		private void OnValidate()
		{
			if (_renderMaterial)
			{
				_renderMaterial.shader = _renderShader;
			}

			if (_outlineMaterial)
			{
				_outlineMaterial.shader = _outlineShader;
			}
		}
	}
}
