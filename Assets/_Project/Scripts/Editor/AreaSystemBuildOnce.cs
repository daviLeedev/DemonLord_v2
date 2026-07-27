#if UNITY_EDITOR
namespace DemonLord.Editor
{
    /// <summary>
    /// AreaSystemSceneBuilder is intentionally not scheduled independently.
    /// WorldAdjustmentLabTextureBuildOnce rebuilds the source shell first and
    /// then invokes the area builder so serialized player/input references
    /// cannot be invalidated by a later laboratory rebuild.
    /// </summary>
    internal static class AreaSystemBuildOnce
    {
    }
}
#endif
