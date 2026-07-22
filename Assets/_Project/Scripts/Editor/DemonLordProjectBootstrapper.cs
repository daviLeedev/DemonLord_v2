using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DemonLord.Editor
{
    public static class DemonLordProjectBootstrapper
    {
        private static TestRunnerApi testRunnerApi;
        private static FoundationTestCallbacks testCallbacks;

        private static readonly string[] RequiredFolders =
        {
            "Assets/_Project/Art",
            "Assets/_Project/Audio",
            "Assets/_Project/Prefabs",
            "Assets/_Project/Scenes",
            "Assets/_Project/ScriptableObjects",
            "Assets/_Project/Scripts/Application",
            "Assets/_Project/Scripts/Bootstrap",
            "Assets/_Project/Scripts/Domain",
            "Assets/_Project/Scripts/Infrastructure",
            "Assets/_Project/Scripts/Presentation",
            "Assets/_Project/Tests/PlayMode",
        };

        private static readonly SceneDefinition[] SceneDefinitions =
        {
            new SceneDefinition("Assets/_Project/Scenes/00_Boot.unity", "BootSceneRoot"),
            new SceneDefinition("Assets/_Project/Scenes/10_Frontend.unity", "FrontendSceneRoot"),
            new SceneDefinition("Assets/_Project/Scenes/90_GameShell.unity", "GameShellSceneRoot"),
        };

        [InitializeOnLoadMethod]
        private static void ScheduleInitialFoundationSetup()
        {
            EditorApplication.delayCall += EnsurePlayModeStartScene;

            if (SceneDefinitions.All(definition => File.Exists(definition.Path)) && HasExpectedBuildSettings())
            {
                return;
            }

            EditorApplication.delayCall += EnsureFoundationScenes;
        }

        [MenuItem("DemonLord/Bootstrap/Ensure Foundation Scenes")]
        public static void EnsureFoundationScenes()
        {
            foreach (string folder in RequiredFolders)
            {
                Directory.CreateDirectory(folder);
            }

            foreach (SceneDefinition definition in SceneDefinitions)
            {
                CreateSceneIfMissing(definition);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            EditorBuildSettings.scenes = SceneDefinitions
                .Select(definition => new EditorBuildSettingsScene(definition.Path, true))
                .ToArray();

            AssetDatabase.SaveAssets();
            Debug.Log("DemonLord foundation scenes and build settings have been configured.");
        }

        [MenuItem("DemonLord/Bootstrap/Use Boot Scene for Play Mode")]
        public static void EnsurePlayModeStartScene()
        {
            SceneAsset bootScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SceneDefinitions[0].Path);
            if (bootScene == null)
            {
                return;
            }

            EditorSceneManager.playModeStartScene = bootScene;
        }

        [MenuItem("DemonLord/Bootstrap/Validate Foundation")]
        public static void ValidateFoundation()
        {
            string[] missingScenePaths = SceneDefinitions
                .Select(definition => definition.Path)
                .Where(path => AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                .ToArray();

            if (missingScenePaths.Length > 0)
            {
                throw new InvalidOperationException(
                    "DemonLord foundation scenes are missing: " + string.Join(", ", missingScenePaths));
            }

            if (!HasExpectedBuildSettings())
            {
                throw new InvalidOperationException("DemonLord foundation build settings do not match the required scene order.");
            }

            Debug.Log("DemonLord foundation validation passed.");
        }

        [MenuItem("DemonLord/Bootstrap/Run EditMode Core Tests %#t")]
        public static void RunEditModeCoreTests()
        {
            if (testCallbacks != null)
            {
                TestRunnerApi.UnregisterTestCallback(testCallbacks);
            }

            testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
            testCallbacks = new FoundationTestCallbacks();
            testRunnerApi.RegisterCallbacks(testCallbacks);
            testRunnerApi.Execute(new ExecutionSettings(
                new Filter
                {
                    testMode = TestMode.EditMode,
                    groupNames = new[] { "^DemonLord\\.Tests\\.EditMode\\." },
                })
            {
                runSynchronously = true,
            });
        }

        private static bool HasExpectedBuildSettings()
        {
            string[] configuredScenePaths = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            return configuredScenePaths.SequenceEqual(SceneDefinitions.Select(definition => definition.Path));
        }

        private static void CreateSceneIfMissing(SceneDefinition definition)
        {
            if (File.Exists(definition.Path))
            {
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            GameObject root = new GameObject(definition.RootObjectName);
            root.transform.SetAsFirstSibling();
            EditorSceneManager.SaveScene(scene, definition.Path, false);
            EditorSceneManager.CloseScene(scene, true);
        }

        private readonly struct SceneDefinition
        {
            public SceneDefinition(string path, string rootObjectName)
            {
                Path = path;
                RootObjectName = rootObjectName;
            }

            public string Path { get; }

            public string RootObjectName { get; }
        }

        private sealed class FoundationTestCallbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun)
            {
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                if (result.FailCount > 0)
                {
                    Debug.LogError(
                        "DemonLord EditMode core tests failed. Failed=" + result.FailCount + ", Passed=" + result.PassCount + ".");
                    return;
                }

                Debug.Log("DemonLord EditMode core tests passed. Passed=" + result.PassCount + ".");
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
            }
        }
    }
}
