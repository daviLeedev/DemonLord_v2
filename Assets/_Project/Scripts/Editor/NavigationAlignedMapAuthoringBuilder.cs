#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DemonLord.Presentation.Exploration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DemonLord.Editor
{
    public static class NavigationAlignedMapAuthoringBuilder
    {
        private const string LabScenePath = "Assets/_Project/Scenes/91_LabInterior.unity";
        private const string AuthoringFolder = "Assets/_Project/Art/Maps/Authoring/WorldAdjustmentLab";
        private const string GuidePath = AuthoringFolder + "/lab_projection_guide_v1.png";
        private const string OverlayPath = AuthoringFolder + "/lab_navigation_overlay_v1.png";
        private const string MiniMapPath = "Assets/_Project/Art/Prototype/Maps/lab_interior_map_v2.png";
        private const int GuideLayer = 31;
        private const float PlayerClearanceDiameter = 0.7f;
        private const float NavigationOverlayHeight = 0.11f;

        private static readonly Color FloorGuide = new Color(0.12f, 0.77f, 0.82f, 1f);
        private static readonly Color ObstacleGuide = new Color(0.72f, 0.12f, 0.16f, 1f);
        private static readonly Color DoorGuide = new Color(0.91f, 0.68f, 0.24f, 1f);
        private static readonly Color SpawnGuide = new Color(0.20f, 0.48f, 1f, 1f);

        [MenuItem("DemonLord/Exploration/Export Navigation-Aligned Lab Art Guides")]
        public static void ExportLabGuides()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Stop Play Mode before exporting navigation guides.");
            }

            EnsureFolder(AuthoringFolder);
            EnsureFolder("Assets/_Project/Art/Prototype/Maps");
            Scene scene = EditorSceneManager.OpenScene(LabScenePath, OpenSceneMode.Additive);
            try
            {
                AreaRoot areaRoot = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<AreaRoot>(true))
                    .Single();
                Transform environment = areaRoot.transform.Find("Content/Environment");
                if (environment == null)
                {
                    throw new InvalidOperationException("Lab environment root is missing.");
                }

                ExportProjectedTexture(areaRoot.transform, environment, GuidePath, false);
                ExportProjectedTexture(areaRoot.transform, environment, OverlayPath, true);
                ExportTopDownMiniMap(areaRoot.transform, environment, MiniMapPath);
            }
            finally
            {
                if (scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureSprite(GuidePath, false);
            ConfigureSprite(OverlayPath, true);
            ConfigureSprite(MiniMapPath, false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("Navigation-aligned lab guide, overlay and mini-map exported.");
        }

        [MenuItem("DemonLord/Exploration/Build Navigation-Aligned Lab And SD Player")]
        public static void BuildNavigationAlignedLabAndSdPlayer()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ExportLabGuides();
            WorldAdjustmentLabSceneBuilder.BuildWorldAdjustmentLab();
            AreaSystemSceneBuilder.Build();
            LayeredImageMapSceneBuilder.Build();
            WorldAdjustmentLabSceneBuilder.ValidateWorldAdjustmentLab();
            AssetDatabase.SaveAssets();
            Debug.Log("Navigation-aligned lab and SD player build completed successfully.");
        }

        private static void ExportProjectedTexture(
            Transform areaRoot,
            Transform environment,
            string assetPath,
            bool overlayOnly)
        {
            GameObject previewRoot = new GameObject("__NavigationGuidePreview")
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            GameObject cameraObject = new GameObject("NavigationGuideCamera")
            {
                hideFlags = HideFlags.HideAndDontSave,
                layer = GuideLayer,
            };
            Camera camera = cameraObject.AddComponent<Camera>();
            RenderTexture renderTexture = null;
            Texture2D output = null;
            List<Material> materials = new List<Material>();
            try
            {
                Material floorMaterial = CreateGuideMaterial(
                    overlayOnly ? new Color(0.20f, 0.86f, 0.92f, 0.16f) : FloorGuide,
                    materials);
                Material obstacleMaterial = CreateGuideMaterial(
                    overlayOnly ? new Color(0.08f, 0.025f, 0.035f, 0.82f) : ObstacleGuide,
                    materials);
                Material doorMaterial = CreateGuideMaterial(
                    overlayOnly ? new Color(0.91f, 0.68f, 0.24f, 0.42f) : DoorGuide,
                    materials);
                Material spawnMaterial = CreateGuideMaterial(
                    overlayOnly ? new Color(0.20f, 0.48f, 1f, 0.34f) : SpawnGuide,
                    materials);

                BoxCollider[] solidColliders = areaRoot.GetComponentsInChildren<BoxCollider>(true)
                    .Where(source => source != null && !source.isTrigger)
                    .ToArray();
                foreach (BoxCollider source in solidColliders.Where(source => IsFloor(source.transform)))
                {
                    CreateProjectedBox(
                        source,
                        previewRoot.transform,
                        floorMaterial,
                        0.06f);
                }

                foreach (BoxCollider source in solidColliders.Where(source => !IsFloor(source.transform)))
                {
                    if (overlayOnly)
                    {
                        CreateProjectedBlockerFootprint(source, previewRoot.transform, obstacleMaterial);
                    }
                    else
                    {
                        CreateProjectedBox(source, previewRoot.transform, obstacleMaterial, 0f);
                    }
                }

                foreach (LabDoorController door in areaRoot.GetComponentsInChildren<LabDoorController>(true))
                {
                    if (door != null)
                    {
                        CreateMarkerBox(
                            "Door_" + door.name,
                            door.transform.position + Vector3.up * 0.08f,
                            new Vector3(1.5f, 0.08f, 1.5f),
                            Quaternion.identity,
                            previewRoot.transform,
                            doorMaterial);
                    }
                }

                foreach (AreaSpawnPoint spawn in areaRoot.GetComponentsInChildren<AreaSpawnPoint>(true))
                {
                    if (spawn != null)
                    {
                        CreateMarkerBox(
                            "Spawn_" + spawn.name,
                            spawn.transform.position + Vector3.up * 0.12f,
                            new Vector3(0.75f, 0.12f, 0.75f),
                            Quaternion.identity,
                            previewRoot.transform,
                            spawnMaterial);
                    }
                }

                Quaternion rotation = Quaternion.Euler(
                    LabNavigationPresentationContract.ReferencePitch,
                    LabNavigationPresentationContract.ReferenceYaw,
                    0f);
                Vector3 forward = rotation * Vector3.forward;
                camera.transform.SetPositionAndRotation(
                    LabNavigationPresentationContract.ReferenceWorldCenter - forward * 50f,
                    rotation);
                camera.orthographic = true;
                camera.orthographicSize = LabNavigationPresentationContract.ReferenceOrthographicSize;
                camera.aspect = LabNavigationPresentationContract.ReferenceAspect;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = overlayOnly ? Color.clear : new Color(0.025f, 0.035f, 0.05f, 1f);
                camera.cullingMask = 1 << GuideLayer;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 100f;
                camera.allowHDR = false;
                camera.allowMSAA = true;

                renderTexture = new RenderTexture(
                    LabNavigationPresentationContract.OutputWidth,
                    LabNavigationPresentationContract.OutputHeight,
                    24,
                    RenderTextureFormat.ARGB32)
                {
                    antiAliasing = 4,
                };
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = renderTexture;
                output = new Texture2D(
                    LabNavigationPresentationContract.OutputWidth,
                    LabNavigationPresentationContract.OutputHeight,
                    TextureFormat.RGBA32,
                    false,
                    false);
                output.ReadPixels(new Rect(0f, 0f, output.width, output.height), 0, 0, false);
                output.Apply(false, false);
                RenderTexture.active = previous;
                WritePng(assetPath, output.EncodeToPNG());
            }
            finally
            {
                if (output != null) UnityEngine.Object.DestroyImmediate(output);
                if (renderTexture != null) UnityEngine.Object.DestroyImmediate(renderTexture);
                foreach (Material material in materials) UnityEngine.Object.DestroyImmediate(material);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(previewRoot);
            }
        }

        private static void ExportTopDownMiniMap(Transform areaRoot, Transform environment, string assetPath)
        {
            const int width = 1024;
            const int height = 1024;
            Color32 background = new Color32(8, 14, 22, 255);
            Color32[] pixels = Enumerable.Repeat(background, width * height).ToArray();

            BoxCollider[] solidColliders = areaRoot.GetComponentsInChildren<BoxCollider>(true)
                .Where(source => source != null && !source.isTrigger)
                .ToArray();
            foreach (BoxCollider source in solidColliders.Where(source => IsFloor(source.transform)))
            {
                FillWorldRect(pixels, width, height, source.bounds, new Color32(43, 72, 88, 255), 0);
            }

            foreach (BoxCollider source in solidColliders.Where(source => !IsFloor(source.transform)))
            {
                Bounds blockedBounds = source.bounds;
                blockedBounds.Expand(new Vector3(PlayerClearanceDiameter, 0f, PlayerClearanceDiameter));
                FillWorldRect(pixels, width, height, blockedBounds, new Color32(12, 20, 29, 255), 3);
            }

            foreach (LabDoorController door in areaRoot.GetComponentsInChildren<LabDoorController>(true))
            {
                if (door == null) continue;
                Bounds bounds = new Bounds(door.transform.position, new Vector3(1.5f, 0.1f, 1.5f));
                FillWorldRect(pixels, width, height, bounds, new Color32(190, 151, 74, 255), 1);
            }

            foreach (AreaSpawnPoint spawn in areaRoot.GetComponentsInChildren<AreaSpawnPoint>(true))
            {
                if (spawn == null) continue;
                Bounds bounds = new Bounds(spawn.transform.position, new Vector3(0.55f, 0.1f, 0.55f));
                FillWorldRect(pixels, width, height, bounds, new Color32(102, 199, 232, 255), 0);
            }

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            try
            {
                texture.SetPixels32(pixels);
                texture.Apply(false, false);
                WritePng(assetPath, texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void FillWorldRect(
            Color32[] pixels,
            int width,
            int height,
            Bounds bounds,
            Color32 color,
            int insetPixels)
        {
            Vector3 origin = LabNavigationPresentationContract.MapWorldOrigin;
            Vector2 size = LabNavigationPresentationContract.MapWorldSize;
            int minX = Mathf.Clamp(Mathf.FloorToInt((bounds.min.x - origin.x) / size.x * width) + insetPixels, 0, width - 1);
            int maxX = Mathf.Clamp(Mathf.CeilToInt((bounds.max.x - origin.x) / size.x * width) - insetPixels, 0, width - 1);
            int minY = Mathf.Clamp(Mathf.FloorToInt((bounds.min.z - origin.z) / size.y * height) + insetPixels, 0, height - 1);
            int maxY = Mathf.Clamp(Mathf.CeilToInt((bounds.max.z - origin.z) / size.y * height) - insetPixels, 0, height - 1);
            for (int y = minY; y <= maxY; y++)
            {
                int row = y * width;
                for (int x = minX; x <= maxX; x++)
                {
                    pixels[row + x] = color;
                }
            }
        }

        private static void CreateProjectedBox(
            BoxCollider source,
            Transform parent,
            Material material,
            float topOffset)
        {
            Vector3 scale = Vector3.Scale(source.size, Abs(source.transform.lossyScale));
            Vector3 position = source.transform.TransformPoint(source.center);
            if (topOffset > 0f)
            {
                position.y = source.bounds.max.y + topOffset;
                scale.y = 0.08f;
            }

            CreateMarkerBox(source.name, position, scale, source.transform.rotation, parent, material);
        }

        private static void CreateProjectedBlockerFootprint(
            BoxCollider source,
            Transform parent,
            Material material)
        {
            Bounds bounds = source.bounds;
            Vector3 position = bounds.center;
            position.y = NavigationOverlayHeight;
            Vector3 scale = new Vector3(
                bounds.size.x + PlayerClearanceDiameter,
                0.08f,
                bounds.size.z + PlayerClearanceDiameter);
            CreateMarkerBox(source.name + "_BlockedFootprint", position, scale, Quaternion.identity, parent, material);
        }

        private static void CreateMarkerBox(
            string name,
            Vector3 position,
            Vector3 scale,
            Quaternion rotation,
            Transform parent,
            Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.hideFlags = HideFlags.HideAndDontSave;
            box.layer = GuideLayer;
            box.transform.SetParent(parent, false);
            box.transform.SetPositionAndRotation(position, rotation);
            box.transform.localScale = scale;
            UnityEngine.Object.DestroyImmediate(box.GetComponent<Collider>());
            box.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static bool IsFloor(Transform candidate)
        {
            return candidate != null
                && candidate.name.IndexOf("Floor", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static Vector3 Abs(Vector3 value)
        {
            return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
        }

        private static Material CreateGuideMaterial(Color color, ICollection<Material> owned)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Sprites/Default");
            if (shader == null)
            {
                throw new InvalidOperationException("No unlit shader is available for guide export.");
            }

            Material material = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave,
                color = color,
            };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", color.a < 0.999f ? 1f : 0f);
            owned.Add(material);
            return material;
        }

        private static void WritePng(string assetPath, byte[] bytes)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, ".."));
            string fullPath = Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? projectRoot);
            File.WriteAllBytes(fullPath, bytes);
        }

        private static void ConfigureSprite(string assetPath, bool alpha)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = alpha;
            importer.sRGBTexture = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
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
