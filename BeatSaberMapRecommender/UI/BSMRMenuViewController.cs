using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeatSaberMapRecommender.Models;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using Newtonsoft.Json;
using SiraUtil.Logging;
using SiraUtil.Web;
using UnityEngine;

namespace BeatSaberMapRecommender.UI
{
	[ViewDefinition("BeatSaberMapRecommender.UI.Views.MenuView.bsml")]
	[HotReload(RelativePathToLayout = "")]
	public class BSMRMenuViewController : BSMLAutomaticViewController
	{
		private SiraLog _siraLog = null!;
		private IHttpService _httpService = null!;

		[UIValue("level-name")] protected string LevelName { get; set; } = null!;

		[UIComponent("recommendation-list")] protected readonly CustomListTableData RecommendationList = null!;

		private JsonSerializer? _jsonSerializer;

		public void Construct(SiraLog siraLog, IHttpService httpService)
		{
			_siraLog = siraLog;
			_httpService = httpService;
			_jsonSerializer = JsonSerializer.CreateDefault();
		}


		public async Task SelectLevel(IPreviewBeatmapLevel level)
		{
			LevelName = $"Recommendations for {level.songName}";

			var coverSprite = await level.GetCoverImageAsync(CancellationToken.None);
			RecommendationList.data.Clear();
			var recommendations = await GetRecommendationItems(coverSprite);
			RecommendationList.data.AddRange(recommendations);
			RecommendationList.tableView.ReloadData();
		}

		private async Task<IEnumerable<ListCellComponent>> GetRecommendationItems(Sprite coverSprite)
		{
			_siraLog.Info("Sending request!");

			var request = await _httpService.GetAsync(
				"https://api-beatsavermaprecommender.belgianbeatsaber.community/recommendation?song_id=1af89&difficulty=3&characteristic=0&n_recommendations=6&n_best_tags=4");
			if (!request.Successful)
			{
				_siraLog.Error("Failed to get recommendations");
				return Enumerable.Empty<ListCellComponent>();
			}

			using var response = await request.ReadAsStreamAsync();
			using var reader = new StreamReader(response);
			using var jsonReader = new JsonTextReader(reader);

			var json = _jsonSerializer!.Deserialize<List<RecommendationMap>>(jsonReader);

			return json.Select(recommendedMap => new ListCellComponent(recommendedMap.SongId, "aa", "aa", coverSprite)).ToList();
		}

		[UIAction("selected-list-item")]
		public void SelectedListItem(TableView _, int index)
		{
			_siraLog.Info($"Selected list item {index}");
		}
	}
}