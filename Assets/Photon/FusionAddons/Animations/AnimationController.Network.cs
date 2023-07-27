namespace Fusion.Animations
{
	using System.Collections.Generic;

	public abstract unsafe partial class AnimationController
	{
		// PRIVATE MEMBERS

		private AnimationNetworkProperty[] _networkProperties;

		// PRIVATE METHODS

		private int GetNetworkDataWordCount()
		{
			int wordCount = 0;

			for (int i = 0, count = _networkProperties.Length; i < count; ++i)
			{
				wordCount += _networkProperties[i].WordCount;
			}

			return wordCount;
		}

		private unsafe void ReadNetworkData()
		{
			int* ptr = Ptr;

			AnimationNetworkProperty   networkProperty;
			AnimationNetworkProperty[] networkProperties = _networkProperties;
			for (int i = 0, count = networkProperties.Length; i < count; ++i)
			{
				networkProperty = networkProperties[i];
				networkProperty.Read(ptr);
				ptr += networkProperty.WordCount;
			}
		}

		private unsafe void WriteNetworkData()
		{
			int* ptr = Ptr;

			AnimationNetworkProperty   networkProperty;
			AnimationNetworkProperty[] networkProperties = _networkProperties;
			for (int i = 0, count = networkProperties.Length; i < count; ++i)
			{
				networkProperty = networkProperties[i];
				networkProperty.Write(ptr);
				ptr += networkProperty.WordCount;
			}
		}

		private unsafe void InterpolateNetworkData()
		{
			if (GetInterpolationData(out InterpolationData interpolationData) == false)
				return;

			AnimationNetworkProperty   networkProperty;
			AnimationNetworkProperty[] networkProperties = _networkProperties;
			for (int i = 0, count = networkProperties.Length; i < count; ++i)
			{
				networkProperty = networkProperties[i];
				networkProperty.Interpolate(interpolationData);
				interpolationData.From += networkProperty.WordCount;
				interpolationData.To   += networkProperty.WordCount;
			}
		}

		protected virtual void OnInitializeNetworkProperties(List<AnimationNetworkProperty> networkProperties) {}

		// PRIVATE METHODS

		private void InitializeNetworkProperties()
		{
			if (_networkProperties != null)
				return;

			List<AnimationNetworkProperty> networkProperties = new List<AnimationNetworkProperty>(32);

			networkProperties.Add(new AnimationNetworkController(this));

			AnimationLayer[] layers = _layers;
			for (int i = 0, count = layers.Length; i < count; ++i)
			{
				AnimationLayer layer = layers[i];
				layer.InitializeNetworkProperties(this, networkProperties);
			}

			OnInitializeNetworkProperties(networkProperties);

			_networkProperties = networkProperties.ToArray();
		}

		private void DeinitializeNetworkProperties()
		{
			_networkProperties = null;
		}
	}
}
