using System;
using System.Threading.Tasks;
using DemonLord.Application;
using DemonLord.Infrastructure;
using UnityEngine;

namespace DemonLord.Bootstrap
{
    public sealed class AppRoot : MonoBehaviour
    {
        public FrontendCoordinator FrontendCoordinator { get; private set; }

        public IPlayerSession PlayerSession { get; private set; }

        public SettingsService SettingsService { get; private set; }

        private ISceneFlowService sceneFlowService;

        private async void Start()
        {
            try
            {
                ComposeServices();
                await sceneFlowService.LoadFrontendAsync(FrontendEntryMode.Opening);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        public Task LoadEntryAsync(EntryDestination destination)
        {
            if (sceneFlowService == null)
            {
                throw new InvalidOperationException("AppRoot has not been composed yet.");
            }

            return sceneFlowService.LoadEntryAsync(destination);
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void ComposeServices()
        {
            IClock clock = new SystemClock();
            SettingsService = new SettingsService(
                new FileSettingsRepository(UnityEngine.Application.persistentDataPath),
                new UnityGameSettingsRuntimeApplier());
            SettingsService.LoadAndApply();
            ISaveRepository saveRepository = new FileSaveRepository(
                UnityEngine.Application.persistentDataPath,
                new UnityJsonSaveSerializer(),
                new NoSaveMigrationPipeline());
            PlayerSession = new InMemoryPlayerSession();
            IEntryPointResolver entryPointResolver = new EntryPointResolver();
            FrontendCoordinator = new FrontendCoordinator(
                new ListSaveSlotsUseCase(saveRepository),
                new CreateNewGameUseCase(saveRepository, clock),
                new LoadGameUseCase(saveRepository),
                PlayerSession,
                entryPointResolver);
            sceneFlowService = new UnitySceneFlowService(
                PlayerSession,
                FrontendCoordinator,
                SettingsService,
                new SaveGameProgressUseCase(saveRepository, clock),
                new UnityApplicationQuitter());
        }
    }
}
