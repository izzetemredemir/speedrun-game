using UnityEngine;

public static class RPMPlayerSetting
{
    private static SimpleRPMAvatarSettings rpmAvatarSettings;
    public static SimpleRPMAvatarSettings Data
    {
        get
        {
            if (rpmAvatarSettings == null)
            {
                rpmAvatarSettings = Resources.Load<SimpleRPMAvatarSettings>("SimpleRPMAvatarSettings");
            }
            return rpmAvatarSettings;
        }
    }
}