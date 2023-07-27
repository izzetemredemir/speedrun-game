using UnityEngine;
using UnityEngine.Audio;

namespace TPSBR
{
	public class SceneAudio : SceneService 
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private AudioMixer _masterMixer;

		// PUBLIC METHODS

		public void UpdateVolume()
		{
			if (_masterMixer == null)
				return;

			_masterMixer.SetFloat("MusicVolume", Mathf.Log10(Context.RuntimeSettings.MusicVolume) * 20);
			_masterMixer.SetFloat("EffectsVolume", Mathf.Log10(Context.RuntimeSettings.EffectsVolume) * 20);
		}

		// GameService INTERFACE

		protected override void OnActivate()
		{
			base.OnActivate();

			UpdateVolume();
		}
	}
}
