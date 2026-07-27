#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DemonLord.Editor
{
    /// <summary>
    /// One-shot texture-kit rebuild trigger. This source file is removed after the scene is saved.
    /// </summary>
    internal static class WorldAdjustmentLabTextureBuildOnce
    {
        // The laboratory builder owns the source player/camera/UI hierarchy.
        // The area builder must always run after it because it binds persistent
        // shell services to those freshly-created objects.
        private const string RunKey = "DemonLord.WorldAdjustmentLab.IntegratedAreaBuild.V2";

        [InitializeOnLoadMethod]
        private static void Schedule()
        {
            if (SessionState.GetBool(RunKey, false))
            {
                return;
            }

            SessionState.SetBool(RunKey, true);
            EditorApplication.delayCall += BuildWhenReady;
        }

        private static void BuildWhenReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.SetBool(RunKey, false);
                EditorApplication.delayCall += Schedule;
                return;
            }

            WorldAdjustmentLabSceneBuilder.BuildWorldAdjustmentLab();
            AreaSystemSceneBuilder.Build();
            Debug.Log("World Adjustment Lab and area/map system integrated build passed.");
        }
    }
}
#endif
