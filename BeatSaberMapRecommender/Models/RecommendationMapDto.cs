using Newtonsoft.Json;

namespace BeatSaberMapRecommender.Models
{
	public class RecommendationMapDto
	{
		[JsonConstructor]
		public RecommendationMapDto(
			[JsonProperty("characteristic")] int? characteristic,
			[JsonProperty("difficulty")] int? difficulty,
			[JsonProperty("meta_sim")] double? metaSim,
			[JsonProperty("song_id")] string songKey,
			[JsonProperty("tag_sim")] double? tagSim,
			[JsonProperty("total_sim")] double? totalSim,
			[JsonProperty("map_name")] string mapName,
			[JsonProperty("uploader_name")] string uploader,
			[JsonProperty("cover_url")] string coverUrl
		)
		{
			Characteristic = characteristic;
			Difficulty = difficulty;
			MetaSim = metaSim;
			SongKey = songKey;
			TagSim = tagSim;
			TotalSim = totalSim;
			MapName = mapName;
			Uploader = uploader;
			CoverUrl = coverUrl;
		}

		[JsonProperty("characteristic")] public int? Characteristic { get; }

		[JsonProperty("difficulty")] public int? Difficulty { get; }

		[JsonProperty("meta_sim")] public double? MetaSim { get; }

		[JsonProperty("song_id")] public string SongKey { get; }

		[JsonProperty("tag_sim")] public double? TagSim { get; }

		[JsonProperty("total_sim")] public double? TotalSim { get; }

		[JsonProperty("map_name")] public string MapName { get; }

		[JsonProperty("uploader_name")] public string Uploader { get; }

		[JsonProperty("cover_url")] public string CoverUrl { get; }
	}
}