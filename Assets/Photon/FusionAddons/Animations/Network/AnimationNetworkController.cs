namespace Fusion.Animations
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Runtime.InteropServices;
	using Unity.Collections.LowLevel.Unsafe;

	public delegate void InterpolationDelegate(InterpolationData interpolationData);

	[AttributeUsage(AttributeTargets.Field)]
	public sealed class AnimationPropertyAttribute : Attribute
	{
		public readonly string InterpolationDelegate;

		public AnimationPropertyAttribute()
		{
		}

		public AnimationPropertyAttribute(string interpolationDelegate)
		{
			InterpolationDelegate = interpolationDelegate;
		}
	}

	public sealed unsafe class AnimationNetworkController : AnimationNetworkProperty
	{
		// PRIVATE MEMBERS

		private readonly AnimationLayer[]            _layers;
		private readonly AnimationState[]            _states;
		private readonly ClipState[]                 _clipStates;
		private readonly MultiClipState[]            _multiClipStates;
		private readonly BlendTreeState[]            _blendTreeStates;
		private readonly MultiBlendTreeState[]       _multiBlendTreeStates;
		private readonly MultiMirrorBlendTreeState[] _multiMirrorBlendTreeStates;
		private readonly AnimationObjectInfo[]       _properties;

		private static readonly List<int> _filteredIndices = new List<int>(32);

		// CONSTRUCTORS

		public AnimationNetworkController(AnimationController controller) : base(GetWordCount(controller))
		{
			_layers = (AnimationLayer[])controller.Layers;

			List<AnimationState> states = new List<AnimationState>(64);

			for (int i = 0, count = _layers.Length; i < count; ++i)
			{
				AddStates(_layers[i], states);
			}

			_clipStates                 = FilterStates<ClipState>(states).ToArray();
			_multiClipStates            = FilterStates<MultiClipState>(states).ToArray();
			_blendTreeStates            = FilterStates<BlendTreeState>(states).ToArray();
			_multiBlendTreeStates       = FilterStates<MultiBlendTreeState>(states).ToArray();
			_multiMirrorBlendTreeStates = FilterStates<MultiMirrorBlendTreeState>(states).ToArray();

			_states = states.ToArray();

			_properties = GetProperties(controller);
		}

		// AnimationNetworkProperty INTERFACE

		public override void Read(int* ptr)
		{
			float* floatPtr = (float*)ptr;

			AnimationLayer   layer;
			AnimationLayer[] layers = _layers;
			for (int i = 0, count = layers.Length; i < count; ++i)
			{
				layer = layers[i];
				layer.Weight      = *floatPtr; ++floatPtr;
				layer.FadingSpeed = *floatPtr; ++floatPtr;
			}

			AnimationState   state;
			AnimationState[] states = _states;
			for (int i = 0, count = states.Length; i < count; ++i)
			{
				state = states[i];
				state.Weight      = *floatPtr; ++floatPtr;
				state.FadingSpeed = *floatPtr; ++floatPtr;
			}

			ClipState   clipState;
			ClipState[] clipStates = _clipStates;
			for (int i = 0, count = clipStates.Length; i < count; ++i)
			{
				clipState = clipStates[i];
				clipState.Weight        = *floatPtr; ++floatPtr;
				clipState.FadingSpeed   = *floatPtr; ++floatPtr;
				clipState.AnimationTime = *floatPtr; ++floatPtr;
			}

			MultiClipState   multiClipState;
			MultiClipState[] multiClipStates = _multiClipStates;
			for (int i = 0, count = multiClipStates.Length; i < count; ++i)
			{
				multiClipState = multiClipStates[i];
				multiClipState.Weight        = *floatPtr; ++floatPtr;
				multiClipState.FadingSpeed   = *floatPtr; ++floatPtr;
				multiClipState.AnimationTime = *floatPtr; ++floatPtr;
			}

			BlendTreeState   blendTreeState;
			BlendTreeState[] blendTreeStates = _blendTreeStates;
			for (int i = 0, count = blendTreeStates.Length; i < count; ++i)
			{
				blendTreeState = blendTreeStates[i];
				blendTreeState.Weight        = *floatPtr; ++floatPtr;
				blendTreeState.FadingSpeed   = *floatPtr; ++floatPtr;
				blendTreeState.AnimationTime = *floatPtr; ++floatPtr;
			}

			MultiBlendTreeState   multiBlendTreeState;
			MultiBlendTreeState[] multiBlendTreeStates = _multiBlendTreeStates;
			for (int i = 0, count = multiBlendTreeStates.Length; i < count; ++i)
			{
				multiBlendTreeState = multiBlendTreeStates[i];
				multiBlendTreeState.Weight        = *floatPtr; ++floatPtr;
				multiBlendTreeState.FadingSpeed   = *floatPtr; ++floatPtr;
				multiBlendTreeState.AnimationTime = *floatPtr; ++floatPtr;

				float[] weights = multiBlendTreeState.Weights;
				for (int j = 0, weightCount = weights.Length; j < weightCount; ++j)
				{
					weights[j] = *floatPtr; ++floatPtr;
				}
			}

			MultiMirrorBlendTreeState   multiMirrorBlendTreeState;
			MultiMirrorBlendTreeState[] multiMirrorBlendTreeStates = _multiMirrorBlendTreeStates;
			for (int i = 0, count = multiMirrorBlendTreeStates.Length; i < count; ++i)
			{
				multiMirrorBlendTreeState = multiMirrorBlendTreeStates[i];
				multiMirrorBlendTreeState.Weight        = *floatPtr; ++floatPtr;
				multiMirrorBlendTreeState.FadingSpeed   = *floatPtr; ++floatPtr;
				multiMirrorBlendTreeState.AnimationTime = *floatPtr; ++floatPtr;
			}

			ptr = (int*)floatPtr;

			AnimationObjectInfo   property;
			AnimationObjectInfo[] properties = _properties;

			for (int i = 0, count = properties.Length; i < count; ++i)
			{
				property = properties[i];

				byte* objectPtr = (byte*)UnsafeUtility.PinGCObjectAndGetAddress(property.Target, out ulong gcHandle);

				for (int j = 0; j < property.Count; ++j)
				{
					int  wordCount   = property.WordCounts[j];
					int* propertyPtr = (int*)(objectPtr + property.FieldOffsets[j]);

					if (wordCount == 1)
					{
						ReadSingle(ptr, propertyPtr);
					}
					else
					{
						ReadMultiple(ptr, propertyPtr, wordCount);
					}

					ptr += wordCount;
				}

				UnsafeUtility.ReleaseGCObject(gcHandle);
			}
		}

		public override void Write(int* ptr)
		{
			float* floatPtr = (float*)ptr;

			AnimationLayer   layer;
			AnimationLayer[] layers = _layers;
			for (int i = 0, count = layers.Length; i < count; ++i)
			{
				layer = layers[i];
				*floatPtr = layer.Weight;      ++floatPtr;
				*floatPtr = layer.FadingSpeed; ++floatPtr;
			}

			AnimationState   state;
			AnimationState[] states = _states;
			for (int i = 0, count = states.Length; i < count; ++i)
			{
				state = states[i];
				*floatPtr = state.Weight;      ++floatPtr;
				*floatPtr = state.FadingSpeed; ++floatPtr;
			}

			ClipState   clipState;
			ClipState[] clipStates = _clipStates;
			for (int i = 0, count = clipStates.Length; i < count; ++i)
			{
				clipState = clipStates[i];
				*floatPtr = clipState.Weight;        ++floatPtr;
				*floatPtr = clipState.FadingSpeed;   ++floatPtr;
				*floatPtr = clipState.AnimationTime; ++floatPtr;
			}

			MultiClipState   multiClipState;
			MultiClipState[] multiClipStates = _multiClipStates;
			for (int i = 0, count = multiClipStates.Length; i < count; ++i)
			{
				multiClipState = multiClipStates[i];
				*floatPtr = multiClipState.Weight;        ++floatPtr;
				*floatPtr = multiClipState.FadingSpeed;   ++floatPtr;
				*floatPtr = multiClipState.AnimationTime; ++floatPtr;
			}

			BlendTreeState   blendTreeState;
			BlendTreeState[] blendTreeStates = _blendTreeStates;
			for (int i = 0, count = blendTreeStates.Length; i < count; ++i)
			{
				blendTreeState = blendTreeStates[i];
				*floatPtr = blendTreeState.Weight;        ++floatPtr;
				*floatPtr = blendTreeState.FadingSpeed;   ++floatPtr;
				*floatPtr = blendTreeState.AnimationTime; ++floatPtr;
			}

			MultiBlendTreeState   multiBlendTreeState;
			MultiBlendTreeState[] multiBlendTreeStates = _multiBlendTreeStates;
			for (int i = 0, count = multiBlendTreeStates.Length; i < count; ++i)
			{
				multiBlendTreeState = multiBlendTreeStates[i];
				*floatPtr = multiBlendTreeState.Weight;        ++floatPtr;
				*floatPtr = multiBlendTreeState.FadingSpeed;   ++floatPtr;
				*floatPtr = multiBlendTreeState.AnimationTime; ++floatPtr;

				float[] weights = multiBlendTreeState.Weights;
				for (int j = 0, weightCount = weights.Length; j < weightCount; ++j)
				{
					*floatPtr = weights[j]; ++floatPtr;
				}
			}

			MultiMirrorBlendTreeState   multiMirrorBlendTreeState;
			MultiMirrorBlendTreeState[] multiMirrorBlendTreeStates = _multiMirrorBlendTreeStates;
			for (int i = 0, count = multiMirrorBlendTreeStates.Length; i < count; ++i)
			{
				multiMirrorBlendTreeState = multiMirrorBlendTreeStates[i];
				*floatPtr = multiMirrorBlendTreeState.Weight;        ++floatPtr;
				*floatPtr = multiMirrorBlendTreeState.FadingSpeed;   ++floatPtr;
				*floatPtr = multiMirrorBlendTreeState.AnimationTime; ++floatPtr;
			}

			ptr = (int*)floatPtr;

			AnimationObjectInfo   property;
			AnimationObjectInfo[] properties = _properties;

			for (int i = 0, count = properties.Length; i < count; ++i)
			{
				property = properties[i];

				byte* objectPtr = (byte*)UnsafeUtility.PinGCObjectAndGetAddress(property.Target, out ulong gcHandle);

				for (int j = 0; j < property.Count; ++j)
				{
					int  wordCount   = property.WordCounts[j];
					int* propertyPtr = (int*)(objectPtr + property.FieldOffsets[j]);

					if (wordCount == 1)
					{
						WriteSingle(ptr, propertyPtr);
					}
					else
					{
						WriteMultiple(ptr, propertyPtr, wordCount);
					}

					ptr += wordCount;
				}

				UnsafeUtility.ReleaseGCObject(gcHandle);
			}
		}

		public override void Interpolate(InterpolationData interpolationData)
		{
			float* fromFloatPtr = (float*)interpolationData.From;
			float* toFloatPtr   = (float*)interpolationData.To;
			float  alpha        = interpolationData.Alpha;

			AnimationLayer   layer;
			AnimationLayer[] layers = _layers;
			for (int i = 0, count = layers.Length; i < count; ++i)
			{
				layer = layers[i];
				layer.InterpolatedWeight = AnimationUtility.InterpolateWeight(*fromFloatPtr, *toFloatPtr, alpha); fromFloatPtr += 2; toFloatPtr += 2; // Weight + FadingSpeed
			}

			AnimationState   state;
			AnimationState[] states = _states;
			for (int i = 0, count = states.Length; i < count; ++i)
			{
				state = states[i];
				state.InterpolatedWeight = AnimationUtility.InterpolateWeight(*fromFloatPtr, *toFloatPtr, alpha); fromFloatPtr += 2; toFloatPtr += 2; // Weight + FadingSpeed
			}

			ClipState   clipState;
			ClipState[] clipStates = _clipStates;
			for (int i = 0, count = clipStates.Length; i < count; ++i)
			{
				clipState = clipStates[i];
				clipState.InterpolatedWeight        = AnimationUtility.InterpolateWeight(*fromFloatPtr, *toFloatPtr, alpha); fromFloatPtr += 2; toFloatPtr += 2; // Weight + FadingSpeed
				clipState.InterpolatedAnimationTime = AnimationUtility.InterpolateTime(*fromFloatPtr, *toFloatPtr, 1.0f, alpha, clipState.InterpolatedWeight); ++fromFloatPtr; ++toFloatPtr;
			}

			MultiClipState   multiClipState;
			MultiClipState[] multiClipStates = _multiClipStates;
			for (int i = 0, count = multiClipStates.Length; i < count; ++i)
			{
				multiClipState = multiClipStates[i];
				multiClipState.InterpolatedWeight        = AnimationUtility.InterpolateWeight(*fromFloatPtr, *toFloatPtr, alpha); fromFloatPtr += 2; toFloatPtr += 2; // Weight + FadingSpeed
				multiClipState.InterpolatedAnimationTime = AnimationUtility.InterpolateTime(*fromFloatPtr, *toFloatPtr, 1.0f, alpha, multiClipState.InterpolatedWeight); ++fromFloatPtr; ++toFloatPtr;
			}

			BlendTreeState   blendTreeState;
			BlendTreeState[] blendTreeStates = _blendTreeStates;
			for (int i = 0, count = blendTreeStates.Length; i < count; ++i)
			{
				blendTreeState = blendTreeStates[i];
				blendTreeState.InterpolatedWeight        = AnimationUtility.InterpolateWeight(*fromFloatPtr, *toFloatPtr, alpha); fromFloatPtr += 2; toFloatPtr += 2; // Weight + FadingSpeed
				blendTreeState.InterpolatedAnimationTime = AnimationUtility.InterpolateTime(*fromFloatPtr, *toFloatPtr, 1.0f, alpha, blendTreeState.InterpolatedWeight); ++fromFloatPtr; ++toFloatPtr;
			}

			MultiBlendTreeState   multiBlendTreeState;
			MultiBlendTreeState[] multiBlendTreeStates = _multiBlendTreeStates;
			for (int i = 0, count = multiBlendTreeStates.Length; i < count; ++i)
			{
				multiBlendTreeState = multiBlendTreeStates[i];
				multiBlendTreeState.InterpolatedWeight        = AnimationUtility.InterpolateWeight(*fromFloatPtr, *toFloatPtr, alpha); fromFloatPtr += 2; toFloatPtr += 2; // Weight + FadingSpeed
				multiBlendTreeState.InterpolatedAnimationTime = AnimationUtility.InterpolateTime(*fromFloatPtr, *toFloatPtr, 1.0f, alpha, multiBlendTreeState.InterpolatedWeight); ++fromFloatPtr; ++toFloatPtr;

				float[] weights = multiBlendTreeState.InterpolatedWeights;
				for (int j = 0, weightCount = weights.Length; j < weightCount; ++j)
				{
					weights[j] = AnimationUtility.InterpolateWeight(*fromFloatPtr, *toFloatPtr, alpha); ++fromFloatPtr; ++toFloatPtr;
				}
			}

			MultiMirrorBlendTreeState   multiMirrorBlendTreeState;
			MultiMirrorBlendTreeState[] multiMirrorBlendTreeStates = _multiMirrorBlendTreeStates;
			for (int i = 0, count = multiMirrorBlendTreeStates.Length; i < count; ++i)
			{
				multiMirrorBlendTreeState = multiMirrorBlendTreeStates[i];
				multiMirrorBlendTreeState.InterpolatedWeight        = AnimationUtility.InterpolateWeight(*fromFloatPtr, *toFloatPtr, alpha); fromFloatPtr += 2; toFloatPtr += 2; // Weight + FadingSpeed
				multiMirrorBlendTreeState.InterpolatedAnimationTime = AnimationUtility.InterpolateTime(*fromFloatPtr, *toFloatPtr, 1.0f, alpha, multiMirrorBlendTreeState.InterpolatedWeight); ++fromFloatPtr; ++toFloatPtr;
			}

			interpolationData.From = (int*)fromFloatPtr;
			interpolationData.To   = (int*)toFloatPtr;

			AnimationObjectInfo   property;
			AnimationObjectInfo[] properties = _properties;

			for (int i = 0, count = properties.Length; i < count; ++i)
			{
				property = properties[i];

				for (int j = 0; j < property.Count; ++j)
				{
					int wordCount = property.WordCounts[j];

					InterpolationDelegate interpolationDelegate = property.InterpolationDelegates[j];
					if (interpolationDelegate != null)
					{
						interpolationDelegate(interpolationData);
					}

					interpolationData.From += wordCount;
					interpolationData.To   += wordCount;
				}
			}
		}

		// PRIVATE METHODS

		private static int GetWordCount(AnimationController controller)
		{
			int wordCount = GetPropertiesWordCount(controller);

			IList<AnimationLayer> layers = controller.Layers;
			for (int i = 0, count = layers.Count; i < count; ++i)
			{
				wordCount += GetLayerWordCount(layers[i]);
			}

			return wordCount;
		}

		private static int GetLayerWordCount(AnimationLayer layer)
		{
			int wordCount = 2 + GetPropertiesWordCount(layer);

			IList<AnimationState> states = layer.States;
			for (int i = 0, count = states.Count; i < count; ++i)
			{
				wordCount += GetStateWordCount(states[i]);
			}

			return wordCount;
		}

		private static int GetStateWordCount(AnimationState state)
		{
			int wordCount = 2 + GetPropertiesWordCount(state);

			if (state is ClipState)
			{
				wordCount += 1;
			}
			else if (state is MultiClipState)
			{
				wordCount += 1;
			}
			else if (state is BlendTreeState)
			{
				wordCount += 1;
			}
			else if (state is MultiBlendTreeState multiBlendTreeState)
			{
				wordCount += 1 + multiBlendTreeState.Weights.Length;
			}
			else if (state is MultiMirrorBlendTreeState)
			{
				wordCount += 1;
			}

			IList<AnimationState> states = state.States;
			for (int i = 0, count = states.Count; i < count; ++i)
			{
				wordCount += GetStateWordCount(states[i]);
			}

			return wordCount;
		}

		private static int GetPropertiesWordCount(object target)
		{
			int wordCount = 0;

			FieldInfo[] fields = target.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			for (int i = 0; i < fields.Length; ++i)
			{
				FieldInfo field = fields[i];

				object[] attributes = field.GetCustomAttributes(typeof(AnimationPropertyAttribute), false);
				if (attributes.Length > 0)
				{
					if (field.FieldType.IsValueType == false)
					{
						throw new NotSupportedException(field.FieldType.FullName);
					}

					wordCount += GetTypeWordCount(field.FieldType);
				}
			}

			return wordCount;
		}

		private static int GetTypeWordCount(Type type)
		{
			return (Marshal.SizeOf(type) + 3) / 4;
		}

		private static void AddStates(AnimationLayer layer, List<AnimationState> allStates)
		{
			IList<AnimationState> layerStates = layer.States;
			for (int i = 0, count = layerStates.Count; i < count; ++i)
			{
				AnimationState layerState = layerStates[i];

				allStates.Add(layerState);

				AddStates(layerState, allStates);
			}
		}

		private static void AddStates(AnimationState state, List<AnimationState> allStates)
		{
			IList<AnimationState> stateStates = state.States;
			for (int i = 0, count = stateStates.Count; i < count; ++i)
			{
				AnimationState stateState = stateStates[i];

				allStates.Add(stateState);

				AddStates(stateState, allStates);
			}
		}

		private static List<T> FilterStates<T>(List<AnimationState> states) where T : class
		{
			_filteredIndices.Clear();

			List<T> filteredStates = new List<T>(8);

			for (int i = 0, count = states.Count; i < count; ++i)
			{
				if (states[i] is T state)
				{
					filteredStates.Add(state);
					_filteredIndices.Add(i);
				}
			}

			for (int i = _filteredIndices.Count - 1; i >= 0; --i)
			{
				states.RemoveAt(_filteredIndices[i]);
			}

			return filteredStates;
		}

		private static AnimationObjectInfo[] GetProperties(AnimationController controller)
		{
			List<AnimationObjectInfo> properties = new List<AnimationObjectInfo>();

			AddTargetProperties(controller, properties);

			IList<AnimationLayer> layers = controller.Layers;
			for (int i = 0, count = layers.Count; i < count; ++i)
			{
				AddLayerProperties(layers[i], properties);
			}

			return properties.ToArray();
		}

		private static void AddLayerProperties(AnimationLayer layer, List<AnimationObjectInfo> properties)
		{
			AddTargetProperties(layer, properties);

			IList<AnimationState> states = layer.States;
			for (int i = 0, count = states.Count; i < count; ++i)
			{
				AddStateProperties(states[i], properties);
			}
		}

		private static void AddStateProperties(AnimationState state, List<AnimationObjectInfo> properties)
		{
			AddTargetProperties(state, properties);

			IList<AnimationState> states = state.States;
			for (int i = 0, count = states.Count; i < count; ++i)
			{
				AddStateProperties(states[i], properties);
			}
		}

		private static void AddTargetProperties(object target, List<AnimationObjectInfo> properties)
		{
			bool                        hasProperties          = false;
			List<int>                   wordCounts             = default;
			List<int>                   fieldOffsets           = default;
			List<InterpolationDelegate> interpolationDelegates = default;

			FieldInfo[] fields = target.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			for (int i = 0; i < fields.Length; ++i)
			{
				FieldInfo field = fields[i];

				object[] attributes = field.GetCustomAttributes(typeof(AnimationPropertyAttribute), false);
				if (attributes.Length > 0)
				{
					if (field.FieldType.IsValueType == false)
					{
						throw new NotSupportedException(field.FieldType.FullName);
					}

					if (hasProperties == false)
					{
						hasProperties          = true;
						wordCounts             = new List<int>(8);
						fieldOffsets           = new List<int>(8);
						interpolationDelegates = new List<InterpolationDelegate>(8);
					}

					InterpolationDelegate interpolationDelegate = null;

					string interpolationDelegateName = ((AnimationPropertyAttribute)attributes[0]).InterpolationDelegate;
					if (string.IsNullOrEmpty(interpolationDelegateName) == false)
					{
						MethodInfo interpolationMethod = target.GetType().GetMethod(interpolationDelegateName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
						if (interpolationMethod == null)
						{
							throw new ArgumentException($"Missing interpolation method {interpolationDelegateName}!");
						}

						interpolationDelegate = interpolationMethod.CreateDelegate(typeof(InterpolationDelegate), target) as InterpolationDelegate;
						if (interpolationMethod == null)
						{
							throw new ArgumentException($"Couldn't create delegate for interpolation method {interpolationDelegateName}!");
						}
					}

					wordCounts.Add(GetTypeWordCount(field.FieldType));
					fieldOffsets.Add(UnsafeUtility.GetFieldOffset(field));
					interpolationDelegates.Add(interpolationDelegate);
				}
			}

			if (hasProperties == true)
			{
				AnimationObjectInfo animationObject = new AnimationObjectInfo();
				animationObject.Count                  = fieldOffsets.Count;
				animationObject.Target                 = target;
				animationObject.WordCounts             = wordCounts.ToArray();
				animationObject.FieldOffsets           = fieldOffsets.ToArray();
				animationObject.InterpolationDelegates = interpolationDelegates.ToArray();

				properties.Add(animationObject);
			}
		}

		private static void ReadSingle(int* ptr, int* propertyPtr)
		{
			*propertyPtr = *ptr;
		}

		private static void WriteSingle(int* ptr, int* propertyPtr)
		{
			*ptr = *propertyPtr;
		}

		private static void ReadMultiple(int* ptr, int* propertyPtr, int count)
		{
			for (int i = 0; i < count; ++i)
			{
				*propertyPtr = *ptr;

				++ptr;
				++propertyPtr;
			}
		}

		private static void WriteMultiple(int* ptr, int* propertyPtr, int count)
		{
			for (int i = 0; i < count; ++i)
			{
				*ptr = *propertyPtr;

				++ptr;
				++propertyPtr;
			}
		}

		private static int InterpolateState(int from, int to, float alpha)
		{
			return alpha < 0.5f ? from : to;
		}

		private sealed class AnimationObjectInfo
		{
			public int                     Count;
			public object                  Target;
			public int[]                   WordCounts;
			public int[]                   FieldOffsets;
			public InterpolationDelegate[] InterpolationDelegates;
		}
	}
}
