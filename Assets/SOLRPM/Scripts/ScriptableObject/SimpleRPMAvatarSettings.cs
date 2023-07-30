using UnityEngine;
using ReadyPlayerMe.AvatarLoader;

[CreateAssetMenu(fileName = "SimpleRPMAvatarSettings", menuName = "Ruals/SimpleRPMAvatarSettings")]
public class SimpleRPMAvatarSettings : ScriptableObject
{
    public AvatarConfig avatarConfig;
    public RuntimeAnimatorController masculineAnimatorController;
    public Avatar masculineAvatar;
    public RuntimeAnimatorController feminineAnimatorController;
    public Avatar feminineAvatar;

    public int runtimeAvatarUrlLength = 58;
    public string validationStart = "https://";
    public string validationContain = "models.readyplayer.me/";
    public string validationEnd = ".glb";

    public string openUrl = "https://readyplayer.me/hub";
}
