using System.Threading.Tasks;
using BeatSaverSharp;
using SiraUtil.Logging;
namespace BeatSaberMapRecommender.Services
{
	public class BeatSaverService
	{
		private readonly SiraLog _siraLog;
		private readonly BeatSaver _beatSaver;

		public BeatSaverService(SiraLog siraLog, BeatSaver beatSaver)
		{
			_siraLog = siraLog;
			_beatSaver = beatSaver;
		}


		public async Task<(string, string, string)?> GetSongInfoFromLevelId(string levelId)
		{
			var hash = levelId.Substring(13);

			var beatMap = await _beatSaver.BeatmapByHash(hash);

			if (beatMap == null)
			{
				_siraLog.Error($"No beatmap found with hash {hash}");
				return null;
			}

			var songId = beatMap.ID;
			var difficulty = (int) beatMap.LatestVersion.Difficulties[0].Difficulty;
			var characteristic = (int) beatMap.LatestVersion.Difficulties[0].Characteristic;

			return (songId, difficulty.ToString(), characteristic.ToString());
		}

		public async Task<(string, string, string, string)?> GetMapInfoFromKey(string itemSongId)
		{
			//Todo: change songId to songKey
			var beatMap = await _beatSaver.Beatmap(itemSongId);

			if (beatMap == null)
			{
				_siraLog.Error($"No beatmap found with id {itemSongId}");
				return null;
			}

			var mapName = beatMap.Name;
			var mapper = beatMap.Metadata.LevelAuthorName;
			var coverUrl = beatMap.LatestVersion.CoverURL;
			var hash = beatMap.LatestVersion.Hash;

			return (mapName, mapper, coverUrl, hash);
		}
	}
}