using UnityEngine;
using System;
using ReadyPlayerMe.AvatarLoader;
using Fusion;
using TPSBR;

public class SimpleAvatarLoader : NetworkBehaviour
{
    private Agent _agent;

    [Networked, Capacity(58)]
    public string AvatarUrl { private get; set; }
    [Networked, Capacity(1)]
    public int Gender { private get; set; }

    [Space]
    [Header("RPM Default Avatar")]
    [SerializeField]
    private GameObject baseAvatarObject;
    [SerializeField]
    private Transform baseArmature;
    [SerializeField]
    private Animator baseAnimator;

    [Space]
    [Header("Avatar References")]
    [SerializeField]
    private GameObject masculineAvatarObject;
    [SerializeField]
    private GameObject feminineAvatarObject;
    [SerializeField]
    private Transform references;

    [Space]
    [Header("Renderer")]
    [SerializeField]
    private SkinnedMeshRenderer rendererAvatar;
    [SerializeField]
    private SkinnedMeshRenderer rendererAvatarTransparent;

    [Space]
    [Header("Component Extras")]
    [SerializeField]
    private bool usingEyeAnimation;
    [SerializeField]
    private bool usingVoiceHandler;

    private bool onSetAvatar;
    private string avatarUrlCache;
    private GameObject avatar;
    private AvatarObjectLoader avatarObjectLoader;
    private readonly Vector3 avatarPositionOffset = new(0, 0, 0);

    public void OnSpawned(Agent agent)
    {
        _agent = agent;

        onSetAvatar = true;
    }
    public void OnDespawned()
    {
        // Add something
    }
    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if (!onSetAvatar)
        {
            return;
        }

        if (string.IsNullOrEmpty(AvatarUrl) && Gender > 0)
        {
            SetLocalAvatar(Gender);
        }
        else
        {
            if (AvatarUrl == avatarUrlCache)
            {
                SetRuntimeAvatarCache(avatarUrlCache);
            }
            else
            {
                SetRuntimeAvatar(AvatarUrl);
            }
        }

        onSetAvatar = false;
    }

    private void SetLocalAvatar(int gendertype)
    {
        if (gendertype > 0)
        {
            Gender = gendertype;
            LoadLocalAvatar(Gender);
        }
    }
    private void SetRuntimeAvatar(string avatarurl)
    {
        if (!string.IsNullOrEmpty(avatarurl))
        {
            AvatarUrl = avatarurl;
            LoadAvatar(AvatarUrl);

            avatarUrlCache = AvatarUrl;
        }
    }
    private void SetRuntimeAvatarCache(string avatarurlcache)
    {
        if (!string.IsNullOrEmpty(avatarurlcache))
        {
            LoadAvatarCache();
        }
    }

    #region Runtime Avatar Load
    private void LoadAvatar(string avatarurl)
    {
        avatarObjectLoader = new AvatarObjectLoader
        {
            AvatarConfig = RPMPlayerSetting.Data.avatarConfig
        };

        avatarObjectLoader.OnCompleted += OnLoadCompleted;
        avatarObjectLoader.OnFailed += OnLoadFailed;

        string loadAvatar = avatarurl.Trim(' ');
        avatarObjectLoader.LoadAvatar(loadAvatar);
    }
    private void OnLoadCompleted(object sender, CompletionEventArgs args)
    {
        SetupAvatar(args.Avatar);
    }
    private void OnLoadFailed(object sender, FailureEventArgs args)
    {
        SetupLocalAvatar(Gender);
    }
    #endregion

    #region Local Avatar Load
    private void LoadLocalAvatar(int gendertype)
    {
        SetupLocalAvatar(gendertype);
    }
    private void LoadAvatarCache()
    {
        if (avatar != null)
        {
            SetupAvatarCache(avatar);
        }
    }
    #endregion

    #region Avatar Processed
    private void SetupLocalAvatar(int localAvatar)
    {
        switch (localAvatar)
        {
            case 1:
                SetupMasculineArmature();
                SetupAvatarRenderer(masculineAvatarObject);
                break;

            case 2:
                SetupFeminineArmature();
                SetupAvatarRenderer(feminineAvatarObject);
                break;

            default:
                // Handle any other cases or provide a default behavior
                break;
        }
    }
    private void SetupAvatar(GameObject targetAvatar)
    {
        if (avatar != null)
        {
            Destroy(avatar);
        }

        avatar = targetAvatar;
        avatar.transform.parent = references;
        avatar.transform.SetLocalPositionAndRotation(avatarPositionOffset, Quaternion.Euler(0, 0, 0));
        avatar.SetActive(false);

        OutfitGender gender = avatar.GetComponent<AvatarData>().AvatarMetadata.OutfitGender;

        switch (gender)
        {
            case OutfitGender.Masculine:
                SetupMasculineArmature();
                break;

            case OutfitGender.Feminine:
                SetupFeminineArmature();
                break;

            default:
                // Handle any other cases or provide a default behavior
                break;
        }

        SetupAvatarRenderer(avatar);
    }
    private void SetupAvatarCache(GameObject targetAvatar)
    {
        avatar = targetAvatar;

        OutfitGender gender = avatar.GetComponent<AvatarData>().AvatarMetadata.OutfitGender;

        switch (gender)
        {
            case OutfitGender.Masculine:
                SetupMasculineArmature();
                break;

            case OutfitGender.Feminine:
                SetupFeminineArmature();
                break;

            default:
                // Handle any other cases or provide a default behavior
                break;
        }

        SetupAvatarRenderer(avatar);
    }

    private void SetupMasculineArmature()
    {
        foreach (Transform childTransformBase in baseArmature.GetComponentsInChildren<Transform>())
        {
            string childName = childTransformBase.gameObject.name;
            Transform armatureTransform = masculineAvatarObject.transform.Find("Armature");

            Transform[] matchingChildTransformsMasculine = Array.FindAll(armatureTransform.GetComponentsInChildren<Transform>(), t => t.gameObject.name == childName);

            foreach (Transform matchingChildTransformMasculine in matchingChildTransformsMasculine)
            {
                childTransformBase.SetPositionAndRotation(matchingChildTransformMasculine.position, matchingChildTransformMasculine.rotation);
                childTransformBase.localScale = matchingChildTransformMasculine.localScale;
            }
        }

        baseAnimator.avatar = RPMPlayerSetting.Data.masculineAvatar;
    }
    private void SetupFeminineArmature()
    {
        foreach (Transform childTransformBase in baseArmature.GetComponentsInChildren<Transform>())
        {
            string childName = childTransformBase.gameObject.name;
            Transform armatureTransform = feminineAvatarObject.transform.Find("Armature");

            Transform[] matchingChildTransformsFeminine = Array.FindAll(armatureTransform.GetComponentsInChildren<Transform>(), t => t.gameObject.name == childName);

            foreach (Transform matchingChildTransformFeminine in matchingChildTransformsFeminine)
            {
                childTransformBase.SetPositionAndRotation(matchingChildTransformFeminine.position, matchingChildTransformFeminine.rotation);
                childTransformBase.localScale = matchingChildTransformFeminine.localScale;
            }
        }

        baseAnimator.avatar = RPMPlayerSetting.Data.feminineAvatar;
    }

    private void SetupAvatarRenderer(GameObject referenceAvatarObject)
    {
        rendererAvatarTransparent.gameObject.SetActive(false);

        SkinnedMeshRenderer[] allChildRenderer = referenceAvatarObject.GetComponentsInChildren<SkinnedMeshRenderer>();

        foreach (SkinnedMeshRenderer child in allChildRenderer)
        {
            if (child.gameObject.name == RPMConstant.AVATAR)
            {
                SkinnedMeshRenderer AvatarRenderer = child.gameObject.GetComponent<SkinnedMeshRenderer>();

                rendererAvatar.sharedMaterial = AvatarRenderer.sharedMaterial;
                rendererAvatar.sharedMesh = AvatarRenderer.sharedMesh;
            }

            if (child.gameObject.name == RPMConstant.AVATAR_TRANSPARENT)
            {
                rendererAvatarTransparent.gameObject.SetActive(true);

                SkinnedMeshRenderer AvatarTransparentRenderer = child.gameObject.GetComponent<SkinnedMeshRenderer>();

                rendererAvatarTransparent.sharedMaterial = AvatarTransparentRenderer.sharedMaterial;
                rendererAvatarTransparent.sharedMesh = AvatarTransparentRenderer.sharedMesh;
            }
        }

        SetupExtras();
    }
    private void SetupExtras()
    {
        if (usingEyeAnimation && !baseAvatarObject.TryGetComponent(out EyeAnimationHandler _))
        {
            baseAvatarObject.AddComponent<EyeAnimationHandler>();
        }

        if (usingVoiceHandler && !baseAvatarObject.TryGetComponent(out VoiceHandler _))
        {
            baseAvatarObject.AddComponent<VoiceHandler>();
        }
    }
    #endregion
}