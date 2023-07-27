namespace TPSBR
{
	using System.Collections;
	using TPSBR.UI;

	public class Menu : Scene
	{
		protected override IEnumerator OnActivate()
		{
			yield return base.OnActivate();

			if (ApplicationSettings.IsQuickPlay == true)
			{
				UIMultiplayerView multiplayerView = Context.UI.Open<UIMultiplayerView>();
				multiplayerView.StartQuickPlay();
			}
		}
	}
}
