using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BeatSaberMapRecommender.Models;
using BeatSaberMapRecommender.Services;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using IPA.Utilities.Async;
using SiraUtil.Logging;
using SiraUtil.Web;
using UnityEngine.UI;
using Zenject;

namespace BeatSaberMapRecommender.UI
{
	[ViewDefinition("BeatSaberMapRecommender.UI.Views.MenuView.bsml")]
	[HotReload(RelativePathToLayout = @"Views\MenuView.bsml")]
	public class BSMRMenuViewController : BSMLAutomaticViewController
	{
		private const int NUMBER_OF_CELLS = 6;

		private SiraLog _siraLog = null!;
		private IHttpService _httpService = null!;
		private BeatSaverService _beatSaverService = null!;
		private BSMRService _bsmrService = null!;

		private List<RecommendationMap> _items = new List<RecommendationMap>();

		public event Action<RecommendationMap>? ListItemWasClicked;
		[UIValue("level-name")] protected string LevelName { get; set; } = null!;
		[UIComponent("recommendation-list")] protected readonly CustomListTableData RecommendationList = null!;
		[UIComponent("txt-loading")] protected readonly FormattableText LoadingText = null!;
		[UIComponent("btn-up")] protected readonly Button UpButton = null!;
		[UIComponent("btn-down")] protected readonly Button DownButton = null!;

		private int _page;


		[Inject]
		public void Construct(SiraLog siraLog, IHttpService httpService, BeatSaverService beatSaverService, BSMRService bsmrService)
		{
			_siraLog = siraLog;
			_httpService = httpService;
			_beatSaverService = beatSaverService;
			_bsmrService = bsmrService;
		}


		public async Task SelectLevel(IPreviewBeatmapLevel level)
		{
			LevelName = $"Recommendations for {level.songName}";
			LoadingText.enabled = true;
			_page = 0;
			_items.Clear();

			RecommendationList.data.Clear();
			RecommendationList.tableView.ReloadData();

			await LoadRecommendationItems(level.levelID);

			await UpdateListItems();
		}

		private async Task UpdateListItems()
		{
			var cells = _items.Skip(_page * NUMBER_OF_CELLS).Take(NUMBER_OF_CELLS).ToList();


			await UnityMainThreadTaskScheduler.Factory.StartNew(async () =>
			{
				var cellComponents = await ConvertToCellComponents(cells);

				LoadingText.enabled = false;
				RecommendationList.data.Clear();
				RecommendationList.data.AddRange(cellComponents);
				RecommendationList.tableView.ReloadData();

				//Update buttons
				UpButton.interactable = _page != 0;

				var maxPages = _items.Count / NUMBER_OF_CELLS;
				DownButton.interactable = _page < maxPages;
			});
		}

		private async Task LoadRecommendationItems(string levelId)
		{
			var songInfo = await _beatSaverService.GetSongInfoFromLevelId(levelId);

			if (songInfo == null)
			{
				_siraLog.Error($"Failed to get song info from level id {levelId}");
				return;
			}

			var (songId, difficulty, characteristic) = songInfo.Value;
			var items = await _bsmrService.GetMapRecommendations(songId, difficulty, characteristic);

			_items.Clear();
			_items.AddRange(items.GroupBy(x => x.SongKey).Select(x => x.First()).ToList());
		}

		private async Task<IEnumerable<ListCellComponent>> ConvertToCellComponents(List<RecommendationMap> recommendationMap)
		{
			var listCellComponents = new List<ListCellComponent>(recommendationMap.Count);
			foreach (var recommendation in recommendationMap)
			{
				var sprite = await _bsmrService.LoadSpriteAsync(recommendation.CoverUrl);
				listCellComponents.Add(new ListCellComponent(recommendation.SongKey, recommendation.MapName, recommendation.Mapper, sprite));
			}

			return listCellComponents;
		}


		[UIAction("selected-list-item")]
		public async Task SelectedListItem(TableView _, int index)
		{
			var item = _items[_page * NUMBER_OF_CELLS + index];
			if (item == null)
			{
				return;
			}

			var mapInfo = await _beatSaverService.GetMapInfoFromKey(item.SongKey);
			if (mapInfo == null)
			{
				return;
			}

			var (hash, downloadUrl) = mapInfo.Value;
			if (hash == null)
			{
				return;
			}

			if (!_bsmrService.LevelIsInstalled(hash))
			{
				_siraLog.Info($"Downloading map {item.MapName}");
				// await download map
				hash = await _bsmrService.DownloadLevel($"{item.SongKey} ({item.MapName} - {item.Mapper})", hash, downloadUrl);
				if (hash == null)
				{
					_siraLog.Error($"Error while downloading map {item.MapName}");
					return;
				}

				_siraLog.Info($"Successfully downloaded map {item.MapName}");
			}

			var level = _bsmrService.TryGetLevel(hash);
			if (level == null)
			{
				return;
			}

			item.Level = level;

			ListItemWasClicked?.Invoke(item);

			_siraLog.Info($"Selected list item {index}");
		}

		[UIAction("page-down")]
		public async Task PageDown()
		{
			var maxPages = _items.Count / NUMBER_OF_CELLS;
			if (_page < maxPages)
			{
				_page++;
				await UpdateListItems();
			}
		}

		[UIAction("page-up")]
		public async Task PageUp()
		{
			if (_page > 0)
			{
				_page--;
				await UpdateListItems();
			}
		}
	}
}