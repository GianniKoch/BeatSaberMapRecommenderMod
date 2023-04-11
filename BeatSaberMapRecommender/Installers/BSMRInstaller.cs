using BeatSaberMapRecommender.Services;
using BeatSaberMapRecommender.UI;
using BeatSaberMapRecommender.UI.FlowCoordinators;
using BeatSaverSharp;
using Zenject;

namespace BeatSaberMapRecommender.Installers
{
	public sealed class BSMRInstaller : Installer
	{
		private readonly BeatSaver _beatSaver;

		public BSMRInstaller(BeatSaver beatSaver)
		{
			_beatSaver = beatSaver;
		}

		public override void InstallBindings()
		{
			Container.BindInstance(_beatSaver).AsSingle();
			Container.Bind<BeatSaverService>().AsSingle();
			Container.Bind<BSMRMenuViewController>().FromNewComponentAsViewController().AsSingle();
			Container.BindInterfacesAndSelfTo<LevelDetailButtonViewController>().AsSingle();
			Container.Bind<IInitializable>().To<BSMRFlowCoordinator>().FromNewComponentOnNewGameObject().AsSingle();
		}
	}
}