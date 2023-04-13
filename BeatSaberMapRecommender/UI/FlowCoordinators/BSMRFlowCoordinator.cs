using System.Threading.Tasks;
using BeatSaberMapRecommender.Models;
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
		private LevelSearchViewController _levelSearchViewController = null!;
		private SelectLevelCategoryViewController _selectLevelCategoryViewController = null!;
		private LevelFilteringNavigationController _levelFilteringNavigationController = null!;
		private LevelCollectionNavigationController _levelCollectionNavigationController = null!;

		private static readonly FieldAccessor<SelectLevelCategoryViewController, IconSegmentedControl>.Accessor SegmentedControl =
			FieldAccessor<SelectLevelCategoryViewController, IconSegmentedControl>.GetAccessor("_levelFilterCategoryIconSegmentedControl");

		private static readonly FieldAccessor<SelectLevelCategoryViewController, SelectLevelCategoryViewController.LevelCategoryInfo[]>.Accessor Categories =
			FieldAccessor<SelectLevelCategoryViewController, SelectLevelCategoryViewController.LevelCategoryInfo[]>.GetAccessor("_levelCategoryInfos");

		[Inject]
		protected void Construct(SiraLog siraLog, LevelDetailButtonViewController levelDetailButtonViewController, MainFlowCoordinator mainFlowCoordinator,
			BSMRMenuViewController bsmrMenuViewController, LevelSearchViewController levelSearchViewController,
			SelectLevelCategoryViewController selectLevelCategoryViewController, LevelFilteringNavigationController levelFilteringNavigationController,
			LevelCollectionNavigationController levelCollectionNavigationController)
		{
			_siraLog = siraLog;
			_levelDetailButtonViewController = levelDetailButtonViewController;
			_mainFlowCoordinator = mainFlowCoordinator;
			_bsmrMenuViewController = bsmrMenuViewController;
			_levelSearchViewController = levelSearchViewController;
			_selectLevelCategoryViewController = selectLevelCategoryViewController;
			_levelFilteringNavigationController = levelFilteringNavigationController;
			_levelCollectionNavigationController = levelCollectionNavigationController;
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
			_bsmrMenuViewController.PlayRecommendationWasClicked += PlayRecommendationWasClicked;
		}

		protected void OnDestroy()
		{
			_levelDetailButtonViewController.WasClicked -= ButtonWasClicked;
			_bsmrMenuViewController.PlayRecommendationWasClicked -= PlayRecommendationWasClicked;
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

		private void PlayRecommendationWasClicked(RecommendationMap map)
		{
			// Credit: Auros
			_parentFlowCoordinator.DismissFlowCoordinator(this, () =>
			{
				var categories = Categories(ref _selectLevelCategoryViewController);
				for (int i = 0; i < categories.Length; i++)
				{
					if (categories[i].levelCategory != SelectLevelCategoryViewController.LevelCategory.All)
					{
						continue;
					}

					var control = SegmentedControl(ref _selectLevelCategoryViewController);
					control.SelectCellWithNumber(i);

					_levelSearchViewController.ResetCurrentFilterParams();
					_levelFilteringNavigationController.UpdateSecondChildControllerContent(SelectLevelCategoryViewController.LevelCategory.All);
					_levelCollectionNavigationController.SelectLevel(map.Level);
					break;
				}
			});
		}

		protected override void BackButtonWasPressed(ViewController topViewController)
		{
			_parentFlowCoordinator.DismissFlowCoordinator(this);
		}
	}
}