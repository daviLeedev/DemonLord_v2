using DemonLord.Domain;
using UnityEngine;

namespace DemonLord.Presentation.Exploration
{
    public enum AreaKind
    {
        Interior = 0,
        Exterior = 1,
    }

    [CreateAssetMenu(menuName = "DemonLord/Exploration/Area Definition", fileName = "AreaDefinition")]
    public sealed class AreaDefinition : ScriptableObject
    {
        [SerializeField] private string areaId = string.Empty;
        [SerializeField] private string sceneKey = string.Empty;
        [SerializeField] private string displayNameKey = string.Empty;
        [SerializeField] private string fallbackDisplayName = string.Empty;
        [SerializeField] private AreaKind areaKind;
        [SerializeField] private string defaultSpawnId = string.Empty;
        [SerializeField] private AreaMapDefinition mapDefinition;

        public string AreaId => areaId ?? string.Empty;
        public string SceneKey => sceneKey ?? string.Empty;
        public string DisplayNameKey => displayNameKey ?? string.Empty;
        public string FallbackDisplayName => fallbackDisplayName ?? string.Empty;
        public AreaKind AreaKind => areaKind;
        public string DefaultSpawnId => defaultSpawnId ?? string.Empty;
        public AreaMapDefinition MapDefinition => mapDefinition;

        public void Configure(
            string configuredAreaId,
            string configuredSceneKey,
            string configuredDisplayNameKey,
            string configuredFallbackDisplayName,
            AreaKind configuredAreaKind,
            string configuredDefaultSpawnId,
            AreaMapDefinition configuredMapDefinition)
        {
            areaId = configuredAreaId ?? string.Empty;
            sceneKey = configuredSceneKey ?? string.Empty;
            displayNameKey = configuredDisplayNameKey ?? string.Empty;
            fallbackDisplayName = configuredFallbackDisplayName ?? string.Empty;
            areaKind = configuredAreaKind;
            defaultSpawnId = configuredDefaultSpawnId ?? string.Empty;
            mapDefinition = configuredMapDefinition;
        }

        public bool TryValidate(out string errorCode)
        {
            if (!StableWorldId.IsValid(AreaId))
            {
                errorCode = "invalid_area_id";
                return false;
            }

            if (string.IsNullOrWhiteSpace(SceneKey))
            {
                errorCode = "area_scene_key_missing";
                return false;
            }

            if (string.IsNullOrWhiteSpace(FallbackDisplayName))
            {
                errorCode = "area_display_name_missing";
                return false;
            }

            if (!StableWorldId.IsValid(DefaultSpawnId))
            {
                errorCode = "invalid_default_spawn_id";
                return false;
            }

            if (mapDefinition == null)
            {
                errorCode = "area_map_definition_missing";
                return false;
            }

            if (!mapDefinition.TryValidate(out errorCode))
            {
                return false;
            }

            errorCode = null;
            return true;
        }
    }
}
