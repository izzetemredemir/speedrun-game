namespace Plugins.Outline
{
	using System.Collections.Generic;
	using UnityEngine;

	public sealed class OutlineObject
	{
		public GameObject       GameObject => _gameObject;
		public List<Renderer>   Renderers  => _renderers;
		public IOutlineSettings Settings   => _settings;

		private readonly GameObject       _gameObject;
		private readonly List<Renderer>   _renderers;
		private readonly IOutlineSettings _settings;

		public OutlineObject(GameObject gameObject, List<Renderer> renderers, IOutlineSettings settings)
		{
			_gameObject = gameObject;
			_renderers  = renderers;
			_settings   = settings;
		}
	}
}
