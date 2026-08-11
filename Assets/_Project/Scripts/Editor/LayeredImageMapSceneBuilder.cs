#if UNITY_EDITOR
using System;
using System.Linq;
using DemonLord.Presentation.Exploration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DemonLord.Editor
{
    public static class LayeredImageMapSceneBuilder
    {
        private const string GeneratedRootName = "__LayeredImageMap";
        private const string DefinitionFolder = "Assets/_Project/ScriptableObjects/Exploration/ImageMaps";
        private const string LabScenePath = "Assets/_Project/Scenes/91_LabInterior.unity";
        private const string CourtyardScenePath = "Assets/_Project/Scenes/92_BureauCourtyard.unity";
        private const string LabTexturePath = "Assets/_Project/Art/Maps/Layered/WorldAdjustmentLab/world_adjustment_lab_base_v2.png";
        private const string LabNavigationOverlayPath = "Assets/_Project/Art/Maps/Authoring/WorldAdjustmentLab/lab_navigation_overlay_v1.png";
        private const string CourtyardTexturePath = "Assets/_Project/Art/Maps/Layered/BureauCourtyard/bureau_courtyard_base_v1.png";
        private const string LabDefinitionPath = DefinitionFolder + "/WorldAdjustmentLabImageMap.asset";
        private const string CourtyardDefinitionPath = DefinitionFolder + "/BureauCourtyardImageMap.asset";

        [MenuItem("DemonLord/Exploration/Build Layered Image Maps")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Stop Play Mode before building layered image maps.");
            }

            PrepareAssets();
            ApplyToExistingScene(LabScenePath, true);
            ApplyToExistingScene(CourtyardScenePath, false);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Validate();
            Debug.Log("Layered image maps built successfully.");
        }

        [MenuItem("DemonLord/Exploration/Validate Layered Image Maps")]
        public static void Validate()
        {
            ValidateScene(LabScenePath, "world-adjustment-lab-image-map");
            ValidateScene(CourtyardScenePath, "bureau-courtyard-image-map");
            Debug.Log("Layered image maps validated successfully.");
        }

        public static void PrepareAssets()
        {
            EnsureFolder(DefinitionFolder);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureTexture(LabTexturePath);
            ConfigureTexture(LabNavigationOverlayPath);
            ConfigureTexture(CourtyardTexturePath);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Sprite labSprite = LoadSprite(LabTexturePath);
            Sprite labNavigationOverlay = LoadSprite(LabNavigationOverlayPath);
            Sprite courtyardSprite = LoadSprite(CourtyardTexturePath);
            LayeredImageMapDefinition labDefinition = LoadOrCreateDefinition(LabDefinitionPath);
            labDefinition.Configure(
                "world-adjustment-lab-image-map",
                labSprite,
                null,
                null,
                LabNavigationPresentationContract.ReferenceWorldCenter,
                LabNavigationPresentationContract.ReferenceOrthographicSize,
                LabNavigationPresentationContract.ReferenceAspect,
                LabNavigationPresentationContract.ReferenceYaw,
                LabNavigationPresentationContract.ReferencePitch,
                LabNavigationPresentationContract.BaseDepth);
            LayeredImageMapDefinition courtyardDefinition = LoadOrCreateDefinition(CourtyardDefinitionPath);
            courtyardDefinition.Configure(
                "bureau-courtyard-image-map",
                courtyardSprite,
                null,
                null,
                Vector3.zero,
                14f,
                16f / 9f,
                45f,
                35f,
                18f);
            EditorUtility.SetDirty(labDefinition);
            EditorUtility.SetDirty(courtyardDefinition);
            AssetDatabase.SaveAssets();
        }

        public static LayeredImageMapRenderer AttachLab(Transform areaGeneratedRoot, Transform environmentRoot)
        {
            if (environmentRoot == null)
            {
                throw new ArgumentNullException(nameof(environmentRoot));
            }

            Renderer[] hiddenRenderers = environmentRoot.GetComponentsInChildren<Renderer>(true)
                .Concat(areaGeneratedRoot.GetComponentsInChildren<LabDoorController>(true)
                    .SelectMany(door => door.GetComponentsInChildren<Renderer>(true)))
                .Concat(areaGeneratedRoot.GetComponentsInChildren<PrototypeInteractable>(true)
                    .Where(interactable => interactable.Facing == null)
                    .SelectMany(interactable => interactable.GetComponentsInChildren<Renderer>(true)))
                .Where(renderer => renderer != null
                    && renderer.GetComponentInParent<WorldInteractionIndicator>() == null)
                .Distinct()
                .ToArray();
            return Attach(
                areaGeneratedRoot,
                AssetDatabase.LoadAssetAtPath<LayeredImageMapDefinition>(LabDefinitionPath),
                hiddenRenderers);
        }

        public static LayeredImageMapRenderer AttachCourtyard(Transform areaGeneratedRoot)
        {
            Renderer[] hiddenRenderers = areaGeneratedRoot
                .GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.GetComponentInParent<LayeredImageMapRenderer>() == null)
                .ToArray();
            return Attach(
                areaGeneratedRoot,
                AssetDatabase.LoadAssetAtPath<LayeredImageMapDefinition>(CourtyardDefinitionPath),
                hiddenRenderers);
        }

        private static void ApplyToExistingScene(string scenePath, bool isLab)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                AreaRoot[] roots = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<AreaRoot>(true))
                    .ToArray();
                if (roots.Length != 1)
                {
                    throw new InvalidOperationException(scenePath + " must contain exactly one AreaRoot.");
                }

                AreaRoot areaRoot = roots[0];
                LayeredImageMapRenderer renderer;
                if (isLab)
                {
                    Transform environment = areaRoot.transform.Find("Content/Environment");
                    if (environment == null)
                    {
                        throw new InvalidOperationException("Lab environment root is missing.");
                    }

                    renderer = AttachLab(areaRoot.transform, environment);
                }
                else
                {
                    renderer = AttachCourtyard(areaRoot.transform);
                }

                areaRoot.SetImageMapRenderer(renderer);
                EditorUtility.SetDirty(areaRoot);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, scenePath, false);
            }
            finally
            {
                if (scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void ValidateScene(string scenePath, string expectedStableId)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                AreaRoot[] roots = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<AreaRoot>(true))
                    .ToArray();
                if (roots.Length != 1)
                {
                    throw new InvalidOperationException(scenePath + " must contain exactly one AreaRoot.");
                }

                LayeredImageMapRenderer renderer = roots[0].ImageMapRenderer;
                if (renderer == null)
                {
                    throw new InvalidOperationException(scenePath + " image map renderer is missing.");
                }

                if (!renderer.TryValidate(out string errorCode))
                {
                    throw new InvalidOperationException(scenePath + " image map is invalid: " + errorCode);
                }

                if (!string.Equals(renderer.Definition.StableId, expectedStableId, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(scenePath + " image map stable ID does not match.");
                }
            }
            finally
            {
                if (scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static LayeredImageMapRenderer Attach(
            Transform areaGeneratedRoot,
            LayeredImageMapDefinition definition,
            Renderer[] hiddenRenderers)
        {
            if (areaGeneratedRoot == null)
            {
                throw new ArgumentNullException(nameof(areaGeneratedRoot));
            }

            if (definition == null)
            {
                throw new InvalidOperationException("Image map definition is missing.");
            }

            if (!definition.TryValidate(out string errorCode))
            {
                throw new InvalidOperationException("Image map definition is invalid: " + errorCode);
            }

            Transform existing = areaGeneratedRoot.Find(GeneratedRootName);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            GameObject root = new GameObject(GeneratedRootName);
            root.transform.SetParent(areaGeneratedRoot, false);
            LayeredImageMapRenderer imageMapRenderer = root.AddComponent<LayeredImageMapRenderer>();
            SpriteRenderer baseLayer = CreateLayer("Base", root.transform);
            SpriteRenderer foregroundLayer = CreateLayer("Foreground", root.transform);
            SpriteRenderer lightingLayer = CreateLayer("Lighting", root.transform);
            imageMapRenderer.Configure(definition, baseLayer, foregroundLayer, lightingLayer, hiddenRenderers);
            EditorUtility.SetDirty(imageMapRenderer);
            return imageMapRenderer;
        }

        private static SpriteRenderer CreateLayer(string name, Transform parent)
        {
            GameObject layerObject = new GameObject(name);
            layerObject.transform.SetParent(parent, false);
            SpriteRenderer renderer = layerObject.AddComponent<SpriteRenderer>();
            renderer.drawMode = SpriteDrawMode.Simple;
            return renderer;
        }

        private static void ConfigureTexture(string assetPath)
        {
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath) == null)
            {
                throw new InvalidOperationException("Layered image map texture is missing: " + assetPath);
            }

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("Texture importer is missing: " + assetPath);
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static Sprite LoadSprite(string assetPath)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                throw new InvalidOperationException("Sprite import failed: " + assetPath);
            }

            return sprite;
        }

        private static LayeredImageMapDefinition LoadOrCreateDefinition(string assetPath)
        {
            LayeredImageMapDefinition definition = AssetDatabase.LoadAssetAtPath<LayeredImageMapDefinition>(assetPath);
            if (definition != null)
            {
                return definition;
            }

            definition = ScriptableObject.CreateInstance<LayeredImageMapDefinition>();
            AssetDatabase.CreateAsset(definition, assetPath);
            return definition;
        }

        private static void EnsureFolder(string assetFolder)
        {
            string[] segments = assetFolder.Split('/');
            string current = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[index]);
                }

                current = next;
            }
        }
    }
}
#endif
