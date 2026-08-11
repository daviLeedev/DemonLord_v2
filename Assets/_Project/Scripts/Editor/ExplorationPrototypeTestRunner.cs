using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace DemonLord.EditorTools
{
    public static class ExplorationPrototypeTestRunner
    {
        private const string CallbackObjectName = "Exploration PlayMode Test Callbacks";
        private const string RunActiveKey = "DemonLord.ExplorationPlayModeTests.Active";
        private const string PreviousStartSceneKey = "DemonLord.ExplorationPlayModeTests.PreviousStartScene";
        private const string GameShellScenePath = "Assets/_Project/Scenes/90_GameShell.unity";

        private static string ResultsPath => Path.Combine(
            UnityEngine.Application.persistentDataPath,
            "ExplorationPlayModeResults.xml");

        [InitializeOnLoadMethod]
        private static void RestoreCallbacksAfterDomainReload()
        {
            if (!SessionState.GetBool(RunActiveKey, false))
            {
                return;
            }

            RegisterCallbacks(SessionState.GetString(PreviousStartSceneKey, string.Empty));
        }

        [MenuItem("DemonLord/Exploration/Run PlayMode Tests")]
        public static void RunPlayModeTests()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ResultsPath) ?? UnityEngine.Application.persistentDataPath);
            CleanupExistingCallbacks();

            string previousStartScenePath = AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene);
            SessionState.SetString(PreviousStartSceneKey, previousStartScenePath ?? string.Empty);
            SessionState.SetBool(RunActiveKey, true);
            ExplorationPlayModeCallbacks callbacks = RegisterCallbacks(previousStartScenePath);

            // The Test Framework must own the PlayMode start scene so it can load its
            // temporary InitTestScene. Individual tests load 90_GameShell themselves.
            // The project bootstrapper observes RunActiveKey and therefore will not
            // replace the framework scene with 00_Boot during this run.
            SceneAsset gameShellScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(GameShellScenePath);
            if (gameShellScene == null)
            {
                throw new InvalidOperationException("PlayMode test start scene is missing: " + GameShellScenePath);
            }

            EditorSceneManager.playModeStartScene = null;

            TestRunnerApi api = ScriptableObject.CreateInstance<TestRunnerApi>();
            ExecutionSettings settings = new ExecutionSettings(new Filter
            {
                testMode = TestMode.PlayMode,
                assemblyNames = new[] { "DemonLord.PlayModeTests" },
            });

            try
            {
                string runId = api.Execute(settings);
                Debug.Log($"DemonLord PlayMode tests started. RunId={runId}");
            }
            catch
            {
                callbacks.RestorePlayModeStartScene();
                SessionState.EraseBool(RunActiveKey);
                SessionState.EraseString(PreviousStartSceneKey);
                TestRunnerApi.UnregisterTestCallback(callbacks);
                UnityEngine.Object.DestroyImmediate(callbacks);
                throw;
            }
        }

        private static ExplorationPlayModeCallbacks RegisterCallbacks(string previousStartScenePath)
        {
            CleanupExistingCallbacks();
            ExplorationPlayModeCallbacks callbacks = ScriptableObject.CreateInstance<ExplorationPlayModeCallbacks>();
            callbacks.name = CallbackObjectName;
            callbacks.ResultPath = ResultsPath;
            callbacks.PreviousPlayModeStartScenePath = previousStartScenePath ?? string.Empty;
            TestRunnerApi.RegisterTestCallback(callbacks, 100);
            return callbacks;
        }

        private static void CleanupExistingCallbacks()
        {
            ExplorationPlayModeCallbacks[] callbacks =
                Resources.FindObjectsOfTypeAll<ExplorationPlayModeCallbacks>();
            foreach (ExplorationPlayModeCallbacks callback in callbacks)
            {
                callback.RestorePlayModeStartScene();
                TestRunnerApi.UnregisterTestCallback(callback);
                UnityEngine.Object.DestroyImmediate(callback);
            }
        }

        [Serializable]
        public sealed class ExplorationPlayModeCallbacks : ScriptableObject, ICallbacks
        {
            [SerializeField] private string resultPath;
            [SerializeField] private string previousPlayModeStartScenePath;

            public string ResultPath
            {
                get => resultPath;
                set => resultPath = value;
            }

            public string PreviousPlayModeStartScenePath
            {
                get => previousPlayModeStartScenePath;
                set => previousPlayModeStartScenePath = value;
            }

            public void RestorePlayModeStartScene()
            {
                SceneAsset previousScene = string.IsNullOrWhiteSpace(previousPlayModeStartScenePath)
                    ? null
                    : AssetDatabase.LoadAssetAtPath<SceneAsset>(previousPlayModeStartScenePath);
                EditorSceneManager.playModeStartScene = previousScene;
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                int batchModeExitCode = 2;
                try
                {
                    string path = string.IsNullOrWhiteSpace(resultPath) ? ResultsPath : resultPath;
                    Directory.CreateDirectory(Path.GetDirectoryName(path) ?? UnityEngine.Application.persistentDataPath);
                    TestRunnerApi.SaveResultToFile(result, path);
                    batchModeExitCode = result.FailCount == 0 && result.InconclusiveCount == 0 ? 0 : 2;

                    string summary =
                        $"DemonLord PlayMode tests finished. Passed={result.PassCount}, "
                        + $"Failed={result.FailCount}, Skipped={result.SkipCount}, "
                        + $"Inconclusive={result.InconclusiveCount}. Results={path}";

                    if (result.FailCount == 0 && result.InconclusiveCount == 0)
                    {
                        Debug.Log(summary);
                    }
                    else
                    {
                        Debug.LogError(summary);
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
                finally
                {
                    RestorePlayModeStartScene();
                    SessionState.EraseBool(RunActiveKey);
                    SessionState.EraseString(PreviousStartSceneKey);
                    TestRunnerApi.UnregisterTestCallback(this);
                    DestroyImmediate(this);

                    if (UnityEngine.Application.isBatchMode)
                    {
                        EditorApplication.Exit(batchModeExitCode);
                    }
                }
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.FailCount > 0)
                {
                    Debug.LogError(
                        $"PlayMode test failed: {result.Test.FullName}\n{result.Message}\n{result.StackTrace}");
                }
            }
        }
    }
}
