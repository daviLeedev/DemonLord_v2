using System;
using System.Collections.Generic;
using DemonLord.Domain;
using UnityEngine;

namespace DemonLord.Presentation.Exploration
{
    [CreateAssetMenu(menuName = "DemonLord/Exploration/Area Registry", fileName = "AreaRegistry")]
    public sealed class AreaRegistry : ScriptableObject
    {
        [SerializeField] private AreaDefinition[] definitions = Array.Empty<AreaDefinition>();

        public IReadOnlyList<AreaDefinition> Definitions => definitions ?? Array.Empty<AreaDefinition>();

        public void Configure(AreaDefinition[] configuredDefinitions)
        {
            definitions = configuredDefinitions == null
                ? Array.Empty<AreaDefinition>()
                : (AreaDefinition[])configuredDefinitions.Clone();
        }

        public bool TryGet(string areaId, out AreaDefinition definition)
        {
            definition = null;
            if (!StableWorldId.IsValid(areaId) || definitions == null)
            {
                return false;
            }

            foreach (AreaDefinition candidate in definitions)
            {
                if (candidate != null && string.Equals(candidate.AreaId, areaId, StringComparison.Ordinal))
                {
                    definition = candidate;
                    return true;
                }
            }

            return false;
        }

        public bool TryValidate(out string errorCode)
        {
            if (definitions == null || definitions.Length == 0)
            {
                errorCode = "area_definitions_missing";
                return false;
            }

            HashSet<string> areaIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> sceneKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (AreaDefinition definition in definitions)
            {
                if (definition == null)
                {
                    errorCode = "area_definition_invalid";
                    return false;
                }

                if (!definition.TryValidate(out errorCode))
                {
                    return false;
                }

                if (!areaIds.Add(definition.AreaId))
                {
                    errorCode = "area_id_duplicate";
                    return false;
                }

                if (!sceneKeys.Add(definition.SceneKey))
                {
                    errorCode = "area_scene_key_duplicate";
                    return false;
                }
            }

            errorCode = null;
            return true;
        }
    }
}
