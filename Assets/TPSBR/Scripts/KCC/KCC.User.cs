namespace Fusion.KCC
{
	using UnityEngine;
	using System.Collections.Generic;

	public partial class KCC
	{
		public void SetAim(bool aim)
		{
			if (HasAnyAuthority == false)
				return;

			Data.Aim = aim;
		}

		public void SetRecoil(Vector2 recoil)
		{
			if (HasAnyAuthority == false)
				return;

			KCCData data = _renderData;
			data.Recoil = recoil;

			if (IsInFixedUpdate == true)
			{
				data = _fixedData;
				data.Recoil = recoil;
			}
		}

		partial void InitializeUserNetworkProperties(KCCNetworkContext networkContext, List<IKCCNetworkProperty> networkProperties)
		{
			networkProperties.Add(new KCCNetworkVector2<KCCNetworkContext>(networkContext, 0.0f, (context, value) => context.Data.Recoil     = value, (context) => context.Data.Recoil,     null));
			networkProperties.Add(new KCCNetworkBool   <KCCNetworkContext>(networkContext,       (context, value) => context.Data.IsGrounded = value, (context) => context.Data.IsGrounded, null));
			networkProperties.Add(new KCCNetworkBool   <KCCNetworkContext>(networkContext,       (context, value) => context.Data.Aim        = value, (context) => context.Data.Aim,        null));
		}
	}

	public partial class KCCData
	{
		public bool    Aim;
		public Vector2 Recoil;

		partial void CopyUserDataFromOther(KCCData other)
		{
			Aim    = other.Aim;
			Recoil = other.Recoil;
		}
	}
}
