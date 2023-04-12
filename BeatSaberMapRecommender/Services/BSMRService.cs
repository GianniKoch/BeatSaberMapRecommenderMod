using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BeatSaberMapRecommender.Models;
using Newtonsoft.Json;
using SiraUtil.Logging;
using SiraUtil.Web;
using UnityEngine;

namespace BeatSaberMapRecommender.Services
{
	public class BSMRService
	{
		private const string RECOMMENDATION_URL = "https://api-beatsavermaprecommender.belgianbeatsaber.community";

		private readonly SiraLog _siraLog;
		private readonly IHttpService _httpService;
		private readonly JsonSerializer _jsonSerializer;
		private readonly BeatmapLevelsModel _beatMapLevelsModel;

		private readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

		public BSMRService(SiraLog siraLog, IHttpService httpService, BeatmapLevelsModel beatMapLevelsModel)
		{
			_siraLog = siraLog;
			_httpService = httpService;
			_beatMapLevelsModel = beatMapLevelsModel;
			_jsonSerializer = JsonSerializer.CreateDefault();
		}

		public async Task<List<RecommendationMap>> GetMapRecommendations(string songId, string difficulty, string characteristic, uint nRecommendations = 100, uint nBestTags = 4)
		{
			var request = await _httpService.GetAsync(
				$"{RECOMMENDATION_URL}/recommendation?song_id={songId}&difficulty={difficulty}&characteristic={characteristic}&n_recommendations={nRecommendations}&n_best_tags={nBestTags}");

			if (!request.Successful)
			{
				_siraLog.Error("Failed to get recommendations");
				return null!;
			}

			using var response = await request.ReadAsStreamAsync();
			using var reader = new StreamReader(response);
			using var jsonReader = new JsonTextReader(reader);

			return _jsonSerializer.Deserialize<List<RecommendationMapDto>>(jsonReader)!.Select(item => new RecommendationMap(item)).ToList();
		}

		// Credit: Auros
		public async Task<string> ExtractZipAsync(byte[] zip, string name, string customSongsPath, bool overwrite = false)
		{
			Stream zipStream = new MemoryStream(zip);
			try
			{
				string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
				Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));

				ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
				string basePath = name;
				string path = customSongsPath + "/" + r.Replace(basePath, "");
				;
				if (!overwrite && Directory.Exists(path))
				{
					int pathNum = 1;
					while (Directory.Exists(path + $" ({pathNum})")) ++pathNum;
					path += $" ({pathNum})";
				}

				if (!Directory.Exists(path))
					Directory.CreateDirectory(path);
				await Task.Run(() =>
				{
					foreach (var entry in archive.Entries)
					{
						var entryPath = Path.Combine(path, entry.Name);
						if (overwrite || !File.Exists(entryPath))
							entry.ExtractToFile(entryPath, overwrite);
					}
				}).ConfigureAwait(false);
				archive.Dispose();
				zipStream.Close();
				return path;
			}
			catch (Exception e)
			{
				_siraLog.Error(e);
				zipStream.Close();
				return "";
			}
		}

		// Credit: Auros
		public bool LevelIsInstalled(string hash, bool wip = false)
		{
			string cleanerHash = $"custom_level_{hash.ToUpper()}";
			bool levelExists = _beatMapLevelsModel.allLoadedBeatmapLevelPackCollection.beatmapLevelPacks.Any(bm =>
				bm.beatmapLevelCollection.beatmapLevels.Any(lvl => wip ? lvl.levelID.StartsWith(cleanerHash) : lvl.levelID == cleanerHash));
			return levelExists;
		}

		// Credit: Auros
		public IPreviewBeatmapLevel? TryGetLevel(string hash, bool wip = false)
		{
			var cleanerHash = $"custom_level_{hash.ToUpper()}";
			return _beatMapLevelsModel.allLoadedBeatmapLevelPackCollection.beatmapLevelPacks.SelectMany(bm => bm.beatmapLevelCollection.beatmapLevels)
				.FirstOrDefault(lvl => wip ? lvl.levelID.StartsWith(cleanerHash) : lvl.levelID == cleanerHash);
		}

		// Credit: Auros
		public async Task<string?> DownloadLevel(string name, string hash, string url)
		{
			var response = await _httpService.GetAsync(url);
			if (!response.Successful)
			{
				_siraLog.Error(response.Code);
				return null;
			}

			var extractPath = await ExtractZipAsync(await response.ReadAsByteArrayAsync(), name, CustomLevelPathHelper.customLevelsDirectoryPath);
			if (string.IsNullOrEmpty(extractPath))
			{
				return null;
			}

			// Eris's black magic 🙏
			var semaphoreSlim = new SemaphoreSlim(0, 1);

			void Release(SongCore.Loader _, ConcurrentDictionary<string, CustomPreviewBeatmapLevel> __)
			{
				SongCore.Loader.SongsLoadedEvent -= Release;
				semaphoreSlim?.Release();
			}

			try
			{
				SongCore.Loader.SongsLoadedEvent += Release;
				SongCore.Loader.Instance.RefreshSongs(false);
				await semaphoreSlim.WaitAsync(CancellationToken.None);
			}
			catch (Exception e)
			{
				Release(null!, null!);
				_siraLog.Error(e);
				return null;
			}

			return hash;
		}

		// Credits: Auros
		public async Task<Sprite> LoadSpriteAsync(string path)
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
	}
}