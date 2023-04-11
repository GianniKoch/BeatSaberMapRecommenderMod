using Newtonsoft.Json;

namespace BeatSaberMapRecommender.Models
{
	public class RecommendationMap
	{
		public RecommendationMap(RecommendationMapDto dto, string mapName, string mapper, string coverUrl, IPreviewBeatmapLevel level)
		{
			MapName = mapName;
			Mapper = mapper;
			CoverUrl = coverUrl;
			Characteristic = dto.Characteristic;
			Difficulty = dto.Difficulty;
			MetaSim = dto.MetaSim;
			SongId = dto.SongId;
			TagSim = dto.TagSim;
			TotalSim = dto.TotalSim;
			Level = level;
		}

		public int? Characteristic { get; }

		public int? Difficulty { get; }

		public double? MetaSim { get; }

		public string SongId { get; }

		public double? TagSim { get; }

		public double? TotalSim { get; }

		public string MapName { get; }

		public string Mapper { get; }

		public string CoverUrl { get; }

		public IPreviewBeatmapLevel Level { get; set; }
	}
}