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
			[JsonProperty("song_id")] string songId,
			[JsonProperty("tag_sim")] double? tagSim,
			[JsonProperty("total_sim")] double? totalSim
		)
		{
			Characteristic = characteristic;
			Difficulty = difficulty;
			MetaSim = metaSim;
			SongId = songId;
			TagSim = tagSim;
			TotalSim = totalSim;
		}

		[JsonProperty("characteristic")] public int? Characteristic { get; }

		[JsonProperty("difficulty")] public int? Difficulty { get; }

		[JsonProperty("meta_sim")] public double? MetaSim { get; }

		[JsonProperty("song_id")] public string SongId { get; }

		[JsonProperty("tag_sim")] public double? TagSim { get; }

		[JsonProperty("total_sim")] public double? TotalSim { get; }
	}
}