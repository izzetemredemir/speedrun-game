using System.Collections.Generic;
using UnityEngine;

namespace TPSBR
{
	using Fusion;
	using Fusion.Animations;

	[RequireComponent(typeof(AnimationController))]
	[OrderAfter(typeof(HitboxManager), typeof(AnimationController))]
	public sealed class AnimationsRecorder : NetworkBehaviour
	{
		// PRIVATE MEMBERS

		private AnimationController _controller;
		private StatsRecorder       _fixedRecorder    = new StatsRecorder();
		private StatsRecorder       _renderRecorder   = new StatsRecorder();
		private StatsRecorder       _combinedRecorder = new StatsRecorder();

		// NetworkBehaviour INTERFACE

		public override void Spawned()
		{
			List<string> fixedHeaders    = new List<string>();
			List<string> renderHeaders   = new List<string>();
			List<string> combinedHeaders = new List<string>();

			AddControllerHeader(_controller, fixedHeaders, true, false);
			AddControllerHeader(_controller, renderHeaders, false, true);
			AddControllerHeader(_controller, combinedHeaders, true, true);

			_fixedRecorder.Initialize(ApplicationUtility.GetFilePath($"Animations_{System.DateTime.Now:yyyy-MM-dd-HH-mm-ss}_FUN.log"), null, fixedHeaders.ToArray());
			_renderRecorder.Initialize(ApplicationUtility.GetFilePath($"Animations_{System.DateTime.Now:yyyy-MM-dd-HH-mm-ss}_Render.log"), null, renderHeaders.ToArray());
			_combinedRecorder.Initialize(ApplicationUtility.GetFilePath($"Animations_{System.DateTime.Now:yyyy-MM-dd-HH-mm-ss}_Combined.log"), null, combinedHeaders.ToArray());
		}

		public override void Despawned(NetworkRunner runner, bool hasState)
		{
			_fixedRecorder.Deinitialize();
			_renderRecorder.Deinitialize();
			_combinedRecorder.Deinitialize();
		}

		public override void FixedUpdateNetwork()
		{
			if (Runner.IsForward == false)
				return;

			RecordController(_fixedRecorder, _controller, true, false);
		}

		public override void Render()
		{
			RecordController(_renderRecorder, _controller, false, true);
			RecordController(_combinedRecorder, _controller, true, true);
		}

		// MonoBehaviour INTERFACE

		private void Awake()
		{
			_controller = GetComponent<AnimationController>();
		}

		// PRIVATE METHODS

		private static void AddControllerHeader(AnimationController controller, List<string> headers, bool addFixed, bool addRender)
		{
			headers.Add("Time");

			for (int i = 0; i < controller.Layers.Count; ++i)
			{
				AddLayerHeader(controller.Layers[i], headers, addFixed, addRender);
			}
		}

		private static void RecordController(StatsRecorder recorder, AnimationController controller, bool writeFixed, bool writeRender)
		{
			recorder.Add(Time.realtimeSinceStartup.ToString("F3", System.Globalization.CultureInfo.InvariantCulture));

			for (int i = 0; i < controller.Layers.Count; ++i)
			{
				RecordLayer(recorder, controller.Layers[i], writeFixed, writeRender);
			}

			recorder.Write();
		}

		//====================================================================================================

		private static void AddLayerHeader(AnimationLayer layer, List<string> headers, bool addFixed, bool addRender)
		{
			if (addFixed  == true) headers.Add($"L|{layer.name}|W");
			if (addRender == true) headers.Add($"L|{layer.name}|IW");

			headers.Add($"L|{layer.name}|PW");

			for (int i = 0; i < layer.States.Count; ++i)
			{
				AddStateHeader(layer.States[i], layer.name, headers, addFixed, addRender);
			}
		}

		private static void RecordLayer(StatsRecorder recorder, AnimationLayer layer, bool writeFixed, bool writeRender)
		{
			if (writeFixed  == true) recorder.Add(layer.Weight.ToString("F3", System.Globalization.CultureInfo.InvariantCulture));
			if (writeRender == true) recorder.Add(layer.InterpolatedWeight.ToString("F3", System.Globalization.CultureInfo.InvariantCulture));

			recorder.Add(layer.GetPlayableWeight().ToString("F3", System.Globalization.CultureInfo.InvariantCulture));

			for (int i = 0; i < layer.States.Count; ++i)
			{
				RecordState(recorder, layer.States[i], writeFixed, writeRender);
			}
		}

		//====================================================================================================

		private static void AddStateHeader(AnimationState state, string prefix, List<string> headers, bool addFixed, bool addRender)
		{
			if (addFixed  == true) headers.Add($"S|{prefix}|{state.name}|W");
			if (addRender == true) headers.Add($"S|{prefix}|{state.name}|IW");

			headers.Add($"S|{prefix}|{state.name}|PW");

			if (state is ClipState || state is MultiClipState || state is BlendTreeState || state is MultiBlendTreeState || state is MultiMirrorBlendTreeState)
			{
				if (addFixed  == true) headers.Add($"S|{prefix}|{state.name}|AT");
				if (addRender == true) headers.Add($"S|{prefix}|{state.name}|IAT");
			}

			for (int i = 0; i < state.States.Count; ++i)
			{
				AddStateHeader(state.States[i], $"{prefix}|{state.name}", headers, addFixed, addRender);
			}
		}

		private static void RecordState(StatsRecorder recorder, AnimationState state, bool writeFixed, bool writeRender)
		{
			if (writeFixed  == true) recorder.Add(state.Weight.ToString("F3", System.Globalization.CultureInfo.InvariantCulture));
			if (writeRender == true) recorder.Add(state.InterpolatedWeight.ToString("F3", System.Globalization.CultureInfo.InvariantCulture));

			recorder.Add(state.GetPlayableWeight().ToString("F3", System.Globalization.CultureInfo.InvariantCulture));

			if (state is ClipState clipState)
			{
				if (writeFixed  == true) recorder.Add(clipState.AnimationTime.ToString("F3", System.Globalization.CultureInfo.InvariantCulture));
				if (writeRender == true) recorder.Add(clipState.InterpolatedAnimationTime.ToString("F3", System.Globalization.CultureInfo.InvariantCulture));
			}
			else if (state is MultiClipState multiClipState)
			{
				if (writeFixed  == true) recorder.Add(multiClipState.AnimationTime.ToString("F3", System.Globalization.CultureInfo.InvariantCulture));
				if (writeRender == true) recorder.Add(multiClipState.InterpolatedAnimationTime.ToString("F3", System.Globalization.CultureInfo.InvariantCulture));
			}
			else if (state is BlendTreeState blendTreeState)
			{
				if (writeFixed  == true) recorder.Add(blendTreeState.AnimationTime.ToString("F3", System.Globalization.CultureInfo.InvariantCulture));
				if (writeRender == true) recorder.Add(blendTreeState.InterpolatedAnimationTime.ToString("F3", System.Globalization.CultureInfo.InvariantCulture));
			}
			else if (state is MultiBlendTreeState multiBlendTreeState)
			{
				if (writeFixed  == true) recorder.Add(multiBlendTreeState.AnimationTime.ToString("F3", System.Globalization.CultureInfo.InvariantCulture));
				if (writeRender == true) recorder.Add(multiBlendTreeState.InterpolatedAnimationTime.ToString("F3", System.Globalization.CultureInfo.InvariantCulture));
			}
			else if (state is MultiMirrorBlendTreeState multiMirrorBlendTreeState)
			{
				if (writeFixed  == true) recorder.Add(multiMirrorBlendTreeState.AnimationTime.ToString("F3", System.Globalization.CultureInfo.InvariantCulture));
				if (writeRender == true) recorder.Add(multiMirrorBlendTreeState.InterpolatedAnimationTime.ToString("F3", System.Globalization.CultureInfo.InvariantCulture));
			}

			for (int i = 0; i < state.States.Count; ++i)
			{
				RecordState(recorder, state.States[i], writeFixed, writeRender);
			}
		}
	}
}
