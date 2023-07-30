using UnityEngine;
using ReadyPlayerMe.AvatarLoader;

namespace TPSBR.UI
{
    public class SimpleAvatarLoaderMenu : UIBehaviour
    {
        [Header("Avatar Loader Settings")]
        [SerializeField]
        private UIBehaviour _refreshingAvatarGroup;
        [Space]
        [SerializeField]
        [Tooltip("Preview avatar to display until avatar loads. Will be inactive after new avatar is loaded")]
        private GameObject previewAvatar;
        [SerializeField]
        private GameObject masculinePreview;
        [SerializeField]
        private GameObject femininePreview;

        [Space]
        [Header("Component Extras")]
        [SerializeField]
        [Tooltip("Using EyeAnimation?")]
        private bool usingEyeAnimation;
        [SerializeField]
        [Tooltip("Using Voice Handler?")]
        private bool usingVoiceHandler;

        private GameObject avatar;
        private AvatarObjectLoader avatarObjectLoader;
        private readonly Vector3 avatarPositionOffset = new(0, 0.27f, 0.27f);

        private string avatarUrl;
        private bool onSetRuntimeAvatar;

        private int userGender;
        private bool onSetGender;

        private void Start()
        {
            avatarObjectLoader = new AvatarObjectLoader();
            avatarObjectLoader.OnCompleted += OnLoadCompleted;
            avatarObjectLoader.OnFailed += OnLoadFailed;
        }
        private void Update()
        {
            if (!onSetRuntimeAvatar || avatarUrl == null || onSetGender)
            {
                return;
            }

            LoadAvatar(avatarUrl);
            onSetRuntimeAvatar = false;
        }

        public void OnSetRuntimeAvatar(string avatarurl)
        {
            SetRuntimeAvatar(avatarurl);
        }
        private void SetRuntimeAvatar(string avatarurl)
        {
            if (!string.IsNullOrEmpty(avatarurl))
            {
                onSetRuntimeAvatar = true;
                avatarUrl = avatarurl;
            }
        }

        public void OnSetGender(int genderType)
        {
            SetGender(genderType);
        }
        private void SetGender(int genderType)
        {
            if (genderType > 0)
            {
                onSetGender = true;
                userGender = genderType;

                SetInitialGender(userGender);
            }
        }

        private void LoadAvatar(string avatarurl)
        {
            _refreshingAvatarGroup.SetActive(true);

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
            if (previewAvatar != null)
            {
                previewAvatar.SetActive(false);
            }
            SetupAvatar(args.Avatar);
        }
        private void OnLoadFailed(object sender, FailureEventArgs args)
        {
            _refreshingAvatarGroup.SetActive(false);

            SetInitialGender(userGender);
        }

        private void SetInitialGender(int initialGender)
        {
            switch (initialGender)
            {
                case 1:
                    masculinePreview.SetActive(true);
                    femininePreview.SetActive(false);
                    break;

                case 2:
                    masculinePreview.SetActive(false);
                    femininePreview.SetActive(true);
                    break;

                default:
                    // Handle any other cases or provide a default behavior
                    break;
            }

            onSetGender = false;
        }
        private void SetupAvatar(GameObject targetAvatar)
        {
            if (avatar != null)
            {
                Destroy(avatar);
            }

            avatar = targetAvatar;
            avatar.transform.parent = transform;
            avatar.transform.SetLocalPositionAndRotation(avatarPositionOffset, Quaternion.Euler(0, 0, 0));

            var animator = avatar.GetComponent<Animator>();

            OutfitGender gender = avatar.GetComponent<AvatarData>().AvatarMetadata.OutfitGender;

            if (gender == OutfitGender.Masculine)
            {
                animator.runtimeAnimatorController = RPMPlayerSetting.Data.masculineAnimatorController;
                animator.avatar = RPMPlayerSetting.Data.masculineAvatar;
            }
            else
            {
                animator.runtimeAnimatorController = RPMPlayerSetting.Data.feminineAnimatorController;
                animator.avatar = RPMPlayerSetting.Data.feminineAvatar;
            }

            animator.applyRootMotion = false;

            if (usingEyeAnimation)
            {
                avatar.AddComponent<EyeAnimationHandler>();
            }

            if (usingVoiceHandler)
            {
                avatar.AddComponent<VoiceHandler>();
            }

            _refreshingAvatarGroup.SetActive(false);
        }
    }
}
