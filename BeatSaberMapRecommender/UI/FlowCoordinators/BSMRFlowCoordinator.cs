using System.Threading.Tasks;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Components;
using HMUI;
using IPA.Utilities;
using SiraUtil.Logging;
using Zenject;

namespace BeatSaberMapRecommender.UI.FlowCoordinators
{
	internal class BSMRFlowCoordinator : FlowCoordinator, IInitializable
	{
		public void Initialize() { }
		private SiraLog _siraLog = null!;

		private LevelDetailButtonViewController _levelDetailButtonViewController = null!;
		private FlowCoordinator _parentFlowCoordinator = null!;
		private MainFlowCoordinator _mainFlowCoordinator = null!;
		private BSMRMenuViewController _bsmrMenuViewController = null!;

		[Inject]
		protected void Construct(SiraLog siraLog, LevelDetailButtonViewController levelDetailButtonViewController, MainFlowCoordinator mainFlowCoordinator, BSMRMenuViewController bsmrMenuViewController)
		{
			_siraLog = siraLog;
			_levelDetailButtonViewController = levelDetailButtonViewController;
			_mainFlowCoordinator = mainFlowCoordinator;
			_bsmrMenuViewController = bsmrMenuViewController;
		}

		protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
		{
			if (firstActivation)
			{
				SetTitle("Beat Saver Map Recommender");
				showBackButton = true;
			}

			if (addedToHierarchy)
			{
				ProvideInitialViewControllers(_bsmrMenuViewController);
			}
		}

		protected void Start()
		{
			_levelDetailButtonViewController.WasClicked += ButtonWasClicked;
		}

		protected void OnDestroy()
		{
			_levelDetailButtonViewController.WasClicked -= ButtonWasClicked;
		}

		private void ButtonWasClicked(IPreviewBeatmapLevel level)
		{
			_parentFlowCoordinator = _mainFlowCoordinator.YoungestChildFlowCoordinatorOrSelf();
			_parentFlowCoordinator.PresentFlowCoordinator(this, animationDirection: ViewController.AnimationDirection.Vertical);

			Task.Run(async () =>
			{
				await _bsmrMenuViewController.SelectLevel(level);
			});
		}

		protected override void BackButtonWasPressed(ViewController topViewController)
		{
			_parentFlowCoordinator.DismissFlowCoordinator(this);
		}
	}
}