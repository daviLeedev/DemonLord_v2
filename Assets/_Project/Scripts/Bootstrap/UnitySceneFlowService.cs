using System;
using System.Threading.Tasks;
using DemonLord.Application;
using DemonLord.Presentation;
using DemonLord.Presentation.Exploration;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DemonLord.Bootstrap
{
    public sealed class UnitySceneFlowService : ISceneFlowService
    {
        private const string FrontendSceneName = "10_Frontend";
        private const string GameShellSceneName = "90_GameShell";
        private readonly IPlayerSession playerSession;
        private readonly FrontendCoordinator frontendCoordinator;
        private readonly SettingsService settingsService;
        private readonly SaveGameProgressUseCase saveProgress;
        private readonly IApplicationQuitter applicationQuitter;

        public UnitySceneFlowService(
            IPlayerSession playerSession,
            FrontendCoordinator frontendCoordinator,
            SettingsService settingsService,
            SaveGameProgressUseCase saveProgress,
            IApplicationQuitter applicationQuitter)
        {
            this.playerSession = playerSession ?? throw new ArgumentNullException(nameof(playerSession));
            this.frontendCoordinator = frontendCoordinator ?? throw new ArgumentNullException(nameof(frontendCoordinator));
            this.settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            this.saveProgress = saveProgress ?? throw new ArgumentNullException(nameof(saveProgress));
            this.applicationQuitter = applicationQuitter ?? throw new ArgumentNullException(nameof(applicationQuitter));
        }

        public async Task LoadFrontendAsync(FrontendEntryMode entryMode)
        {
            frontendCoordinator.PrepareForEntry(entryMode);
            await LoadSceneAsync(FrontendSceneName);
            Scene scene = SceneManager.GetActiveScene();
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                FrontendView frontendView = rootObject.GetComponent<FrontendView>();
                if (frontendView != null)
                {
                    frontendView.Initialize(frontendCoordinator, this, settingsService, entryMode);
                    return;
                }
            }

            throw new InvalidOperationException("FrontendView is missing from the Frontend scene.");
        }

        public async Task LoadEntryAsync(EntryDestination destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (destination.SceneKey != GameShellSceneName)
            {
                throw new InvalidOperationException("The requested entry destination is not supported: " + destination.SceneKey);
            }

            await LoadSceneAsync(GameShellSceneName);
            Scene scene = SceneManager.GetActiveScene();
            GameShellRoot gameShellRoot = null;
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                GameShellRoot candidate = rootObject.GetComponent<GameShellRoot>();
                if (candidate == null)
                {
                    continue;
                }

                if (gameShellRoot != null)
                {
                    throw new InvalidOperationException("Multiple GameShellRoot components were found in the GameShell scene.");
                }

                gameShellRoot = candidate;
            }

            if (gameShellRoot == null)
            {
                throw new InvalidOperationException("GameShellRoot is missing from the GameShell scene.");
            }

            string errorCode = await gameShellRoot.InitializeAsync(
                playerSession,
                destination,
                saveProgress,
                settingsService,
                this,
                applicationQuitter);
            if (errorCode != null)
            {
                throw new InvalidOperationException("GameShell initialization failed: " + errorCode);
            }
        }

        private static Task LoadSceneAsync(string sceneName)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (operation == null)
            {
                throw new InvalidOperationException("Unable to start loading scene: " + sceneName);
            }

            TaskCompletionSource<bool> completionSource = new TaskCompletionSource<bool>();
            operation.completed += ignored => completionSource.TrySetResult(true);
            return completionSource.Task;
        }
    }
}
