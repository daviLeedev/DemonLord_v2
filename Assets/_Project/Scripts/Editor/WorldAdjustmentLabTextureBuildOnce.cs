#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DemonLord.Editor
{
    /// <summary>
    /// One-shot texture-kit rebuild trigger. This source file is removed after the scene is saved.
    /// </summary>
    public static class WorldAdjustmentLabTextureBuildOnce
    {
        // The laboratory builder owns the source player/camera/UI hierarchy.
        // The area builder must always run after it because it binds persistent
        // shell services to those freshly-created objects.
        private const string RunKey = "DemonLord.WorldAdjustmentLab.IntegratedAreaBuild.V7";
        private const string PlayModeTestRunActiveKey = "DemonLord.ExplorationPlayModeTests.Active";
        private static bool integratedBuildRanThisDomain;

        public static void BuildIntegrated()
        {
            if (integratedBuildRanThisDomain)
            {
                return;
            }

            integratedBuildRanThisDomain = true;
            SessionState.SetBool(RunKey, true);
            WorldAdjustmentLabSceneBuilder.BuildWorldAdjustmentLab();
            AreaSystemSceneBuilder.Build();
            Debug.Log("World Adjustment Lab and area/map system integrated build passed.");
        }

        [InitializeOnLoadMethod]
        private static void Schedule()
        {
            if (IsAutomatedTestProcess())
            {
                return;
            }

            if (SessionState.GetBool(PlayModeTestRunActiveKey, false)
                && EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            // A cancelled or interrupted PlayMode run can leave this session flag behind.
            // Do not let that stale value permanently suppress interactive scene rebuilds.
            SessionState.SetBool(PlayModeTestRunActiveKey, false);

            if (SessionState.GetBool(RunKey, false))
            {
                return;
            }

            SessionState.SetBool(RunKey, true);
            EditorApplication.delayCall += BuildWhenReady;
        }

        private static bool IsAutomatedTestProcess()
        {
            if (!UnityEngine.Application.isBatchMode)
            {
                return false;
            }

            string[] arguments = System.Environment.GetCommandLineArgs();
            foreach (string argument in arguments)
            {
                if (string.Equals(argument, "-runTests", System.StringComparison.OrdinalIgnoreCase)
                    || argument.IndexOf("ExplorationPrototypeTestRunner.RunPlayModeTests", System.StringComparison.Ordinal) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static void BuildWhenReady()
        {
            if (integratedBuildRanThisDomain)
            {
                return;
            }

            if (SessionState.GetBool(PlayModeTestRunActiveKey, false)
                && EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.SetBool(RunKey, false);
                return;
            }

            SessionState.SetBool(PlayModeTestRunActiveKey, false);

            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.SetBool(RunKey, false);
                EditorApplication.delayCall += Schedule;
                return;
            }

            BuildIntegrated();
        }
    }
}
#endif
