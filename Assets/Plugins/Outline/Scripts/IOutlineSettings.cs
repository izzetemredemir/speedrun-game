namespace Plugins.Outline
{
	using UnityEngine;
	using UnityEngine.Rendering.Universal;

	public interface IOutlineSettings
	{
		float              Width      { get; set; }
		float              Intensity  { get; set; }
		Color              Color      { get; set; }
		RenderPassEvent    Pass       { get; set; }
		EOutlineUpdateMode UpdateMode { get; set; }
	}
}
