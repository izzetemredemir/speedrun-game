namespace Fusion.Animations
{
	using UnityEngine;

	public interface IAnimationConvertor
	{
		public void Convert(AnimationConvertor convertor);
	}

	public partial class AnimationConvertor : MonoBehaviour
	{
		public AnimationController Controller;
		public AnimationClip[]     Clips;
		public bool                Log;

		public void ConvertClips()
		{
			if (Controller == null)
				return;

			foreach (IAnimationConvertor clipConvertor in Controller.GetComponentsInChildren<IAnimationConvertor>())
			{
				clipConvertor.Convert(this);
			}
		}

		public bool Convert(AnimationClip clip, out AnimationClip convertedClip)
		{
			if (clip == null)
			{
				convertedClip = null;
				return true;
			}

			string clipName = clip.name;

			for (int i = 0; i < Clips.Length; ++i)
			{
				AnimationClip targetClip = Clips[i];
				if (targetClip.name == clipName)
				{
					convertedClip = targetClip;
					Debug.Log($"Converted {clip.name}");
					return true;
				}
			}

			if (Log == true)
			{
				Debug.LogWarning($"Conversion failed for {clip.name}");
			}

			convertedClip = null;
			return false;
		}
	}
}
