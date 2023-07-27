namespace Plugins.Outline
{
	using UnityEngine;
	using UnityEngine.Rendering;

	public static class OutlineConstants
	{
		public const int RenderShaderDefaultPassId   = 0;
		public const int RenderShaderAlphaTestPassId = 1;
		public const int OutlineShaderHPassId        = 0;
		public const int OutlineShaderVPassId        = 1;

		public const string MainTexName   = "_MainTex";
		public const string MaskTexName   = "_MaskTex";
		public const string TempTexName   = "_TempTex";
		public const string ColorName     = "_Color";
		public const string WidthName     = "_Width";
		public const string IntensityName = "_Intensity";
		public const string SamplesName   = "_Samples";

		public static readonly int MainTexId   = Shader.PropertyToID(MainTexName);
		public static readonly int MaskTexId   = Shader.PropertyToID(MaskTexName);
		public static readonly int TempTexId   = Shader.PropertyToID(TempTexName);
		public static readonly int ColorId     = Shader.PropertyToID(ColorName);
		public static readonly int WidthId     = Shader.PropertyToID(WidthName);
		public static readonly int IntensityId = Shader.PropertyToID(IntensityName);
		public static readonly int SamplesId   = Shader.PropertyToID(SamplesName);

		public static readonly RenderTargetIdentifier MainTex = new RenderTargetIdentifier(MainTexName);
		public static readonly RenderTargetIdentifier MaskTex = new RenderTargetIdentifier(MaskTexName);
		public static readonly RenderTargetIdentifier TempTex = new RenderTargetIdentifier(TempTexName);
	}
}
