using System;
using System.Reflection;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using SiraUtil.Logging;
using Zenject;

namespace BeatSaberMapRecommender.UI
{
	[HotReload(RelativePathToLayout = "")]
	public class LevelDetailButtonViewController : IInitializable
	{
		private readonly StandardLevelDetailViewController _standardLevelDetailViewController;
		private readonly SiraLog _siraLog;
		public event Action<IPreviewBeatmapLevel>? WasClicked;

		public LevelDetailButtonViewController(StandardLevelDetailViewController standardLevelDetailViewController, SiraLog siraLog)
		{
			_standardLevelDetailViewController = standardLevelDetailViewController;
			_siraLog = siraLog;
		}

		public void Initialize()
		{
			BSMLParser.instance.Parse(Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "BeatSaberMapRecommender.UI.Views.LevelDetailButtonsView.bsml"),
				_standardLevelDetailViewController.transform.Find("LevelDetail").gameObject, this);
		}

		[UIAction("toggle-recommender")]
		public void ToggleRecommender()
		{
			var level = _standardLevelDetailViewController.beatmapLevel;
			_siraLog.Info($"Button was clicked! name: {level.songName}");
			WasClicked?.Invoke(level);
		}

		[UIValue("highlight-button-hover")]
		private string HighlightButtonHover => "Open Map Recommender";
	}
}