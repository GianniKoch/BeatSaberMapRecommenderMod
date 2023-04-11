using BeatSaberMarkupLanguage.Components;
using UnityEngine;

namespace BeatSaberMapRecommender.UI
{
	public class ListCellComponent : CustomListTableData.CustomCellInfo
	{
		public string Key { get; set; }
		public string MapName { get; set; }
		public string Mapper { get; set; }

		internal ListCellComponent(string key, string mapName, string mapper, Sprite icon) : base(mapName, mapper, icon)
		{
			Key = key;
			MapName = mapName;
			Mapper = mapper;
		}
	}
}