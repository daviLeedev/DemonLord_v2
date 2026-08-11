#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DemonLord.Domain;
using DemonLord.Presentation;
using DemonLord.Presentation.Combat;
using DemonLord.Presentation.Exploration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DemonLord.Editor
{
    public static class AreaSystemSceneBuilder
    {
        private const string ShellScenePath = "Assets/_Project/Scenes/90_GameShell.unity";
        private const string LabScenePath = "Assets/_Project/Scenes/91_LabInterior.unity";
        private const string CourtyardScenePath = "Assets/_Project/Scenes/92_BureauCourtyard.unity";
        private const string DefinitionsFolder = "Assets/_Project/ScriptableObjects/Exploration/Areas";
        private const string MapArtFolder = "Assets/_Project/Art/Prototype/Maps";
        private const string LabMiniMapTexturePath = "Assets/_Project/Art/Maps/Layered/WorldAdjustmentLab/world_adjustment_lab_base_v2.png";
        private const string LabNavigationOverlayPath = "Assets/_Project/Art/Maps/Authoring/WorldAdjustmentLab/lab_navigation_overlay_v1.png";
        private const string CourtyardMapTexturePath = "Assets/_Project/Art/Maps/Layered/BureauCourtyard/bureau_courtyard_base_v1.png";
        private const string ShellGeneratedRootName = "__AreaSystemShellGenerated";
        private const string AreaGeneratedRootName = "__AreaSystemGenerated";
        private const string WorldGeneratedRootName = "__WorldAdjustmentLabGenerated";
        // V3 assets intentionally use fresh paths. Earlier generated assets referenced classes
        // that shared a source file, which Unity cannot deserialize reliably for components.
        // Leaving the old generated assets untouched preserves user data while the rebuilt
        // scenes use type/file-name matched assets exclusively.
        private const string LabMapAssetPath = DefinitionsFolder + "/LabInteriorMap_V3.asset";
        private const string CourtyardMapAssetPath = DefinitionsFolder + "/BureauCourtyardMap_V3.asset";
        private const string RegistryAssetPath = DefinitionsFolder + "/AreaRegistry_V3.asset";
        private const string MapUiGeneratedRootName = "__AreaSystemMapUi";
        private const string FadeGeneratedRootName = "AreaTransitionFade";

        [MenuItem("DemonLord/Exploration/Build Area Transition And Map System")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Stop Play Mode before building the area system.");
            }

            EnsureFolder(DefinitionsFolder);
            EnsureFolder(MapArtFolder);
            CreateMapTexture(MapArtFolder + "/bureau_courtyard_map.png", false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureMapTexture(LabMiniMapTexturePath);
            ConfigureMapTexture(LabNavigationOverlayPath);
            ConfigureMapTexture(CourtyardMapTexturePath);
            ConfigureMapTexture(MapArtFolder + "/bureau_courtyard_map.png");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            LayeredImageMapSceneBuilder.PrepareAssets();

            AreaAssets assets = CreateAreaAssets();
            Scene shellScene = OpenScene(ShellScenePath, OpenSceneMode.Additive);
            try
            {
                GameObject shellRoot = FindRoot(shellScene, "GameShellSceneRoot");
                Transform sourceRoot = shellRoot.transform.Find(WorldGeneratedRootName);
                if (sourceRoot == null)
                {
                    throw new InvalidOperationException(
                        "World Adjustment Lab source root is missing. Run 'Build World Adjustment Lab' first.");
                }

                BuildLabScene(sourceRoot, assets.Lab);
                BuildCourtyardScene(assets.Courtyard);
                ConfigureShell(shellRoot, sourceRoot, assets.Registry);
                EnsureBuildSettings();
                EditorSceneManager.MarkSceneDirty(shellScene);
                EditorSceneManager.SaveScene(shellScene, ShellScenePath, false);
                AssetDatabase.SaveAssets();
                Validate();
                Debug.Log("Area transition and map system built successfully.");
            }
            finally
            {
                if (shellScene.IsValid() && shellScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(shellScene, true);
                }
            }
        }

        [MenuItem("DemonLord/Exploration/Validate Area Transition And Map System")]
        public static void Validate()
        {
            AreaRegistry registry = AssetDatabase.LoadAssetAtPath<AreaRegistry>(RegistryAssetPath);
            if (registry == null)
            {
                throw new InvalidOperationException("Area registry is missing.");
            }

            if (!registry.TryValidate(out string registryError))
            {
                throw new InvalidOperationException("Area registry is invalid: " + registryError);
            }

            ValidateShellScene();
            ValidateAreaScene(LabScenePath, ExplorationAreaIds.WorldAdjustmentLabInterior);
            ValidateAreaScene(CourtyardScenePath, ExplorationAreaIds.BureauCourtyard);

            string[] enabledScenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            foreach (string path in RequiredScenePaths())
            {
                if (!enabledScenes.Contains(path, StringComparer.Ordinal))
                {
                    throw new InvalidOperationException("Build Settings is missing required scene: " + path);
                }
            }
        }

        private static void ValidateShellScene()
        {
            Scene scene = SceneManager.GetSceneByPath(ShellScenePath);
            bool openedForValidation = !scene.IsValid() || !scene.isLoaded;
            if (openedForValidation)
            {
                scene = EditorSceneManager.OpenScene(ShellScenePath, OpenSceneMode.Additive);
            }

            try
            {
                GameObject shellRoot = FindRoot(scene, "GameShellSceneRoot");
                GameShellRoot shell = shellRoot.GetComponent<GameShellRoot>();
                if (shell == null
                    || shell.AreaTransitionCoordinator == null
                    || shell.MapCoordinator == null
                    || shell.BattleHandoffCoordinator == null
                    || shell.CombatTrainingCoordinator == null)
                {
                    throw new InvalidOperationException(
                        "GameShell area/map references are missing. Rebuild the laboratory before the area system.");
                }

                RequireObjectReferences(
                    shell.AreaTransitionCoordinator,
                    "registry",
                    "playerRoot",
                    "playerController",
                    "playerMotor",
                    "cameraRig",
                    "inputReader",
                    "locationTracker",
                    "dialogueController",
                    "notificationView",
                    "fadeView");
                RequireObjectReferences(
                    shell.MapCoordinator,
                    "playerRoot",
                    "playerFacing",
                    "locationTracker",
                    "miniMapView",
                    "areaMapView",
                    "transitionCoordinator");
                RequireObjectReferences(
                    shell.InGameUiCoordinator,
                    "inputReader",
                    "dialogueController",
                    "pauseMenuView",
                    "mapCoordinator",
                    "areaTransitionCoordinator");
                RequireObjectReferences(
                    shell.BattleHandoffCoordinator,
                    "dialogueController",
                    "progressController",
                    "inputReader",
                    "preparationView",
                    "notificationView",
                    "battleFlowServiceSource");
                RequireObjectReferences(
                    shell.CombatTrainingCoordinator,
                    "inputReader",
                    "inGameUiCoordinator",
                    "view");
            }
            finally
            {
                if (openedForValidation && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void RequireObjectReferences(Component component, params string[] propertyNames)
        {
            if (component == null)
            {
                throw new InvalidOperationException("Area system component is missing from GameShell.");
            }

            SerializedObject serialized = new SerializedObject(component);
            foreach (string propertyName in propertyNames)
            {
                SerializedProperty property = serialized.FindProperty(propertyName);
                if (property == null || property.objectReferenceValue == null)
                {
                    throw new InvalidOperationException(
                        component.GetType().Name + "." + propertyName + " is not connected in GameShell.");
                }
            }
        }

        private static AreaAssets CreateAreaAssets()
        {
            Sprite labMapSprite = AssetDatabase.LoadAssetAtPath<Sprite>(LabMiniMapTexturePath);
            Sprite labNavigationOverlaySprite = AssetDatabase.LoadAssetAtPath<Sprite>(LabNavigationOverlayPath);
            Sprite courtyardMapSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CourtyardMapTexturePath);
            if (labMapSprite == null || labNavigationOverlaySprite == null || courtyardMapSprite == null)
            {
                throw new InvalidOperationException("Generated map sprites could not be imported.");
            }

            AreaMapDefinition labMap = LoadOrCreate<AreaMapDefinition>(LabMapAssetPath);
            MapFloorDefinition labFloor = new MapFloorDefinition();
            labFloor.Configure(
                "floor-1",
                "1층",
                labMapSprite,
                LabNavigationPresentationContract.MapImageWorldOrigin,
                LabNavigationPresentationContract.MapImageAxisX,
                LabNavigationPresentationContract.MapImageAxisY,
                LabNavigationPresentationContract.MapImageWorldSize,
                LabNavigationPresentationContract.MiniMapViewportWorldSize,
                labNavigationOverlaySprite);
            labMap.Configure(new[] { labFloor });

            AreaMapDefinition courtyardMap = LoadOrCreate<AreaMapDefinition>(CourtyardMapAssetPath);
            MapFloorDefinition courtyardFloor = new MapFloorDefinition();
            courtyardFloor.Configure(
                "ground",
                "지상",
                courtyardMapSprite,
                CourtyardNavigationPresentationContract.MapImageWorldOrigin,
                CourtyardNavigationPresentationContract.MapImageAxisX,
                CourtyardNavigationPresentationContract.MapImageAxisY,
                CourtyardNavigationPresentationContract.MapImageWorldSize,
                CourtyardNavigationPresentationContract.MiniMapViewportWorldSize);
            courtyardMap.Configure(new[] { courtyardFloor });

            AreaDefinition lab = LoadOrCreate<AreaDefinition>(DefinitionsFolder + "/LabInterior.asset");
            lab.Configure(
                ExplorationAreaIds.WorldAdjustmentLabInterior,
                "91_LabInterior",
                "area.world_adjustment_lab_interior",
                "세계조정국 연구실",
                AreaKind.Interior,
                ExplorationSpawnIds.ReceptionStart,
                labMap);

            AreaDefinition courtyard = LoadOrCreate<AreaDefinition>(DefinitionsFolder + "/BureauCourtyard.asset");
            courtyard.Configure(
                ExplorationAreaIds.BureauCourtyard,
                "92_BureauCourtyard",
                "area.bureau_courtyard",
                "세계조정국 중앙 청사",
                AreaKind.Exterior,
                ExplorationSpawnIds.LabExit,
                courtyardMap);

            AreaRegistry registry = LoadOrCreate<AreaRegistry>(RegistryAssetPath);
            registry.Configure(new[] { lab, courtyard });
            MarkDirty(labMap, courtyardMap, lab, courtyard, registry);
            return new AreaAssets(registry, lab, courtyard);
        }

        private static void BuildLabScene(Transform sourceRoot, AreaDefinition definition)
        {
            Scene scene = CreateOrOpenAreaScene(LabScenePath, "LabInteriorSceneRoot");
            try
            {
                GameObject sceneRoot = FindRoot(scene, "LabInteriorSceneRoot");
                RemoveOwnedRoot(sceneRoot.transform, AreaGeneratedRootName);
                GameObject generated = CreateObject(AreaGeneratedRootName, sceneRoot.transform);
                AreaRoot areaRoot = generated.AddComponent<AreaRoot>();

                Transform content = CreateObject("Content", generated.transform).transform;
                Transform sourceEnvironment = sourceRoot.Find("Environment");
                Transform sourceGameplay = sourceRoot.Find("Gameplay");
                Transform sourceLighting = sourceRoot.Find("Lighting");
                if (sourceEnvironment == null || sourceGameplay == null)
                {
                    throw new InvalidOperationException("The World Adjustment Lab source hierarchy is incomplete.");
                }

                GameObject environment = UnityEngine.Object.Instantiate(sourceEnvironment.gameObject, content, true);
                environment.name = "Environment";
                environment.SetActive(true);
                Transform gameplay = CreateObject("Gameplay", content).transform;
                foreach (Transform child in sourceGameplay.Cast<Transform>().ToArray())
                {
                    if (string.Equals(child.name, "TaxOfficer", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    GameObject clone = UnityEngine.Object.Instantiate(child.gameObject, gameplay, true);
                    clone.name = child.name;
                    clone.SetActive(true);
                }

                if (sourceLighting != null)
                {
                    GameObject lighting = UnityEngine.Object.Instantiate(sourceLighting.gameObject, content, true);
                    lighting.name = "Lighting";
                    lighting.SetActive(true);
                }

                foreach (LocationVolume volume in generated.GetComponentsInChildren<LocationVolume>(true))
                {
                    volume.Configure(
                        volume.StableId,
                        "세계조정국 연구실",
                        volume.StableId,
                        volume.RoomName,
                        "floor-1",
                        volume.Priority,
                        null);
                }

                AreaSpawnPoint start = CreateAreaSpawn(
                    generated.transform,
                    ExplorationSpawnIds.ReceptionStart,
                    "floor-1",
                    new Vector3(0f, 0.05f, -3.25f),
                    Quaternion.identity);
                AreaSpawnPoint fromCourtyard = CreateAreaSpawn(
                    generated.transform,
                    ExplorationSpawnIds.CourtyardEntrance,
                    "floor-1",
                    new Vector3(0f, 0.05f, -3.85f),
                    Quaternion.identity);
                AreaPortal exitPortal = CreatePortal(
                    generated.transform,
                    "lab-courtyard-exit",
                    "중앙 청사 앞마당 출입구",
                    ExplorationAreaIds.BureauCourtyard,
                    ExplorationSpawnIds.LabExit,
                    new Vector3(4.6f, 0f, -3.9f));
                MapFloorVolume floorVolume = CreateFloorVolume(
                    generated.transform,
                    "floor-1",
                    new Vector3(0f, 1.5f, 2.5f),
                    new Vector3(31f, 4f, 35f));

                areaRoot.Configure(
                    definition,
                    new[] { start, fromCourtyard },
                    new[] { exitPortal },
                    generated.GetComponentsInChildren<LocationVolume>(true),
                    new[] { floorVolume },
                    generated.GetComponentsInChildren<CameraZone>(true),
                    generated.GetComponentsInChildren<PrototypeInteractable>(true),
                    generated.GetComponentsInChildren<LabDoorController>(true));
                areaRoot.SetImageMapRenderer(LayeredImageMapSceneBuilder.AttachLab(generated.transform, environment.transform));
                MarkDirty(areaRoot, start, fromCourtyard, exitPortal, floorVolume);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, LabScenePath, false);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void BuildCourtyardScene(AreaDefinition definition)
        {
            Scene scene = CreateOrOpenAreaScene(CourtyardScenePath, "BureauCourtyardSceneRoot");
            try
            {
                GameObject sceneRoot = FindRoot(scene, "BureauCourtyardSceneRoot");
                RemoveOwnedRoot(sceneRoot.transform, AreaGeneratedRootName);
                GameObject generated = CreateObject(AreaGeneratedRootName, sceneRoot.transform);
                AreaRoot areaRoot = generated.AddComponent<AreaRoot>();
                Material floorMaterial = LoadMaterial("ReceptionFloor.mat");
                Material wallMaterial = LoadMaterial("WallStoneBrass.mat");
                Material trimMaterial = LoadMaterial("Trim.mat");

                CreateCube("CourtyardFloor", generated.transform, new Vector3(0f, -0.25f, 0f), new Vector3(30f, 0.5f, 22f), floorMaterial);
                CreateCube("BureauFacade", generated.transform, new Vector3(0f, 3f, 9.8f), new Vector3(22f, 6f, 0.6f), wallMaterial);
                CreateCube("FacadeLintel", generated.transform, new Vector3(0f, 5.6f, 9.25f), new Vector3(12f, 0.35f, 0.4f), trimMaterial);
                CreateCube("BoundaryWest", generated.transform, new Vector3(-15f, 1.5f, 0f), new Vector3(0.5f, 3f, 22f), wallMaterial);
                CreateCube("BoundaryEast", generated.transform, new Vector3(15f, 1.5f, 0f), new Vector3(0.5f, 3f, 22f), wallMaterial);
                CreateCube("BoundarySouth", generated.transform, new Vector3(0f, 1.5f, -11f), new Vector3(30f, 3f, 0.5f), wallMaterial);
                for (int index = -2; index <= 2; index++)
                {
                    CreateCube(
                        "CourtyardPillar_" + index,
                        generated.transform,
                        new Vector3(index * 5f, 1.25f, 4f),
                        new Vector3(0.8f, 2.5f, 0.8f),
                        trimMaterial);
                }

                Transform navigationMask = CreateObject("NavigationCollisionMask", generated.transform).transform;
                CreateCollisionMask("WestGarden", navigationMask, new Vector3(-8.8f, 0.7f, 5.2f), new Vector3(7.2f, 1.4f, 4.6f));
                CreateCollisionMask("EastGarden", navigationMask, new Vector3(8.2f, 0.7f, 5.0f), new Vector3(7.4f, 1.4f, 4.8f));
                CreateCollisionMask("AdjustmentMonument", navigationMask, new Vector3(7.6f, 0.7f, 0.7f), new Vector3(3.2f, 1.4f, 3.2f));
                CreateCollisionMask("SouthGateHouse", navigationMask, new Vector3(-10.4f, 0.7f, -8.4f), new Vector3(4.2f, 1.4f, 3.6f));

                GameObject locationObject = CreateObject("Location_Courtyard", generated.transform);
                locationObject.transform.position = new Vector3(0f, 1.5f, 0f);
                BoxCollider locationCollider = locationObject.AddComponent<BoxCollider>();
                locationCollider.isTrigger = true;
                locationCollider.size = new Vector3(29f, 4f, 21f);
                LocationVolume location = locationObject.AddComponent<LocationVolume>();
                location.Configure(
                    "bureau-courtyard",
                    "세계조정국 중앙 청사",
                    "research-wing-courtyard",
                    "연구동 앞마당",
                    "ground",
                    10,
                    null);

                AreaSpawnPoint spawn = CreateAreaSpawn(
                    generated.transform,
                    ExplorationSpawnIds.LabExit,
                    "ground",
                    new Vector3(0f, 0.05f, 5.8f),
                    Quaternion.Euler(0f, 180f, 0f));
                AreaPortal returnPortal = CreatePortal(
                    generated.transform,
                    "courtyard-lab-entrance",
                    "세계조정국 연구실 출입구",
                    ExplorationAreaIds.WorldAdjustmentLabInterior,
                    ExplorationSpawnIds.CourtyardEntrance,
                    new Vector3(0f, 0f, 8.6f));
                MapFloorVolume floorVolume = CreateFloorVolume(
                    generated.transform,
                    "ground",
                    new Vector3(0f, 1.5f, 0f),
                    new Vector3(31f, 4f, 23f));

                GameObject lightRoot = CreateObject("Courtyard Lighting", generated.transform);
                Light sun = lightRoot.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.color = new Color(0.45f, 0.58f, 0.78f);
                sun.intensity = 1.1f;
                lightRoot.transform.rotation = Quaternion.Euler(52f, -28f, 0f);

                areaRoot.Configure(
                    definition,
                    new[] { spawn },
                    new[] { returnPortal },
                    new[] { location },
                    new[] { floorVolume },
                    Array.Empty<CameraZone>(),
                    Array.Empty<PrototypeInteractable>(),
                    Array.Empty<LabDoorController>());
                areaRoot.SetImageMapRenderer(LayeredImageMapSceneBuilder.AttachCourtyard(generated.transform));
                MarkDirty(areaRoot, spawn, returnPortal, floorVolume, location, sun);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, CourtyardScenePath, false);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static BoxCollider CreateCollisionMask(string name, Transform parent, Vector3 position, Vector3 size)
        {
            GameObject root = CreateObject(name, parent);
            root.transform.position = position;
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.size = size;
            return collider;
        }

        private static void ConfigureShell(GameObject shellRoot, Transform sourceRoot, AreaRegistry registry)
        {
            RemoveOwnedRoot(shellRoot.transform, ShellGeneratedRootName);
            GameObject generated = CreateObject(ShellGeneratedRootName, shellRoot.transform);
            GameShellRoot shell = shellRoot.GetComponent<GameShellRoot>();
            if (shell == null)
            {
                throw new InvalidOperationException("GameShellRoot component is missing.");
            }

            Transform player = sourceRoot.Find("Gameplay/TaxOfficer");
            QuarterViewCameraRig cameraRig = sourceRoot.GetComponentInChildren<QuarterViewCameraRig>(true);
            Camera camera = sourceRoot.GetComponentInChildren<Camera>(true);
            ExplorationInputReader input = player == null ? null : player.GetComponent<ExplorationInputReader>();
            PlayerMotor motor = player == null ? null : player.GetComponent<PlayerMotor>();
            PlayerFacing facing = player == null ? null : player.GetComponent<PlayerFacing>();
            CharacterController controller = player == null ? null : player.GetComponent<CharacterController>();
            LocationTracker tracker = shellRoot.GetComponent<LocationTracker>();
            DialogueFocusController dialogue = sourceRoot.GetComponentInChildren<DialogueFocusController>(true);
            NotificationView notification = sourceRoot.GetComponentInChildren<NotificationView>(true);
            InGameUiCoordinator inGameUi = sourceRoot.GetComponentInChildren<InGameUiCoordinator>(true);
            LabProgressController progress = shellRoot.GetComponent<LabProgressController>();
            BattleHandoffCoordinator battleHandoff = sourceRoot.GetComponentInChildren<BattleHandoffCoordinator>(true);
            BattlePreparationView battlePreparation = sourceRoot.GetComponentInChildren<BattlePreparationView>(true);
            CombatTrainingCoordinator combatTraining = sourceRoot.GetComponentInChildren<CombatTrainingCoordinator>(true);
            CombatTrainingView combatTrainingView = sourceRoot.GetComponentInChildren<CombatTrainingView>(true);
            Canvas canvas = sourceRoot.GetComponentInChildren<Canvas>(true);
            if (player == null || cameraRig == null || camera == null || input == null || motor == null || facing == null
                || controller == null || tracker == null || dialogue == null || notification == null || inGameUi == null
                || progress == null || battleHandoff == null || battlePreparation == null
                || combatTraining == null || combatTrainingView == null || canvas == null)
            {
                throw new InvalidOperationException("The GameShell source references are incomplete.");
            }

            RemoveOwnedRoot(canvas.transform, FadeGeneratedRootName);
            RemoveOwnedRoot(canvas.transform, MapUiGeneratedRootName);
            // These names were created by the first, pre-owned-root revision of this builder.
            // They are not user-authored UI roots and must be removed once so map views do not
            // duplicate after the serialized component repair.
            RemoveOwnedRoot(canvas.transform, "MiniMap");
            RemoveOwnedRoot(canvas.transform, "AreaMapOverlay");
            ScreenFadeView fade = BuildFade(canvas.transform);
            MapUiReferences mapUi = BuildMapUi(canvas.transform);
            AreaTransitionCoordinator transition = generated.AddComponent<AreaTransitionCoordinator>();
            transition.Configure(
                registry,
                player,
                controller,
                motor,
                cameraRig,
                input,
                tracker,
                dialogue,
                notification,
                fade);
            MapCoordinator mapCoordinator = generated.AddComponent<MapCoordinator>();
            mapCoordinator.Configure(player, facing, tracker, mapUi.MiniMap, mapUi.AreaMap, transition, progress);
            PauseMenuView pauseView = sourceRoot.GetComponentInChildren<PauseMenuView>(true);
            inGameUi.Configure(input, dialogue, pauseView, mapCoordinator, transition);
            combatTraining.Configure(input, inGameUi, combatTrainingView);
            battleHandoff.Configure(dialogue, progress, input, battlePreparation, notification, combatTraining);

            SerializedObject shellSerialized = new SerializedObject(shell);
            SetObject(shellSerialized, "areaTransitionCoordinator", transition);
            SetObject(shellSerialized, "mapCoordinator", mapCoordinator);
            SetObject(shellSerialized, "battleHandoffCoordinator", battleHandoff);
            SetObject(shellSerialized, "combatTrainingCoordinator", combatTraining);
            Transform environment = sourceRoot.Find("Environment");
            Transform lighting = sourceRoot.Find("Lighting");
            Transform entryPoints = sourceRoot.Find("EntryPoints");
            List<GameObject> legacyContent = new List<GameObject>();
            if (environment != null)
            {
                environment.gameObject.SetActive(true);
                legacyContent.Add(environment.gameObject);
            }
            if (lighting != null)
            {
                lighting.gameObject.SetActive(true);
                legacyContent.Add(lighting.gameObject);
            }
            if (entryPoints != null)
            {
                entryPoints.gameObject.SetActive(true);
                legacyContent.Add(entryPoints.gameObject);
            }
            Transform gameplay = sourceRoot.Find("Gameplay");
            if (gameplay != null)
            {
                foreach (Transform child in gameplay)
                {
                    child.gameObject.SetActive(true);
                    if (!string.Equals(child.name, "TaxOfficer", StringComparison.Ordinal))
                    {
                        legacyContent.Add(child.gameObject);
                    }
                }
            }

            SetObjectArray(shellSerialized, "legacyAreaContentRoots", legacyContent.ToArray());
            shellSerialized.ApplyModifiedPropertiesWithoutUndo();

            MarkDirty(shell, transition, mapCoordinator, inGameUi, battleHandoff, combatTraining, combatTrainingView, fade, mapUi.MiniMap, mapUi.AreaMap);
        }

        private static ScreenFadeView BuildFade(Transform canvas)
        {
            GameObject overlay = new GameObject(FadeGeneratedRootName, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            overlay.transform.SetParent(canvas, false);
            Stretch(overlay.GetComponent<RectTransform>());
            Image image = overlay.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = true;
            CanvasGroup group = overlay.GetComponent<CanvasGroup>();
            ScreenFadeView fade = overlay.AddComponent<ScreenFadeView>();
            fade.Configure(group);
            overlay.transform.SetAsLastSibling();
            return fade;
        }

        private static MapUiReferences BuildMapUi(Transform canvas)
        {
            FrontendUiTheme theme = new FrontendUiTheme();
            GameObject generatedRoot = new GameObject(MapUiGeneratedRootName, typeof(RectTransform));
            generatedRoot.transform.SetParent(canvas, false);
            Stretch(generatedRoot.GetComponent<RectTransform>());
            Transform safeArea = generatedRoot.transform;

            GameObject miniRoot = CreatePanel("MiniMap", safeArea, new Color(0.035f, 0.055f, 0.08f, 0.94f));
            SetRect(miniRoot.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(42f, -270f), new Vector2(300f, 210f), new Vector2(0f, 1f));
            CanvasGroup miniGroup = miniRoot.AddComponent<CanvasGroup>();
            GameObject miniMask = CreatePanel("MapMask", miniRoot.transform, Color.white);
            SetRect(miniMask.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -5f), new Vector2(270f, 166f), new Vector2(0.5f, 0.5f));
            miniMask.AddComponent<Mask>().showMaskGraphic = false;
            RawImage miniImage = CreateRawImage("MapImage", miniMask.transform);
            Stretch(miniImage.rectTransform);
            RawImage miniOverlay = CreateRawImage("NavigationOverlay", miniMask.transform);
            miniOverlay.color = new Color(1f, 1f, 1f, 0.34f);
            miniOverlay.raycastTarget = false;
            Stretch(miniOverlay.rectTransform);
            Image miniMarkerImage = CreateImage("PlayerMarker", miniMask.transform, new Color(0.40f, 0.78f, 0.91f, 1f));
            SetRect(miniMarkerImage.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(18f, 26f), Vector2.one * 0.5f);
            Image miniObjectiveImage = CreateImage("ObjectiveMarker", miniMask.transform, new Color(0.95f, 0.72f, 0.24f, 1f));
            SetRect(miniObjectiveImage.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(18f, 18f), Vector2.one * 0.5f);
            Text north = CreateText("North", miniRoot.transform, theme.Font, "N", 18, TextAnchor.MiddleCenter);
            SetRect(north.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(40f, 24f), new Vector2(0.5f, 1f));
            Text miniFloor = CreateText("Floor", miniRoot.transform, theme.Font, string.Empty, 17, TextAnchor.MiddleRight);
            SetRect(miniFloor.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-12f, 10f), new Vector2(90f, 24f), new Vector2(1f, 0f));
            MiniMapView miniView = miniRoot.AddComponent<MiniMapView>();
            miniView.Configure(miniGroup, miniImage, miniOverlay, miniMarkerImage.rectTransform, miniObjectiveImage.rectTransform, miniFloor);

            GameObject fullRoot = CreatePanel("AreaMapOverlay", generatedRoot.transform, new Color(0.005f, 0.008f, 0.014f, 0.88f));
            Stretch(fullRoot.GetComponent<RectTransform>());
            CanvasGroup fullGroup = fullRoot.AddComponent<CanvasGroup>();
            GameObject frame = CreatePanel("MapFrame", fullRoot.transform, new Color(0.045f, 0.06f, 0.078f, 0.98f));
            SetRect(frame.GetComponent<RectTransform>(), Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(1180f, 800f), Vector2.one * 0.5f);
            Text area = CreateText("Area", frame.transform, theme.Font, string.Empty, 34, TextAnchor.MiddleCenter);
            SetRect(area.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(800f, 52f), new Vector2(0.5f, 1f));
            Text room = CreateText("Room", frame.transform, theme.Font, string.Empty, 22, TextAnchor.MiddleCenter);
            room.color = theme.MenuSecondary;
            SetRect(room.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -86f), new Vector2(800f, 34f), new Vector2(0.5f, 1f));
            Text floor = CreateText("Floor", frame.transform, theme.Font, string.Empty, 24, TextAnchor.MiddleLeft);
            SetRect(floor.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(54f, -116f), new Vector2(250f, 40f), new Vector2(0f, 1f));
            Text actualFloor = CreateText("ActualFloor", frame.transform, theme.Font, string.Empty, 19, TextAnchor.MiddleRight);
            actualFloor.color = theme.MenuSecondary;
            SetRect(actualFloor.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-54f, -116f), new Vector2(300f, 40f), new Vector2(1f, 1f));
            GameObject fullMask = CreatePanel("MapMask", frame.transform, Color.white);
            SetRect(fullMask.GetComponent<RectTransform>(), Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(0f, -15f), new Vector2(1040f, 590f), Vector2.one * 0.5f);
            fullMask.AddComponent<Mask>().showMaskGraphic = false;
            RawImage fullImage = CreateRawImage("MapImage", fullMask.transform);
            Stretch(fullImage.rectTransform);
            RawImage fullOverlay = CreateRawImage("NavigationOverlay", fullMask.transform);
            fullOverlay.color = new Color(1f, 1f, 1f, 0.42f);
            fullOverlay.raycastTarget = false;
            Stretch(fullOverlay.rectTransform);
            Image playerMarker = CreateImage("PlayerMarker", fullMask.transform, new Color(0.40f, 0.78f, 0.91f, 1f));
            SetRect(playerMarker.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(24f, 34f), Vector2.one * 0.5f);
            Image objectiveMarker = CreateImage("ObjectiveMarker", fullMask.transform, new Color(0.95f, 0.72f, 0.24f, 1f));
            SetRect(objectiveMarker.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(24f, 24f), Vector2.one * 0.5f);
            Text objectiveMarkerLabel = CreateText("Label", objectiveMarker.transform, theme.Font, string.Empty, 16, TextAnchor.MiddleLeft);
            objectiveMarkerLabel.color = new Color(1f, 0.88f, 0.55f, 1f);
            objectiveMarkerLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            SetRect(objectiveMarkerLabel.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(22f, 12f), new Vector2(360f, 30f), new Vector2(0f, 0.5f));
            RectTransform[] portalMarkers = new RectTransform[8];
            for (int index = 0; index < portalMarkers.Length; index++)
            {
                Image portal = CreateImage("PortalMarker_" + index, fullMask.transform, new Color(0.73f, 0.60f, 0.35f, 1f));
                SetRect(portal.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(17f, 17f), Vector2.one * 0.5f);
                Text portalLabel = CreateText("Label", portal.transform, theme.Font, string.Empty, 15, TextAnchor.MiddleLeft);
                portalLabel.color = new Color(0.94f, 0.84f, 0.58f, 1f);
                portalLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
                portalLabel.verticalOverflow = VerticalWrapMode.Overflow;
                SetRect(portalLabel.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(16f, 8f), new Vector2(190f, 28f), new Vector2(0f, 0.5f));
                portalMarkers[index] = portal.rectTransform;
            }

            Text legend = CreateText("Legend", frame.transform, theme.Font, "◆ 출입구     ▲ 현재 위치     ◇ 현재 업무", 18, TextAnchor.MiddleLeft);
            SetRect(legend.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(54f, 26f), new Vector2(360f, 34f), new Vector2(0f, 0f));
            Text help = CreateText("Help", frame.transform, theme.Font, string.Empty, 18, TextAnchor.MiddleRight);
            help.color = theme.MenuSecondary;
            SetRect(help.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-54f, 26f), new Vector2(620f, 34f), new Vector2(1f, 0f));
            AreaMapView areaView = fullRoot.AddComponent<AreaMapView>();
            areaView.Configure(fullGroup, fullImage, fullOverlay, playerMarker.rectTransform, objectiveMarker.rectTransform, objectiveMarkerLabel, portalMarkers, area, room, floor, actualFloor, help);
            fullRoot.transform.SetAsLastSibling();
            return new MapUiReferences(miniView, areaView);
        }

        private static AreaSpawnPoint CreateAreaSpawn(
            Transform parent,
            string spawnId,
            string floorId,
            Vector3 position,
            Quaternion rotation)
        {
            AreaSpawnPoint spawn = CreateObject("Spawn_" + spawnId, parent).AddComponent<AreaSpawnPoint>();
            spawn.Configure(spawnId, floorId);
            spawn.transform.SetPositionAndRotation(position, rotation);
            return spawn;
        }

        private static AreaPortal CreatePortal(
            Transform parent,
            string portalId,
            string displayName,
            string targetAreaId,
            string targetSpawnId,
            Vector3 position)
        {
            GameObject root = CreateObject("Portal_" + portalId, parent);
            root.layer = Physics.IgnoreRaycastLayer;
            root.transform.position = position;
            SphereCollider collider = root.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 1.8f;
            Transform focus = CreateObject("FocusPoint", root.transform).transform;
            focus.localPosition = new Vector3(0f, 1f, 0f);
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "SelectionMarker";
            marker.transform.SetParent(root.transform, false);
            marker.transform.localScale = new Vector3(1.1f, 0.025f, 1.1f);
            UnityEngine.Object.DestroyImmediate(marker.GetComponent<Collider>());
            Renderer renderer = marker.GetComponent<Renderer>();
            renderer.sharedMaterial = LoadMaterial("Selection.mat");
            marker.SetActive(false);
            AreaPortal portal = root.AddComponent<AreaPortal>();
            portal.Configure(
                portalId,
                displayName,
                "이동",
                targetAreaId,
                targetSpawnId,
                focus,
                marker);
            return portal;
        }

        private static MapFloorVolume CreateFloorVolume(Transform parent, string floorId, Vector3 position, Vector3 size)
        {
            GameObject root = CreateObject("FloorVolume_" + floorId, parent);
            root.transform.position = position;
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = size;
            MapFloorVolume volume = root.AddComponent<MapFloorVolume>();
            volume.Configure(floorId);
            return volume;
        }

        private static void ValidateAreaScene(string path, string expectedAreaId)
        {
            Scene scene = OpenScene(path, OpenSceneMode.Additive);
            try
            {
                AreaRoot[] roots = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<AreaRoot>(true))
                    .ToArray();
                if (roots.Length != 1)
                {
                    throw new InvalidOperationException(path + " must contain exactly one AreaRoot.");
                }

                if (!roots[0].TryValidate(expectedAreaId, out string errorCode))
                {
                    throw new InvalidOperationException(path + " AreaRoot validation failed: " + errorCode);
                }

                HashSet<string> portalIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (AreaPortal portal in roots[0].Portals)
                {
                    if (portal == null || !portalIds.Add(portal.StableId))
                    {
                        throw new InvalidOperationException(path + " contains an invalid or duplicate portal ID.");
                    }
                }

                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                    {
                        if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) > 0)
                        {
                            throw new InvalidOperationException(path + " contains a missing script at " + transform.name);
                        }
                    }
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void EnsureBuildSettings()
        {
            List<EditorBuildSettingsScene> result = new List<EditorBuildSettingsScene>();
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string path in RequiredScenePaths())
            {
                result.Add(new EditorBuildSettingsScene(path, true));
                seen.Add(path);
            }

            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (seen.Add(scene.path))
                {
                    result.Add(scene);
                }
            }

            EditorBuildSettings.scenes = result.ToArray();
        }

        private static IEnumerable<string> RequiredScenePaths()
        {
            yield return "Assets/_Project/Scenes/00_Boot.unity";
            yield return "Assets/_Project/Scenes/10_Frontend.unity";
            yield return ShellScenePath;
            yield return LabScenePath;
            yield return CourtyardScenePath;
        }

        private static Scene CreateOrOpenAreaScene(string path, string rootName)
        {
            Scene scene;
            if (File.Exists(path))
            {
                scene = OpenScene(path, OpenSceneMode.Additive);
            }
            else
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                new GameObject(rootName);
                EditorSceneManager.SaveScene(scene, path, false);
            }

            if (FindRootOrNull(scene, rootName) == null)
            {
                GameObject root = new GameObject(rootName);
                SceneManager.MoveGameObjectToScene(root, scene);
            }

            return scene;
        }

        private static Scene OpenScene(string path, OpenSceneMode mode)
        {
            Scene loaded = SceneManager.GetSceneByPath(path);
            return loaded.IsValid() && loaded.isLoaded ? loaded : EditorSceneManager.OpenScene(path, mode);
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            return FindRootOrNull(scene, name)
                ?? throw new InvalidOperationException("Scene root is missing: " + name);
        }

        private static GameObject FindRootOrNull(Scene scene, string name)
        {
            return scene.GetRootGameObjects().FirstOrDefault(root => string.Equals(root.name, name, StringComparison.Ordinal));
        }

        private static void RemoveOwnedRoot(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }
        }

        private static GameObject CreateObject(string name, Transform parent)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            return root;
        }

        private static GameObject CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = name;
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            root.transform.localScale = scale;
            root.GetComponent<Renderer>().sharedMaterial = material;
            return root;
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            Image image = root.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return root;
        }

        private static RawImage CreateRawImage(string name, Transform parent)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            root.transform.SetParent(parent, false);
            RawImage image = root.GetComponent<RawImage>();
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            Image image = root.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Text CreateText(string name, Transform parent, Font font, string value, int size, TextAnchor anchor)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Text));
            root.transform.SetParent(parent, false);
            Text text = root.GetComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = new Color(0.92f, 0.90f, 0.84f);
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 position,
            Vector2 size,
            Vector2 pivot)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static Material LoadMaterial(string name)
        {
            const string folder = "Assets/_Project/Art/Prototype/WorldAdjustmentLab/Materials/";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(folder + name);
            if (material == null)
            {
                throw new InvalidOperationException("Required material is missing: " + name);
            }

            return material;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }

            return asset;
        }

        private static void CreateMapTexture(string path, bool interior)
        {
            const int Size = 512;
            Color32 background = new Color32(15, 24, 34, 255);
            Color32 floor = interior ? new Color32(38, 57, 73, 255) : new Color32(42, 62, 67, 255);
            Color32 border = new Color32(185, 154, 89, 255);
            Color32 accent = new Color32(102, 199, 232, 255);
            Color32[] pixels = Enumerable.Repeat(background, Size * Size).ToArray();
            if (interior)
            {
                FillRect(pixels, Size, 155, 170, 202, 178, floor, border);
                FillRect(pixels, Size, 45, 210, 110, 120, floor, border);
                FillRect(pixels, Size, 357, 210, 110, 120, floor, border);
                FillRect(pixels, Size, 181, 348, 150, 100, floor, border);
                FillRect(pixels, Size, 201, 448, 110, 52, floor, border);
                FillRect(pixels, Size, 181, 48, 150, 100, floor, border);
            }
            else
            {
                FillRect(pixels, Size, 54, 70, 404, 340, floor, border);
                FillRect(pixels, Size, 120, 340, 272, 88, new Color32(27, 38, 49, 255), border);
                for (int x = 120; x < 392; x += 54)
                {
                    FillRect(pixels, Size, x, 225, 18, 70, accent, border);
                }
            }

            Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static void FillRect(
            Color32[] pixels,
            int size,
            int x,
            int y,
            int width,
            int height,
            Color32 fill,
            Color32 border)
        {
            for (int py = Mathf.Max(0, y); py < Mathf.Min(size, y + height); py++)
            {
                for (int px = Mathf.Max(0, x); px < Mathf.Min(size, x + width); px++)
                {
                    bool edge = px < x + 4 || px >= x + width - 4 || py < y + 4 || py >= y + height - 4;
                    pixels[py * size + px] = edge ? border : fill;
                }
            }
        }

        private static void ConfigureMapTexture(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static void EnsureFolder(string folder)
        {
            string[] segments = folder.Split('/');
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

        private static void SetObject(SerializedObject serialized, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName)
                ?? throw new InvalidOperationException("Serialized property is missing: " + propertyName);
            property.objectReferenceValue = value;
        }

        private static void SetObjectArray(
            SerializedObject serialized,
            string propertyName,
            IReadOnlyList<UnityEngine.Object> values)
        {
            SerializedProperty property = serialized.FindProperty(propertyName)
                ?? throw new InvalidOperationException("Serialized property is missing: " + propertyName);
            property.arraySize = values.Count;
            for (int index = 0; index < values.Count; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }

        private static void MarkDirty(params UnityEngine.Object[] values)
        {
            foreach (UnityEngine.Object value in values)
            {
                if (value != null)
                {
                    EditorUtility.SetDirty(value);
                }
            }
        }

        private readonly struct AreaAssets
        {
            public AreaAssets(AreaRegistry registry, AreaDefinition lab, AreaDefinition courtyard)
            {
                Registry = registry;
                Lab = lab;
                Courtyard = courtyard;
            }

            public AreaRegistry Registry { get; }
            public AreaDefinition Lab { get; }
            public AreaDefinition Courtyard { get; }
        }

        private readonly struct MapUiReferences
        {
            public MapUiReferences(MiniMapView miniMap, AreaMapView areaMap)
            {
                MiniMap = miniMap;
                AreaMap = areaMap;
            }

            public MiniMapView MiniMap { get; }
            public AreaMapView AreaMap { get; }
        }
    }
}
#endif
