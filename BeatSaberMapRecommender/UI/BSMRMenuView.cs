using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;

namespace BeatSaberMapRecommender.UI
{
	[ViewDefinition("BeatSaberMapRecommender.UI.Views.MenuView.bsml")]
	[HotReload(RelativePathToLayout = "")]
	public class BSMRMenuView : BSMLAutomaticViewController
	{
		// Notify property changes by Fody
		[UIValue("level-name")]
		public string LevelName { get; set; } = null!;

	}
}