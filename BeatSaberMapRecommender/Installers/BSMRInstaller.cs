using BeatSaberMapRecommender.UI;
using BeatSaberMapRecommender.UI.FlowCoordinators;
using Zenject;

namespace BeatSaberMapRecommender.Installers
{
	public sealed class BSMRInstaller : Installer
	{
		public override void InstallBindings()
		{
			Container.Bind<BSMRMenuViewController>().FromNewComponentAsViewController().AsSingle();
			Container.BindInterfacesAndSelfTo<LevelDetailButtonViewController>().AsSingle();
			Container.Bind<IInitializable>().To<BSMRFlowCoordinator>().FromNewComponentOnNewGameObject().AsSingle();
		}
	}
}