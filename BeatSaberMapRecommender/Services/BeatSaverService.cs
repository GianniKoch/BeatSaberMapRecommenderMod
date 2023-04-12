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

		public async Task<string?> GetMapInfoFromKey(string itemSongKey)
		{
			var beatMap = await _beatSaver.Beatmap(itemSongKey);

			if (beatMap == null)
			{
				_siraLog.Error($"No beatmap found with key {itemSongKey}");
				return null;
			}

			var hash = beatMap.LatestVersion.Hash;

			return (hash);
		}
	}
}