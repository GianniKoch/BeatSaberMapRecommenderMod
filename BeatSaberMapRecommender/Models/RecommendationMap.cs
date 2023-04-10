namespace BeatSaberMapRecommender.Models
{
	public class RecommendationMap
	{
		public uint Characteristic { get; set; }
		public uint Difficulty { get; set; }
		public float MetaSim { get; set; }
		public string SongId { get; set; } = null!;
		public float TagSim { get; set; }
		public float TotalSim { get; set; }
	}
}