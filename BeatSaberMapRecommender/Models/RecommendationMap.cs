namespace BeatSaberMapRecommender.Models
{
	public class RecommendationMap
	{
		public RecommendationMap(RecommendationMapDto dto)
		{
			Characteristic = dto.Characteristic;
			Difficulty = dto.Difficulty;
			MetaSim = dto.MetaSim;
			SongKey = dto.SongKey;
			TagSim = dto.TagSim;
			TotalSim = dto.TotalSim;
			MapName = dto.MapName;
			Mapper = dto.Uploader;
			CoverUrl = dto.CoverUrl;
		}

		public int? Characteristic { get; }

		public int? Difficulty { get; }

		public double? MetaSim { get; }

		public string SongKey { get; }

		public double? TagSim { get; }

		public double? TotalSim { get; }

		public string MapName { get; set; }

		public string Mapper { get; set; }

		public string CoverUrl { get; set; }

		public IPreviewBeatmapLevel? Level { get; set; }
	}
}