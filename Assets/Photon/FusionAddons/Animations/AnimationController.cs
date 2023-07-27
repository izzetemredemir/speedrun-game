namespace Fusion.Animations
{
	using System;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.Animations;
	using UnityEngine.Playables;
	using UnityEngine.Profiling;

	#pragma warning disable 0109

	[DisallowMultipleComponent]
	public abstract unsafe partial class AnimationController : NetworkBehaviour, IBeforeAllTicks, IAfterTick
	{
		// PUBLIC MEMBERS

		public PlayableGraph               Graph             => _graph;
		public AnimationLayerMixerPlayable Mixer             => _mixer;
		public IList<AnimationLayer>       Layers            => _layers;
		public Animator                    Animator          => _animator;
		public bool                        HasManualUpdate   => _hasManualUpdate;
		public new bool                    HasInputAuthority => _hasInputAuthority;
		public new bool                    HasStateAuthority => _hasStateAuthority;
		public bool                        HasAnyAuthority   => _hasInputAuthority == true || _hasStateAuthority == true;
		public new bool                    IsProxy           => _hasInputAuthority == false && _hasStateAuthority == false;
		public float                       StateAlpha        => _stateAlpha;
		public float                       DeltaTime         => _deltaTime;

		// PRIVATE MEMBERS

		[SerializeField]
		private Transform                   _root;
		[SerializeField]
		private Animator                    _animator;

		private PlayableGraph               _graph;
		private AnimationLayerMixerPlayable _mixer;
		private AnimationPlayableOutput     _output;
		private AnimationLayer[]            _layers;
		private bool                        _isSpawned;
		private bool                        _hasManualUpdate;
		private bool                        _hasInputAuthority;
		private bool                        _hasStateAuthority;
		private bool                        _suppressEvaluation;
		private int                         _evaluationFrame;
		private int                         _evaluationRate;
		private int                         _evaluationSeed;
		private float                       _stateAlpha;
		private float                       _deltaTime;

		// PUBLIC METHODS

		public void SuppressEvaluation(bool suppressEvaluation)
		{
			_suppressEvaluation = suppressEvaluation;
		}

		public void SetEvaluationFrame(int evaluationFrame)
		{
			if (evaluationFrame < 0)
				throw new ArgumentException(nameof(evaluationFrame));

			_evaluationFrame = evaluationFrame;
		}

		public void SetEvaluationRate(int evaluationRate)
		{
			if (evaluationRate < 0)
				throw new ArgumentException(nameof(evaluationRate));

			_evaluationRate = evaluationRate;
		}

		public void SetEvaluationSeed(int evaluationSeed)
		{
			if (evaluationSeed < 0)
				throw new ArgumentException(nameof(evaluationSeed));

			_evaluationSeed = evaluationSeed;
		}

		public void SetAnimator(Animator animator)
		{
			_animator = animator;

			if (_graph.IsValid() == false)
				return;

			if (_output.IsOutputValid() == true)
			{
				_graph.DestroyOutput(_output);
				_output = default;
			}

			if (_animator != null)
			{
				_output = AnimationPlayableOutput.Create(_graph, name, _animator);
				_output.SetSourcePlayable(_mixer);
			}
		}

		public void SetManualUpdate(bool hasManualUpdate)
		{
			_hasManualUpdate = hasManualUpdate;
		}

		public void ManualFixedUpdate()
		{
			if (_hasManualUpdate == false)
				throw new InvalidOperationException("Manual update is not set!");

			Profiler.BeginSample("AnimationController.FixedUpdate");
			OnFixedUpdateInternal();
			Profiler.EndSample();
		}

		public void ManualRenderUpdate()
		{
			if (_hasManualUpdate == false)
				throw new InvalidOperationException("Manual update is not set!");

			Profiler.BeginSample("AnimationController.RenderUpdate");
			OnRenderUpdateInternal();
			Profiler.EndSample();
		}

		public void Interpolate()
		{
			Profiler.BeginSample("AnimationController.Interpolate");
			InterpolateNetworkData();

			AnimationLayer[] layers = _layers;
			for (int i = 0, count = layers.Length; i < count; ++i)
			{
				AnimationLayer layer = layers[i];
				layer.Interpolate();
			}

			OnInterpolate();

			for (int i = 0, count = layers.Length; i < count; ++i)
			{
				AnimationLayer layer = layers[i];
				layer.SetPlayableWeights(true);
			}

			Evaluate(false);
			Profiler.EndSample();
		}

		public T FindLayer<T>() where T : class
		{
			AnimationLayer[] layers = _layers;
			for (int i = 0, count = layers.Length; i < count; ++i)
			{
				if (layers[i] is T layer)
					return layer;
			}

			return default;
		}

		public T FindState<T>() where T : class
		{
			AnimationLayer[] layers = _layers;
			for (int i = 0, count = layers.Length; i < count; ++i)
			{
				AnimationLayer layer = layers[i];
				if (layer.FindState<T>(out T state) == true)
					return state;
			}

			return default;
		}

		[System.Diagnostics.Conditional("ANIMATION_LOGS")]
		public static void Log(string message, GameObject context)
		{
			Debug.Log($"[{Time.realtimeSinceStartup.ToString("F3")}] {message}", context);
		}

		// AnimationController INTERFACE

		protected virtual void OnInitialize()   {}
		protected virtual void OnDeinitialize() {}
		protected virtual void OnSpawned()      {}
		protected virtual void OnDespawned()    {}
		protected virtual void OnFixedUpdate()  {}
		protected virtual void OnInterpolate()  {}
		protected virtual void OnEvaluate()     {}

		// NetworkBehaviour INTERFACE

		public override sealed int? DynamicWordCount => GetNetworkDataWordCount();

		public override sealed void Spawned()
		{
			_isSpawned         = true;
			_hasInputAuthority = Object.HasInputAuthority;
			_hasStateAuthority = Object.HasStateAuthority;
			_stateAlpha        = 1.0f;
			_deltaTime         = Runner.Simulation.DeltaTime;

			_graph = PlayableGraph.Create(name);
			_graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

			_mixer = AnimationLayerMixerPlayable.Create(_graph);

			_output = AnimationPlayableOutput.Create(_graph, name, _animator);
			_output.SetSourcePlayable(_mixer);

			AnimationLayer[] layers = _layers;
			for (int i = 0, count = layers.Length; i < count; ++i)
			{
				AnimationLayer layer = layers[i];
				layer.Spawned();
			}

			OnSpawned();

			if (HasAnyAuthority == true)
			{
				WriteNetworkData();
			}
		}

		public override sealed void Despawned(NetworkRunner runner, bool hasState)
		{
			_isSpawned = false;

			OnDespawned();

			AnimationLayer[] layers = _layers;
			for (int i = 0, count = layers != null ? layers.Length : 0; i < count; ++i)
			{
				AnimationLayer layer = layers[i];
				if (layer != null)
				{
					layer.Despawned();
				}
			}

			if (_graph.IsValid() == true)
			{
				_graph.Destroy();
			}

			SetDefaults();
		}

		public override sealed void FixedUpdateNetwork()
		{
			if (_hasManualUpdate == true)
				return;

			Profiler.BeginSample("AnimationController.FixedUpdate");
			OnFixedUpdateInternal();
			Profiler.EndSample();
		}

		public override sealed void Render()
		{
			if (_hasManualUpdate == true)
				return;

			Profiler.BeginSample("AnimationController.RenderUpdate");
			OnRenderUpdateInternal();
			Profiler.EndSample();
		}

		// IBeforeAllTicks INTERFACE

		void IBeforeAllTicks.BeforeAllTicks(bool resimulation, int tickCount)
		{
			if (resimulation == false)
				return;

			Profiler.BeginSample("AnimationController.BeforeAllTicks");

			_hasInputAuthority = Object.HasInputAuthority;
			_hasStateAuthority = Object.HasStateAuthority;

			ReadNetworkData();

			Profiler.EndSample();
		}

		// IAfterTick INTERFACE

		void IAfterTick.AfterTick()
		{
			Profiler.BeginSample("AnimationController.AfterTick");

			if (HasAnyAuthority == true)
			{
				WriteNetworkData();
			}

			Profiler.EndSample();
		}

		// MonoBehaviour INTERFACE

		protected virtual void Awake()
		{
			InitializeLayers();
			InitializeNetworkProperties();

			OnInitialize();
		}

		protected virtual void OnDestroy()
		{
			if (_isSpawned == true)
			{
				Despawned(null, false);
			}

			OnDeinitialize();

			DeinitializeNetworkProperties();
			DeinitializeLayers();
		}

		// PRIVATE METHODS

		private void InitializeLayers()
		{
			if (_layers != null)
				return;

			List<AnimationLayer> activeLayers = new List<AnimationLayer>(8);

			Transform root = _root;
			for (int i = 0, count = root.childCount; i < count; ++i)
			{
				Transform child = root.GetChild(i);

				AnimationLayer layer = child.GetComponentNoAlloc<AnimationLayer>();
				if (layer != null && layer.enabled == true && layer.gameObject.activeSelf == true)
				{
					activeLayers.Add(layer);
				}
			}

			_layers = activeLayers.ToArray();

			AnimationLayer[] layers = _layers;
			for (int i = 0, count = layers.Length; i < count; ++i)
			{
				AnimationLayer layer = layers[i];
				layer.Initialize(this);
			}
		}

		private void DeinitializeLayers()
		{
			AnimationLayer[] layers = _layers;
			for (int i = 0, count = layers != null ? layers.Length : 0; i < count; ++i)
			{
				AnimationLayer layer = layers[i];
				if (layer != null)
				{
					layer.Deinitialize();
				}
			}

			_layers = null;
		}

		private void OnFixedUpdateInternal()
		{
			if (Runner.Stage == default)
				throw new InvalidOperationException();
			if (HasAnyAuthority == false)
				return;

			_stateAlpha = 1.0f;
			_deltaTime  = Runner.Simulation.DeltaTime;

			_hasInputAuthority = Object.HasInputAuthority;
			_hasStateAuthority = Object.HasStateAuthority;

			AnimationLayer[] layers = _layers;
			for (int i = 0, count = layers.Length; i < count; ++i)
			{
				AnimationLayer layer = layers[i];
				layer.ManualFixedUpdate();
			}

			OnFixedUpdate();

			for (int i = 0, count = layers.Length; i < count; ++i)
			{
				AnimationLayer layer = layers[i];
				layer.SetPlayableWeights(false);
			}

			if (_hasStateAuthority == true || Runner.IsResimulation == false)
			{
				Evaluate(true);
			}
		}

		private void OnRenderUpdateInternal()
		{
			if (Runner.Stage != default)
				throw new InvalidOperationException();

			_stateAlpha = Runner.Simulation.StateAlpha;
			_deltaTime  = Time.deltaTime;

			InterpolateNetworkData();

			AnimationLayer[] layers = _layers;
			for (int i = 0, count = layers.Length; i < count; ++i)
			{
				AnimationLayer layer = layers[i];
				layer.Interpolate();
			}

			OnInterpolate();

			for (int i = 0, count = layers.Length; i < count; ++i)
			{
				AnimationLayer layer = layers[i];
				layer.SetPlayableWeights(true);
			}

			Evaluate(false);
		}

		private void Evaluate(bool checkEvaluationRate)
		{
			if (_suppressEvaluation == true)
				return;

			if (checkEvaluationRate == true && _evaluationRate > 1)
			{
				int rateSeed   = _evaluationSeed  % _evaluationRate;
				int targetSeed = _evaluationFrame % _evaluationRate;

				if (rateSeed != targetSeed)
					return;
			}

			Profiler.BeginSample("AnimationController.Evaluate");
			Profiler.BeginSample("PlayableGraph.Evaluate");
			_graph.Evaluate();
			Profiler.EndSample();

			OnEvaluate();
			Profiler.EndSample();
		}

		private void SetDefaults()
		{
			_hasManualUpdate    = default;
			_hasInputAuthority  = default;
			_hasStateAuthority  = default;
			_suppressEvaluation = default;
			_evaluationFrame    = default;
			_evaluationRate     = default;
			_evaluationSeed     = default;
		}
	}
}
