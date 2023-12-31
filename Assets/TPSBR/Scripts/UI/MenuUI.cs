using UnityEngine;

namespace TPSBR.UI
{
    public class MenuUI : SceneUI
    {
        // SceneUI INTERFACE

        // EDITED
        [SerializeField]
        private GameObject _avatarLoader;
        // END

        protected override void OnInitializeInternal()
        {
            base.OnInitializeInternal();

            Context.Input.RequestCursorVisibility(true, ECursorStateSource.Menu);

            // EDITED
            //if (Context.PlayerData.Nickname.HasValue() == false)
            //{
            //    var changeNicknameView = Open<UIChangeNicknameView>();
            //    changeNicknameView.SetData("ENTER NICKNAME", true);
            //}

            if (Context.PlayerData.Nickname.HasValue() == false || Context.PlayerData.Gender == 0)
            {
                var changeNicknameView = Open<UIChangeNicknameView>();
                changeNicknameView.SetData("ENTER NICKNAME", true);
            }

            if (Context.PlayerData.AvatarUrl.HasValue() == true)
            {
                _avatarLoader.GetComponent<SimpleAvatarLoaderMenu>().OnSetRuntimeAvatar(Context.PlayerData.AvatarUrl);
            }
            else
            {
			    _avatarLoader.GetComponent<SimpleAvatarLoaderMenu>().OnSetGender(Context.PlayerData.Gender);
            }
            // END
        }

        protected override void OnDeinitializeInternal()
        {
            Context.Input.RequestCursorVisibility(false, ECursorStateSource.Menu);

            base.OnDeinitializeInternal();
        }

        protected override void OnActivate()
        {
            base.OnActivate();

            if (Global.Networking.ErrorStatus.HasValue() == true)
            {
                Open<UIMultiplayerView>();
                var errorDialog = Open<UIErrorDialogView>();

                errorDialog.Title.text = "Connection Issue";

                if (Global.Networking.ErrorStatus == Networking.STATUS_SERVER_CLOSED)
                {
                    errorDialog.Description.text = $"Server was closed.";
                }
                else
                {
                    errorDialog.Description.text = $"Failed to start network game\n\nReason:\n{Global.Networking.ErrorStatus}";
                }

                Global.Networking.ClearErrorStatus();
            }
        }
    }
}
