using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DemonLord.Presentation.Exploration
{
    public interface IAreaSceneLoader
    {
        Task<AreaRoot> LoadAsync(AreaDefinition definition);
        Task UnloadAsync(AreaRoot areaRoot);
    }

    public sealed class UnityAreaSceneLoader : IAreaSceneLoader
    {
        public async Task<AreaRoot> LoadAsync(AreaDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            AsyncOperation operation = SceneManager.LoadSceneAsync(definition.SceneKey, LoadSceneMode.Additive);
            if (operation == null)
            {
                throw new InvalidOperationException("Unable to load area scene: " + definition.SceneKey);
            }

            await AwaitOperation(operation);
            Scene scene = SceneManager.GetSceneByName(definition.SceneKey);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                throw new InvalidOperationException("Loaded area scene is unavailable: " + definition.SceneKey);
            }

            AreaRoot match = null;
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                foreach (AreaRoot candidate in rootObject.GetComponentsInChildren<AreaRoot>(true))
                {
                    if (match != null)
                    {
                        throw new InvalidOperationException("Area scene contains multiple AreaRoot components: " + definition.SceneKey);
                    }

                    match = candidate;
                }
            }

            return match ?? throw new InvalidOperationException("AreaRoot is missing from area scene: " + definition.SceneKey);
        }

        public async Task UnloadAsync(AreaRoot areaRoot)
        {
            if (areaRoot == null)
            {
                return;
            }

            Scene scene = areaRoot.gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            AsyncOperation operation = SceneManager.UnloadSceneAsync(scene);
            if (operation != null)
            {
                await AwaitOperation(operation);
            }
        }

        private static async Task AwaitOperation(AsyncOperation operation)
        {
            while (!operation.isDone)
            {
                await Task.Yield();
            }
        }
    }
}
