using BeatSaberMapRecommender.Installers;
using IPA;
using SiraUtil.Zenject;

namespace BeatSaberMapRecommender
{
	[Plugin(RuntimeOptions.DynamicInit), NoEnableDisable]
	public class Plugin
	{
		[Init]
		public Plugin(Zenjector zenject)
		{

			zenject.UseLogger();
			zenject.Install<BSMRInstaller>(Location.Menu);
		}
	}
}