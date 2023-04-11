using System;
using BeatSaberMapRecommender.Installers;
using BeatSaverSharp;
using IPA;
using IPA.Loader;
using SiraUtil.Zenject;

namespace BeatSaberMapRecommender
{
	[Plugin(RuntimeOptions.DynamicInit), NoEnableDisable]
	public class Plugin
	{
		[Init]
		public Plugin(Zenjector zenject, PluginMetadata metadata)
		{
			var version = metadata.HVersion;
			zenject.UseLogger();
			zenject.UseHttpService();
			zenject.Install<BSMRInstaller>(Location.Menu, new BeatSaver("BeatSaberMapRecommender", new Version((int) version.Major, (int) version.Minor, (int) version.Patch)));
		}
	}
}