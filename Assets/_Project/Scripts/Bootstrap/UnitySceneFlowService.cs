using System;
using System.Threading.Tasks;
using DemonLord.Application;
using DemonLord.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DemonLord.Bootstrap
{
    public sealed class UnitySceneFlowService : ISceneFlowService
    {
        private const string FrontendSceneName = "10_Frontend";
        private const string GameShellSceneName = "90_GameShell";
        private readonly IPlayerSession playerSession;

        public UnitySceneFlowService(IPlayerSession playerSession)
        {
            this.playerSession = playerSession ?? throw new ArgumentNullException(nameof(playerSession));
        }

        public Task LoadFrontendAsync()
        {
            return LoadSceneAsync(FrontendSceneName);
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
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                GameShellSessionView sessionView = rootObject.GetComponent<GameShellSessionView>();
                if (sessionView != null)
                {
                    sessionView.SetSession(playerSession);
                    return;
                }
            }

            throw new InvalidOperationException("GameShellSessionView is missing from the GameShell scene.");
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
