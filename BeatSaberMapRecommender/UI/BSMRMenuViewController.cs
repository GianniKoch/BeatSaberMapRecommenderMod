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
using Zenject;

namespace BeatSaberMapRecommender.UI
{
	[ViewDefinition("BeatSaberMapRecommender.UI.Views.MenuView.bsml")]
	[HotReload(RelativePathToLayout = @"Views\MenuView.bsml")]
	public class BSMRMenuViewController : BSMLAutomaticViewController
	{
		private const string RECOMMENDATION_URL = "https://api-beatsavermaprecommender.belgianbeatsaber.community";

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
			RecommendationList.data.Clear();
			RecommendationList.tableView.ReloadData();

			var recommendationItems = await GetRecommendationItems(level.levelID);
			if (recommendationItems == null)
			{
				_siraLog.Error("Failed to get recommendations");
				return;
			}

			await UnityMainThreadTaskScheduler.Factory.StartNew(async () =>
			{
				var cellComponents = await ConvertToCellComponents(recommendationItems);
				RecommendationList.data.AddRange(cellComponents);
				LoadingText.enabled = false;
				RecommendationList.tableView.ReloadData();
			});
		}

		private async Task<List<RecommendationMap>?> GetRecommendationItems(string levelId)
		{
			var songInfo = await _beatSaverService.GetSongInfoFromLevelId(levelId);

			if (songInfo == null)
			{
				_siraLog.Error($"Failed to get song info from level id {levelId}");
				return null;
			}

			var (songId, difficulty, characteristic) = songInfo.Value;

			//TODO: move to service!!! qyuickk! don't telleris
			var request = await _httpService.GetAsync(
				$"{RECOMMENDATION_URL}/recommendation?song_id={songId}&difficulty={difficulty}&characteristic={characteristic}&n_recommendations=20&n_best_tags=4");

			if (!request.Successful)
			{
				_siraLog.Error("Failed to get recommendations");
				return null;
			}

			using var response = await request.ReadAsStreamAsync();
			using var reader = new StreamReader(response);
			using var jsonReader = new JsonTextReader(reader);

			foreach (var item in _jsonSerializer!.Deserialize<List<RecommendationMapDto>>(jsonReader)!)
			{
				var mapInfo = await _beatSaverService.GetMapInfoFromKey(item.SongId);
				if (mapInfo == null)
				{
					continue;
				}

				var (mapName, mapper, coverUrl, hash) = mapInfo.Value;

				var level = TryGetLevel(hash);
				if (level == null)
				{
					continue;
				}

				var recommendationMap = new RecommendationMap(item, mapName, mapper, coverUrl, level);
				_items.Add(recommendationMap);
			}

			_items = _items.GroupBy(x => x.SongId).Select(x => x.First()).ToList();

			return _items;
		}

		// Credit: Auros
		public IPreviewBeatmapLevel? TryGetLevel(string hash, bool wip = false)
		{
			var cleanerHash = $"custom_level_{hash.ToUpper()}";
			return _beatMapLevelsModel.allLoadedBeatmapLevelPackCollection.beatmapLevelPacks.SelectMany(bm => bm.beatmapLevelCollection.beatmapLevels).FirstOrDefault(lvl => wip ? lvl.levelID.StartsWith(cleanerHash) : lvl.levelID == cleanerHash);
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
				listCellComponents.Add(new ListCellComponent(recommendation.SongId, recommendation.MapName, recommendation.Mapper, sprite));
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
		public void SelectedListItem(TableView _, int index)
		{
			var item = _items[index];

			if (item == null)
			{
				return;
			}

			ListItemWasClicked?.Invoke(item);

			_siraLog.Info($"Selected list item {index}");
		}
	}
}