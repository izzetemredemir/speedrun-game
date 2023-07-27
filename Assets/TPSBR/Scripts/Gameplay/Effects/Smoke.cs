using Fusion;
using UnityEngine;

namespace TPSBR
{
	public class Smoke : NetworkBehaviour
	{
		// PRIVATE MEMBERS

		[Networked]
		private NetworkBool _finished { get; set; }

		[SerializeField]
		private float      _radius;
		[SerializeField]
		private float      _duration;

		[SerializeField]
		private float      _despawnDelay;
		[SerializeField]
		private ParticleSystem _effect;

		private TickTimer _timer;

		// NetworkBehaviour INTERFACE

		public override void CopyBackingFieldsToState(bool firstTime)
		{
			InvokeWeavedCode();

			_effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
		}

		public override void Spawned()
		{
			if (Object.HasStateAuthority == true)
			{
				_timer = TickTimer.CreateFromSeconds(Runner, _duration);
				_finished = false;
			}
		}

		public override void FixedUpdateNetwork()
		{
			if (Object.HasStateAuthority == false)
				return;

			if (_timer.Expired(Runner) == true)
			{
				if (_finished == false)
				{
					_finished = true;
					_timer = TickTimer.CreateFromSeconds(Runner, _despawnDelay);
				}
				else
				{
					Runner.Despawn(Object);
				}
			}
		}

		public override void Render()
		{
			if (_finished == false && _effect.isPlaying == false)
			{
				_effect.transform.localScale = Vector3.one * _radius * 2f;
				_effect.Play(true);
			}
			else if (_finished == true && _effect.isPlaying == true)
			{
				_effect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
			}
		}
	}
}
