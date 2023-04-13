using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BeatSaberMapRecommender.Models;
using BeatSaberMapRecommender.Services;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using IPA.Utilities.Async;
using SiraUtil.Logging;
using SiraUtil.Web;
using TMPro;
using UnityEngine;
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

		public event Action<RecommendationMap>? PlayRecommendationWasClicked;
		[UIValue("level-name")] protected string LevelName { get; set; } = null!;
		[UIComponent("recommendation-list")] protected readonly CustomListTableData RecommendationList = null!;
		[UIComponent("btn-up")] protected readonly Button UpButton = null!;
		[UIComponent("btn-down")] protected readonly Button DownButton = null!;

		[UIComponent("img-map-logo")] protected readonly ImageView MapLogoImage = null!;
		[UIComponent("txt-map-name")] protected readonly CurvedTextMeshPro MapNameText = null!;
		[UIComponent("txt-map-author")] protected readonly CurvedTextMeshPro MapAuthorText = null!;
		[UIComponent("txt-map-description")] protected readonly CurvedTextMeshPro MapDescriptionText = null!;
		[UIComponent("txt-tags")] protected readonly CurvedTextMeshPro TagsText = null!;
		[UIComponent("txt-meta-data")] protected readonly CurvedTextMeshPro MetaDataText = null!;
		[UIComponent("btn-play")] protected readonly Button PlayButton = null!;
		[UIComponent("btn-sort-total")] protected readonly Button SortTotalButton = null!;
		[UIComponent("btn-sort-tag")] protected readonly Button SortTagButton = null!;
		[UIComponent("btn-sort-meta")] protected readonly Button SortMetaButton = null!;

		private int _page;
		private List<RecommendationMap> _items = new List<RecommendationMap>();
		private RecommendationMap? _selectedItem;

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
			_page = 0;
			_items.Clear();

			SortTotalButton.interactable = false;
			SortTagButton.interactable = true;
			SortMetaButton.interactable = true;

			RecommendationList.data.Clear();
			RecommendationList.tableView.ReloadData();

			await LoadRecommendationItems(level.levelID);

			await UpdateListItems();

			await LoadDetails();
		}

		private async Task UpdateListItems()
		{
			var cells = _items.Skip(_page * NUMBER_OF_CELLS).Take(NUMBER_OF_CELLS).ToList();


			await UnityMainThreadTaskScheduler.Factory.StartNew(async () =>
			{
				var cellComponents = await ConvertToCellComponents(cells);

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
			_selectedItem = _items[_page * NUMBER_OF_CELLS + index];
			await LoadDetails();
		}

		private async Task LoadDetails()
		{
			PlayButton.enabled = false;
			PlayButton.SetButtonText("Play");
			MapNameText.text = string.Empty;
			MapAuthorText.text = string.Empty;
			MapDescriptionText.text = string.Empty;
			TagsText.text = string.Empty;
			MetaDataText.text = string.Empty;

			if (_selectedItem == null)
			{
				return;
			}

			var beatMap = await _beatSaverService.GetBeatMapFromKey(_selectedItem.SongKey);
			if (beatMap == null)
			{
				return;
			}

			PlayButton.interactable = true;
			PlayButton.enabled = true;
			MapLogoImage.sprite = await _bsmrService.LoadSpriteAsync(beatMap.LatestVersion.CoverURL);
			MapNameText.text = beatMap.Metadata.SongName;
			MapAuthorText.text = beatMap.Metadata.LevelAuthorName;
			MapDescriptionText.text = beatMap.Description;
			MapDescriptionText.overflowMode = TextOverflowModes.Truncate;
			TagsText.text = "Tags : " + string.Join(", ", beatMap.Tags);
			MetaDataText.text =
				$"Similarity: {_selectedItem.TotalSim!*100:f1}% (T: {_selectedItem.TagSim!*100:f1}% M: {_selectedItem.MetaSim!*100:f1}%)";
			// MetaDataText.text = $"Test";
		}

		[UIAction("page-down")]
		public async Task OnPageDownClick()
		{
			var maxPages = _items.Count / NUMBER_OF_CELLS;
			if (_page < maxPages)
			{
				_page++;
				await UpdateListItems();
			}
		}

		[UIAction("page-up")]
		public async Task OnPageUpClick()
		{
			if (_page > 0)
			{
				_page--;
				await UpdateListItems();
			}
		}

		[UIAction("play")]
		public async Task OnPlayClick()
		{
			if (_selectedItem == null)
			{
				return;
			}

			var beatMap = await _beatSaverService.GetBeatMapFromKey(_selectedItem.SongKey);
			if (beatMap == null)
			{
				return;
			}

			var hash = beatMap.LatestVersion.Hash;

			if (!_bsmrService.LevelIsInstalled(hash))
			{
				PlayButton.interactable = false;
				PlayButton.SetButtonText("Downloading...");
				_siraLog.Info($"Downloading map {_selectedItem.MapName}");

				var downloadUrl = beatMap.LatestVersion.DownloadURL;
				hash = await _bsmrService.DownloadLevel($"{_selectedItem.SongKey} ({_selectedItem.MapName} - {_selectedItem.Mapper})", hash, downloadUrl);
				if (hash == null)
				{
					_siraLog.Error($"Error while downloading map {_selectedItem.MapName}");
					return;
				}

				_siraLog.Info($"Successfully downloaded map {_selectedItem.MapName}");
				PlayButton.interactable = true;
				PlayButton.SetButtonText("Downloaded!");
			}

			var level = _bsmrService.TryGetLevel(hash);
			if (level == null)
			{
				return;
			}

			_selectedItem.Level = level;

			PlayRecommendationWasClicked?.Invoke(_selectedItem);
		}

		[UIAction("sort-total")]
		public async Task OnSortTotalClick()
		{
			_page = 0;
			_items = _items.OrderByDescending(x => x.TotalSim).ToList();
			await UpdateListItems();
			SortTotalButton.interactable = false;
			SortTagButton.interactable = true;
			SortMetaButton.interactable = true;
		}

		[UIAction("sort-tag")]
		public async Task OnSortTagClick()
		{
			_page = 0;
			_items = _items.OrderByDescending(x => x.TagSim).ToList();
			await UpdateListItems();
			SortTotalButton.interactable = true;
			SortTagButton.interactable = false;
			SortMetaButton.interactable = true;
		}

		[UIAction("sort-meta")]
		public async Task OnSortMetaClick()
		{
			_page = 0;
			_items = _items.OrderByDescending(x => x.MetaSim).ToList();
			await UpdateListItems();
			SortTotalButton.interactable = true;
			SortTagButton.interactable = true;
			SortMetaButton.interactable = false;
		}
	}
}