using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BeatSaberMapRecommender.Models;
using BeatSaberMapRecommender.Services;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using IPA.Utilities.Async;
using Newtonsoft.Json;
using SiraUtil.Logging;
using SiraUtil.Web;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Logger = IPA.Logging.Logger;

namespace BeatSaberMapRecommender.UI
{
	[ViewDefinition("BeatSaberMapRecommender.UI.Views.MenuView.bsml")]
	[HotReload(RelativePathToLayout = @"Views\MenuView.bsml")]
	public class BSMRMenuViewController : BSMLAutomaticViewController
	{
		private const string RECOMMENDATION_URL = "https://api-beatsavermaprecommender.belgianbeatsaber.community";
		private const int NUMBER_OF_CELLS = 6;

		private SiraLog _siraLog = null!;
		private IHttpService _httpService = null!;
		private JsonSerializer _jsonSerializer = null!;
		private BeatSaverService _beatSaverService = null!;
		private BeatmapLevelsModel _beatMapLevelsModel = null!;

		private List<RecommendationMap> _items = new List<RecommendationMap>();
		private readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

		public event Action<RecommendationMap>? ListItemWasClicked;
		[UIValue("level-name")] protected string LevelName { get; set; } = null!;
		[UIComponent("recommendation-list")] protected readonly CustomListTableData RecommendationList = null!;
		[UIComponent("txt-loading")] protected readonly FormattableText LoadingText = null!;
		[UIComponent("btn-up")] protected readonly Button UpButton = null!;
		[UIComponent("btn-down")] protected readonly Button DownButton = null!;

		private int _page;


		[Inject]
		public void Construct(SiraLog siraLog, IHttpService httpService, BeatSaverService beatSaverService, BeatmapLevelsModel beatmapLevelsModel)
		{
			_siraLog = siraLog;
			_httpService = httpService;
			_jsonSerializer = JsonSerializer.CreateDefault();
			_beatSaverService = beatSaverService;
			_beatMapLevelsModel = beatmapLevelsModel;
		}


		public async Task SelectLevel(IPreviewBeatmapLevel level)
		{
			LevelName = $"Recommendations for {level.songName}";
			LoadingText.enabled = true;
			_page = 0;
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

			//TODO: move to service!!! qyuickk! don't telleris
			var request = await _httpService.GetAsync(
				$"{RECOMMENDATION_URL}/recommendation?song_id={songId}&difficulty={difficulty}&characteristic={characteristic}&n_recommendations=60&n_best_tags=4");

			if (!request.Successful)
			{
				_siraLog.Error("Failed to get recommendations");
				return;
			}

			using var response = await request.ReadAsStreamAsync();
			using var reader = new StreamReader(response);
			using var jsonReader = new JsonTextReader(reader);

			foreach (var item in _jsonSerializer!.Deserialize<List<RecommendationMapDto>>(jsonReader)!)
			{
				var recommendationMap = new RecommendationMap(item);
				_items.Add(recommendationMap);
			}

			_items = _items.GroupBy(x => x.SongKey).Select(x => x.First()).ToList();
		}

		// Credit: Auros
		public IPreviewBeatmapLevel? TryGetLevel(string hash, bool wip = false)
		{
			var cleanerHash = $"custom_level_{hash.ToUpper()}";
			return _beatMapLevelsModel.allLoadedBeatmapLevelPackCollection.beatmapLevelPacks.SelectMany(bm => bm.beatmapLevelCollection.beatmapLevels)
				.FirstOrDefault(lvl => wip ? lvl.levelID.StartsWith(cleanerHash) : lvl.levelID == cleanerHash);
		}

		//TODO: add map downloading but should be only when player clicks map.
		// // Credit: Auros
		// public async Task<IPreviewBeatmapLevel?> DownloadLevel(string name, string hash, string url, State state, CancellationToken token, IProgress<float>? downloadProgress = null)
		// {
		// 	var response = await _httpService.GetAsync(url, downloadProgress, token);
		// 	if (!response.Successful)
		// 	{
		// 		_siraLog.Error(response.Code);
		// 		return null;
		// 	}
		//
		// 	// Songcore doesn't have a constant for the WIP folder and does the same Path.Combine to access that folder
		// 	var extractPath = await ExtractZipAsync(await response.ReadAsByteArrayAsync(), name, state == State.Published ? CustomLevelPathHelper.customLevelsDirectoryPath : Path.Combine(Application.dataPath, "CustomWIPLevels"));
		// 	if (string.IsNullOrEmpty(extractPath))
		// 		return null;
		//
		// 	// Eris's black magic 🙏
		// 	var semaphoreSlim = new SemaphoreSlim(0, 1);
		// 	void Release(SongCore.Loader _, ConcurrentDictionary<string, CustomPreviewBeatmapLevel> __)
		// 	{
		// 		SongCore.Loader.SongsLoadedEvent -= Release;
		// 		semaphoreSlim?.Release();
		// 	}
		// 	try
		// 	{
		// 		SongCore.Loader.SongsLoadedEvent += Release;
		// 		SongCore.Loader.Instance.RefreshSongs(false);
		// 		await semaphoreSlim.WaitAsync(CancellationToken.None);
		// 	}
		// 	catch (Exception e)
		// 	{
		// 		Release(null!, null!);
		// 		_siraLog.Error(e);
		// 		return null;
		// 	}
		// 	return TryGetLevel(hash, state != State.Published);
		// }

		private async Task<IEnumerable<ListCellComponent>> ConvertToCellComponents(List<RecommendationMap> recommendationMap)
		{
			var listCellComponents = new List<ListCellComponent>(recommendationMap.Count);
			foreach (var recommendation in recommendationMap)
			{
				var sprite = await LoadSpriteAsync(recommendation.CoverUrl);
				listCellComponents.Add(new ListCellComponent(recommendation.SongKey, recommendation.MapName, recommendation.Mapper, sprite));
			}

			return listCellComponents;
		}

		// Credits: Auros
		private async Task<Sprite> LoadSpriteAsync(string path)
		{
			if (_spriteCache.TryGetValue(path, out Sprite sprite))
			{
				return sprite;
			}

			_siraLog.Debug("Downloading Sprite at " + path);
			var response = await _httpService.GetAsync(path);
			if (response.Successful)
			{
				var imageBytes = await response.ReadAsByteArrayAsync();
				if (imageBytes.Length > 0)
				{
					_siraLog.Debug("Successfully downloaded sprite. Parsing and adding to cache.");
					if (_spriteCache.TryGetValue(path, out sprite))
					{
						return sprite;
					}

					sprite = BeatSaberMarkupLanguage.Utilities.LoadSpriteRaw(imageBytes);
					sprite.texture.wrapMode = TextureWrapMode.Clamp;
					_spriteCache.Add(path, sprite);
					return sprite;
				}
			}

			_siraLog.Warn("Could not downloading and parse sprite. Using blank sprite...");
			return BeatSaberMarkupLanguage.Utilities.ImageResources.BlankSprite;
		}

		[UIAction("selected-list-item")]
		public async Task SelectedListItem(TableView _, int index)
		{
			var item = _items[index];
			if (item == null)
			{
				return;
			}

			var hash = await _beatSaverService.GetMapInfoFromKey(item.SongKey);
			if(hash == null)
			{
				return;
			}

			var level = TryGetLevel(hash);
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