namespace TPSBR.UI
{
	using TMPro;

	public class UIJokeDialogView : UIButtonDialogView
	{
		// PUBLIC MEMBERS

		public UIButton        JokeButton01;
		public TextMeshProUGUI JokeButton01Text;
		public UIButton        JokeButton02;
		public TextMeshProUGUI JokeButton02Text;

		// PUBLIC METHODS

		public void OnJokeButtonHover(int index)
		{
			JokeButton01.SetActive(index != 0);
			JokeButton02.SetActive(index != 1);
		}

		// UIView INTERFACE

		protected override void OnInitialize()
		{
			base.OnInitialize();

			OnJokeButtonHover(0);
		}
	}
}
