using System;
using System.Collections.Generic;
using System.Linq;
using DemonLord.Presentation;
using DemonLord.Presentation.Exploration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DemonLord.Editor
{
    public static class ExplorationPrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/90_GameShell.unity";
        private const string SceneRootName = "GameShellSceneRoot";
        private const string GeneratedRootName = "__ExplorationPrototypeGenerated";
        private const string PrefabFolder = "Assets/_Project/Prefabs/Exploration";
        private const string MaterialFolder = "Assets/_Project/Art/Prototype/Materials";

        [MenuItem("DemonLord/Exploration/Build Prototype Scene")]
        public static void BuildPrototypeScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Stop Play Mode before building the exploration prototype scene.");
            }

            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene targetScene = FindLoadedScene(ScenePath);
            bool openedForBuild = !targetScene.IsValid();
            if (openedForBuild)
            {
                targetScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            SceneManager.SetActiveScene(targetScene);
            try
            {
                EnsureAssetFolder(PrefabFolder);
                EnsureAssetFolder(MaterialFolder);

                PrototypeMaterials materials = CreateMaterials();
                PrototypePrefabs prefabs = CreatePrefabs(materials);
                GameObject sceneRoot = GetOrCreateSceneRoot(targetScene);
                RemovePreviousGeneratedRoot(sceneRoot.transform);

                GameObject generatedRoot = CreateObject(GeneratedRootName, sceneRoot.transform);
                BuildScene(sceneRoot, generatedRoot, targetScene, prefabs, materials);

                EditorSceneManager.MarkSceneDirty(targetScene);
                if (!EditorSceneManager.SaveScene(targetScene, ScenePath, false))
                {
                    throw new InvalidOperationException("Unity could not save the exploration prototype scene.");
                }

                AssetDatabase.SaveAssets();
                ValidateScene(targetScene);
                Debug.Log("Exploration prototype scene built and validated: " + ScenePath);
            }
            finally
            {
                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                {
                    SceneManager.SetActiveScene(previousActiveScene);
                }

                if (openedForBuild && targetScene.IsValid() && targetScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(targetScene, true);
                }
            }
        }

        [MenuItem("DemonLord/Exploration/Validate Prototype Scene")]
        public static void ValidatePrototypeScene()
        {
            Scene targetScene = FindLoadedScene(ScenePath);
            bool openedForValidation = !targetScene.IsValid();
            if (openedForValidation)
            {
                targetScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            try
            {
                ValidateScene(targetScene);
                Debug.Log("Exploration prototype validation passed: " + ScenePath);
            }
            finally
            {
                if (openedForValidation && targetScene.IsValid() && targetScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(targetScene, true);
                }
            }
        }

        private static void BuildScene(
            GameObject sceneRoot,
            GameObject generatedRoot,
            Scene targetScene,
            PrototypePrefabs prefabs,
            PrototypeMaterials materials)
        {
            GameShellSessionView diagnostics = GetOrAdd<GameShellSessionView>(sceneRoot);
            GameShellRoot compositionRoot = GetOrAdd<GameShellRoot>(sceneRoot);

            Transform entryPoints = CreateObject("EntryPoints", generatedRoot.transform).transform;
            SpawnPoint spawnPoint = CreateObject("Spawn_start", entryPoints).AddComponent<SpawnPoint>();
            spawnPoint.transform.SetPositionAndRotation(new Vector3(0f, 0.05f, -5f), Quaternion.identity);
            spawnPoint.Configure("start");

            Transform gameplay = CreateObject("Gameplay", generatedRoot.transform).transform;
            GameObject player = InstantiatePrefab(prefabs.Player, targetScene, gameplay, "Player");
            player.transform.SetPositionAndRotation(spawnPoint.transform.position, spawnPoint.transform.rotation);
            CharacterController controller = player.GetComponent<CharacterController>();
            ExplorationInputReader inputReader = player.GetComponent<ExplorationInputReader>();
            PlayerFacing playerFacing = player.GetComponent<PlayerFacing>();
            PlayerMotor playerMotor = player.GetComponent<PlayerMotor>();
            InteractionSensor sensor = player.GetComponent<InteractionSensor>();
            Transform sensorOrigin = player.transform.Find("SensorOrigin");

            Transform cameraArea = CreateObject("CameraRig", generatedRoot.transform).transform;
            GameObject cameraRigObject = InstantiatePrefab(prefabs.CameraRig, targetScene, cameraArea, "QuarterViewCameraRig");
            QuarterViewCameraRig cameraRig = cameraRigObject.GetComponent<QuarterViewCameraRig>();
            Camera gameCamera = cameraRigObject.GetComponentInChildren<Camera>(true);
            gameCamera.tag = "MainCamera";
            cameraRig.Configure(gameCamera, player.transform, inputReader, inputReader.Gate);
            playerMotor.Initialize(inputReader, cameraRig, playerFacing);

            Transform uiArea = CreateObject("UI", generatedRoot.transform).transform;
            GameObject hud = InstantiatePrefab(prefabs.Hud, targetScene, uiArea, "ExplorationHud");
            InteractionPromptView promptView = hud.GetComponentInChildren<InteractionPromptView>(true);
            DialogueFocusController dialogueController = hud.GetComponentInChildren<DialogueFocusController>(true);
            ConfigureSensor(sensor, inputReader, playerFacing, player.transform, sensorOrigin, promptView);
            ConfigureDialogue(dialogueController, inputReader, playerFacing, player.transform, cameraRig);

            Transform environment = CreateObject("Environment", generatedRoot.transform).transform;
            BuildEnvironment(environment, materials);

            PrototypeInteractable firstNpc = CreateNpc(
                prefabs.Npc,
                targetScene,
                gameplay,
                "NPC_Aster",
                new Vector3(2.4f, 0f, -0.8f),
                "npc-aster",
                "아스테르",
                new[] { "이곳은 이동과 카메라를 시험하는 임시 공간입니다.", "Q와 E로 시점을 돌려 보세요." },
                dialogueController);
            PrototypeInteractable secondNpc = CreateNpc(
                prefabs.Npc,
                targetScene,
                gameplay,
                "NPC_Mina",
                new Vector3(-3.1f, 0f, 2.2f),
                "npc-mina",
                "미나",
                new[] { "달리기는 왼쪽 Shift, 대시는 Space입니다.", "대화가 끝나면 이전 카메라로 돌아갑니다." },
                dialogueController);
            PrototypeInteractable inspectObject = CreateInspectObject(
                prefabs.InspectObject,
                targetScene,
                gameplay,
                dialogueController);

            FaceAnchorToward(firstNpc.DialogueCameraAnchor, firstNpc.FocusPoint.position);
            FaceAnchorToward(secondNpc.DialogueCameraAnchor, secondNpc.FocusPoint.position);
            FaceAnchorToward(inspectObject.DialogueCameraAnchor, inspectObject.FocusPoint.position);

            Transform lighting = CreateObject("Lighting", generatedRoot.transform).transform;
            GameObject lightObject = CreateObject("Directional Light", lighting);
            Light directionalLight = lightObject.AddComponent<Light>();
            directionalLight.type = LightType.Directional;
            directionalLight.intensity = 1.2f;
            directionalLight.color = new Color(0.78f, 0.84f, 1f);
            lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

            GameObject zoneObject = CreateObject("CameraZone_RuinedRoom", environment);
            zoneObject.transform.position = new Vector3(0f, 1.25f, 7.5f);
            BoxCollider zoneCollider = zoneObject.AddComponent<BoxCollider>();
            zoneCollider.size = new Vector3(6f, 2.5f, 4.5f);
            zoneCollider.isTrigger = true;
            CameraZone zone = zoneObject.AddComponent<CameraZone>();
            zone.Configure(
                cameraRig,
                new QuarterViewCameraProfile(135f, 40f, 7f, new Vector3(0f, 1.25f, 0.5f), 0.35f),
                10,
                "ruined-room");

            compositionRoot.Configure(
                player.transform,
                controller,
                gameCamera,
                inputReader,
                playerFacing,
                playerMotor,
                cameraRig,
                sensor,
                diagnostics,
                new[] { spawnPoint });

            MarkDirty(
                compositionRoot,
                spawnPoint,
                controller,
                inputReader,
                playerFacing,
                playerMotor,
                sensor,
                gameCamera,
                cameraRig,
                promptView,
                dialogueController,
                zone,
                firstNpc,
                secondNpc,
                inspectObject);
        }

        private static void BuildEnvironment(Transform parent, PrototypeMaterials materials)
        {
            CreateCube("Ground", parent, new Vector3(0f, -0.25f, 2f), new Vector3(22f, 0.5f, 22f), Quaternion.identity, materials.Ground);
            CreateCube("Wall_West", parent, new Vector3(-11f, 1.5f, 2f), new Vector3(0.5f, 3f, 22f), Quaternion.identity, materials.Wall);
            CreateCube("Wall_East", parent, new Vector3(11f, 1.5f, 2f), new Vector3(0.5f, 3f, 22f), Quaternion.identity, materials.Wall);
            CreateCube("Wall_North", parent, new Vector3(0f, 1.5f, 13f), new Vector3(22f, 3f, 0.5f), Quaternion.identity, materials.Wall);
            CreateCube("Wall_South", parent, new Vector3(0f, 1.5f, -9f), new Vector3(22f, 3f, 0.5f), Quaternion.identity, materials.Wall);
            CreateCube("DashCollisionWall", parent, new Vector3(4.5f, 1f, -3.5f), new Vector3(0.45f, 2f, 5f), Quaternion.identity, materials.Wall);
            CreateCube("CornerWall_A", parent, new Vector3(-5f, 1f, 6f), new Vector3(5f, 2f, 0.45f), Quaternion.identity, materials.Wall);
            CreateCube("CornerWall_B", parent, new Vector3(-7.25f, 1f, 8f), new Vector3(0.45f, 2f, 4f), Quaternion.identity, materials.Wall);

            CreateCube(
                "Ramp",
                parent,
                new Vector3(6.2f, 0.55f, 4.5f),
                new Vector3(3f, 0.35f, 5f),
                Quaternion.Euler(-12f, 0f, 0f),
                materials.Ramp);
            CreateCube("RampPlatform", parent, new Vector3(6.2f, 1.1f, 7.4f), new Vector3(3.5f, 0.35f, 2.5f), Quaternion.identity, materials.Ramp);

            Transform stairs = CreateObject("ShortStairs", parent).transform;
            for (int index = 0; index < 5; index++)
            {
                float height = (index + 1) * 0.18f;
                CreateCube(
                    "Step_" + (index + 1),
                    stairs,
                    new Vector3(-6f, height * 0.5f, -3.2f + index * 0.62f),
                    new Vector3(3f, height, 0.65f),
                    Quaternion.identity,
                    materials.Stairs);
            }

            CreateCube("ZoneFloor", parent, new Vector3(0f, 0.03f, 7.5f), new Vector3(6f, 0.08f, 4.5f), Quaternion.identity, materials.ZoneFloor);
            CreateCube("ZoneWall_Left", parent, new Vector3(-3f, 1f, 7.5f), new Vector3(0.25f, 2f, 4.5f), Quaternion.identity, materials.Wall);
            CreateCube("ZoneWall_Right", parent, new Vector3(3f, 1f, 7.5f), new Vector3(0.25f, 2f, 4.5f), Quaternion.identity, materials.Wall);
        }

        private static PrototypeInteractable CreateNpc(
            GameObject prefab,
            Scene targetScene,
            Transform parent,
            string objectName,
            Vector3 position,
            string stableId,
            string displayName,
            string[] lines,
            DialogueFocusController dialogueController)
        {
            GameObject instance = InstantiatePrefab(prefab, targetScene, parent, objectName);
            instance.transform.position = position;
            PrototypeInteractable interactable = instance.GetComponent<PrototypeInteractable>();
            ConfigureInteractable(
                interactable,
                stableId,
                displayName,
                "대화",
                dialogueController,
                instance.GetComponent<PlayerFacing>(),
                lines);
            return interactable;
        }

        private static PrototypeInteractable CreateInspectObject(
            GameObject prefab,
            Scene targetScene,
            Transform parent,
            DialogueFocusController dialogueController)
        {
            GameObject instance = InstantiatePrefab(prefab, targetScene, parent, "Inspect_SealedArchive");
            instance.transform.position = new Vector3(-1.7f, 0f, -1.2f);
            PrototypeInteractable interactable = instance.GetComponent<PrototypeInteractable>();
            ConfigureInteractable(
                interactable,
                "inspect-sealed-archive",
                "봉인된 기록함",
                "조사",
                dialogueController,
                null,
                new[] { "오래된 봉인이 남아 있다. 지금은 열 수 없다." });
            return interactable;
        }

        private static void ConfigureInteractable(
            PrototypeInteractable interactable,
            string stableId,
            string displayName,
            string actionLabel,
            DialogueFocusController dialogueController,
            PlayerFacing facing,
            string[] lines)
        {
            SerializedObject serialized = new SerializedObject(interactable);
            SetString(serialized, "stableId", stableId);
            SetString(serialized, "displayName", displayName);
            SetString(serialized, "actionLabel", actionLabel);
            SetObject(serialized, "dialogueController", dialogueController);
            SetObject(serialized, "facing", facing);
            SetStringArray(serialized, "dialogueLines", lines);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(interactable);
        }

        private static void ConfigureSensor(
            InteractionSensor sensor,
            ExplorationInputReader inputReader,
            PlayerFacing facing,
            Transform playerRoot,
            Transform sensorOrigin,
            InteractionPromptView promptView)
        {
            SerializedObject serialized = new SerializedObject(sensor);
            SetObject(serialized, "inputReader", inputReader);
            SetObject(serialized, "playerFacing", facing);
            SetObject(serialized, "playerRoot", playerRoot);
            SetObject(serialized, "sensorOrigin", sensorOrigin);
            SetObject(serialized, "promptView", promptView);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(sensor);
        }

        private static void ConfigureDialogue(
            DialogueFocusController dialogue,
            ExplorationInputReader inputReader,
            PlayerFacing facing,
            Transform playerRoot,
            QuarterViewCameraRig cameraRig)
        {
            SerializedObject serialized = new SerializedObject(dialogue);
            SetObject(serialized, "inputReader", inputReader);
            SetObject(serialized, "playerFacing", facing);
            SetObject(serialized, "playerRoot", playerRoot);
            SetObject(serialized, "cameraRig", cameraRig);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(dialogue);
        }

        private static PrototypeMaterials CreateMaterials()
        {
            return new PrototypeMaterials(
                CreateMaterial("Ground", new Color(0.11f, 0.15f, 0.18f)),
                CreateMaterial("Wall", new Color(0.18f, 0.19f, 0.23f)),
                CreateMaterial("Ramp", new Color(0.22f, 0.26f, 0.31f)),
                CreateMaterial("Stairs", new Color(0.25f, 0.22f, 0.27f)),
                CreateMaterial("ZoneFloor", new Color(0.16f, 0.12f, 0.22f)),
                CreateMaterial("Player", new Color(0.16f, 0.58f, 0.88f)),
                CreateMaterial("Npc", new Color(0.65f, 0.24f, 0.32f)),
                CreateMaterial("Inspect", new Color(0.62f, 0.46f, 0.18f)),
                CreateMaterial("Facing", new Color(0.25f, 0.95f, 1f), true),
                CreateMaterial("Selection", new Color(1f, 0.74f, 0.18f), true));
        }

        private static PrototypePrefabs CreatePrefabs(PrototypeMaterials materials)
        {
            GameObject player = CreatePlayerTemplate(materials);
            GameObject playerPrefab = SavePrefab(player, PrefabFolder + "/PrototypePlayer.prefab");

            GameObject npc = CreateNpcTemplate(materials);
            GameObject npcPrefab = SavePrefab(npc, PrefabFolder + "/PrototypeNpc.prefab");

            GameObject inspect = CreateInspectTemplate(materials);
            GameObject inspectPrefab = SavePrefab(inspect, PrefabFolder + "/PrototypeInspectObject.prefab");

            GameObject cameraRig = CreateCameraTemplate();
            GameObject cameraPrefab = SavePrefab(cameraRig, PrefabFolder + "/PrototypeCameraRig.prefab");

            GameObject hud = CreateHudTemplate();
            GameObject hudPrefab = SavePrefab(hud, PrefabFolder + "/PrototypeHud.prefab");
            return new PrototypePrefabs(playerPrefab, npcPrefab, inspectPrefab, cameraPrefab, hudPrefab);
        }

        private static GameObject CreatePlayerTemplate(PrototypeMaterials materials)
        {
            GameObject root = new GameObject("PrototypePlayer");
            CharacterController controller = root.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.slopeLimit = 45f;
            controller.stepOffset = 0.3f;

            root.AddComponent<ExplorationInputReader>();
            PlayerFacing facing = root.AddComponent<PlayerFacing>();
            root.AddComponent<PlayerMotor>();
            root.AddComponent<InteractionSensor>();

            Transform visualRoot = CreateObject("VisualRoot", root.transform).transform;
            GameObject body = CreatePrimitiveWithoutCollider("Body", PrimitiveType.Capsule, visualRoot, materials.Player);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            GameObject marker = CreatePrimitiveWithoutCollider("FacingMarker", PrimitiveType.Cube, visualRoot, materials.Facing);
            marker.transform.localPosition = new Vector3(0f, 0.8f, 0.52f);
            marker.transform.localScale = new Vector3(0.15f, 0.12f, 0.65f);
            facing.SetVisualRoot(visualRoot);

            Transform sensorOrigin = CreateObject("SensorOrigin", root.transform).transform;
            sensorOrigin.localPosition = new Vector3(0f, 1.05f, 0f);
            SerializedObject facingSerialized = new SerializedObject(facing);
            facingSerialized.FindProperty("initialDirection").enumValueIndex = (int)FacingDirection8.North;
            facingSerialized.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        private static GameObject CreateNpcTemplate(PrototypeMaterials materials)
        {
            GameObject root = new GameObject("PrototypeNpc");
            SetLayerRecursively(root, Physics.IgnoreRaycastLayer);
            CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 1f, 0f);
            collider.height = 2f;
            collider.radius = 0.42f;
            PlayerFacing facing = root.AddComponent<PlayerFacing>();
            PrototypeInteractable interactable = root.AddComponent<PrototypeInteractable>();

            Transform visualRoot = CreateObject("VisualRoot", root.transform).transform;
            GameObject body = CreatePrimitiveWithoutCollider("Body", PrimitiveType.Capsule, visualRoot, materials.Npc);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);
            body.transform.localScale = new Vector3(0.82f, 1f, 0.82f);
            GameObject face = CreatePrimitiveWithoutCollider("FacingMarker", PrimitiveType.Cube, visualRoot, materials.Facing);
            face.transform.localPosition = new Vector3(0f, 1.15f, 0.48f);
            face.transform.localScale = new Vector3(0.12f, 0.12f, 0.42f);
            facing.SetVisualRoot(visualRoot);

            Transform focus = CreateObject("FocusPoint", root.transform).transform;
            focus.localPosition = new Vector3(0f, 1.3f, 0f);
            Transform anchor = CreateObject("DialogueCameraAnchor", root.transform).transform;
            anchor.localPosition = new Vector3(0f, 3f, -4.5f);
            GameObject selection = CreatePrimitiveWithoutCollider("SelectionMarker", PrimitiveType.Cylinder, root.transform, materials.Selection);
            selection.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            selection.transform.localScale = new Vector3(0.8f, 0.025f, 0.8f);
            selection.SetActive(false);

            SerializedObject serialized = new SerializedObject(interactable);
            SetObject(serialized, "focusPoint", focus);
            SetObject(serialized, "selectionMarker", selection);
            SetObject(serialized, "facing", facing);
            SetObject(serialized, "dialogueCameraAnchor", anchor);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            SetLayerRecursively(root, Physics.IgnoreRaycastLayer);
            return root;
        }

        private static GameObject CreateInspectTemplate(PrototypeMaterials materials)
        {
            GameObject root = new GameObject("PrototypeInspectObject");
            SetLayerRecursively(root, Physics.IgnoreRaycastLayer);
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.55f, 0f);
            collider.size = new Vector3(1.1f, 1.1f, 1.1f);
            PrototypeInteractable interactable = root.AddComponent<PrototypeInteractable>();

            GameObject body = CreatePrimitiveWithoutCollider("Body", PrimitiveType.Cube, root.transform, materials.Inspect);
            body.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            body.transform.localScale = new Vector3(1.05f, 1.05f, 1.05f);
            Transform focus = CreateObject("FocusPoint", root.transform).transform;
            focus.localPosition = new Vector3(0f, 1.1f, 0f);
            Transform anchor = CreateObject("DialogueCameraAnchor", root.transform).transform;
            anchor.localPosition = new Vector3(0f, 2.5f, -4f);
            GameObject selection = CreatePrimitiveWithoutCollider("SelectionMarker", PrimitiveType.Cylinder, root.transform, materials.Selection);
            selection.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            selection.transform.localScale = new Vector3(0.75f, 0.025f, 0.75f);
            selection.SetActive(false);

            SerializedObject serialized = new SerializedObject(interactable);
            SetObject(serialized, "focusPoint", focus);
            SetObject(serialized, "selectionMarker", selection);
            SetObject(serialized, "dialogueCameraAnchor", anchor);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            SetLayerRecursively(root, Physics.IgnoreRaycastLayer);
            return root;
        }

        private static GameObject CreateCameraTemplate()
        {
            GameObject root = new GameObject("PrototypeCameraRig");
            root.AddComponent<QuarterViewCameraRig>();
            GameObject cameraObject = CreateObject("Main Camera", root.transform);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 8f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.032f, 0.05f);
            cameraObject.AddComponent<AudioListener>();
            return root;
        }

        private static GameObject CreateHudTemplate()
        {
            FrontendUiTheme theme = new FrontendUiTheme();
            GameObject canvasObject = new GameObject("PrototypeHud", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject promptPanel = CreateUiPanel("InteractionPrompt", canvasObject.transform, new Color(0.025f, 0.035f, 0.055f, 0.92f));
            RectTransform promptRect = promptPanel.GetComponent<RectTransform>();
            SetRect(promptRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 72f), new Vector2(440f, 92f));
            CanvasGroup promptGroup = promptPanel.AddComponent<CanvasGroup>();
            promptGroup.alpha = 0f;
            Text targetLabel = CreateText("Target", promptPanel.transform, theme, 24, TextAnchor.MiddleLeft, new Vector2(28f, 47f), new Vector2(270f, 32f));
            Text promptLabel = CreateText("Prompt", promptPanel.transform, theme, 24, TextAnchor.MiddleRight, new Vector2(292f, 47f), new Vector2(120f, 32f));
            InteractionPromptView prompt = promptPanel.AddComponent<InteractionPromptView>();
            SerializedObject promptSerialized = new SerializedObject(prompt);
            SetObject(promptSerialized, "canvasGroup", promptGroup);
            SetObject(promptSerialized, "targetLabel", targetLabel);
            SetObject(promptSerialized, "promptLabel", promptLabel);
            promptSerialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject dialoguePanel = CreateUiPanel("DialoguePanel", canvasObject.transform, new Color(0.035f, 0.028f, 0.045f, 0.96f));
            RectTransform dialogueRect = dialoguePanel.GetComponent<RectTransform>();
            SetRect(dialogueRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 145f), new Vector2(1040f, 230f));
            CanvasGroup dialogueGroup = dialoguePanel.AddComponent<CanvasGroup>();
            dialogueGroup.alpha = 0f;
            Text speaker = CreateText("Speaker", dialoguePanel.transform, theme, 30, TextAnchor.MiddleLeft, new Vector2(42f, 180f), new Vector2(420f, 42f));
            speaker.color = theme.Focus;
            Text line = CreateText("Line", dialoguePanel.transform, theme, 27, TextAnchor.UpperLeft, new Vector2(42f, 112f), new Vector2(956f, 110f));
            Text hint = CreateText("Hint", dialoguePanel.transform, theme, 18, TextAnchor.MiddleRight, new Vector2(690f, 27f), new Vector2(308f, 28f));
            hint.text = "F / Enter 다음   Esc 닫기";

            DialogueFocusController dialogue = canvasObject.AddComponent<DialogueFocusController>();
            SerializedObject dialogueSerialized = new SerializedObject(dialogue);
            SetObject(dialogueSerialized, "dialogueCanvasGroup", dialogueGroup);
            SetObject(dialogueSerialized, "speakerLabel", speaker);
            SetObject(dialogueSerialized, "lineLabel", line);
            dialogueSerialized.ApplyModifiedPropertiesWithoutUndo();
            return canvasObject;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            FrontendUiTheme theme,
            int fontSize,
            TextAnchor alignment,
            Vector2 position,
            Vector2 size)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.font = theme.Font;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.color = theme.Paper;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            SetRect(textObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero, position, size);
            return text;
        }

        private static GameObject CreateUiPanel(string name, Transform parent, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            Image image = panel.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return panel;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = anchorMin == Vector2.zero && anchorMax == Vector2.zero
                ? new Vector2(0f, 0.5f)
                : new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static GameObject SavePrefab(GameObject template, string path)
        {
            GameObject asset = PrefabUtility.SaveAsPrefabAsset(template, path);
            UnityEngine.Object.DestroyImmediate(template);
            if (asset == null)
            {
                throw new InvalidOperationException("Could not save prototype prefab: " + path);
            }

            return asset;
        }

        private static GameObject InstantiatePrefab(GameObject prefab, Scene scene, Transform parent, string name)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            return instance;
        }

        private static Material CreateMaterial(string name, Color color, bool emission = false)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (emission && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.5f);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreateCube(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Quaternion rotation,
            Material material)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.position = position;
            cube.transform.rotation = rotation;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            return cube;
        }

        private static GameObject CreatePrimitiveWithoutCollider(
            string name,
            PrimitiveType primitiveType,
            Transform parent,
            Material material)
        {
            GameObject primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            Collider collider = primitive.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            primitive.GetComponent<Renderer>().sharedMaterial = material;
            return primitive;
        }

        private static void FaceAnchorToward(Transform anchor, Vector3 focus)
        {
            if (anchor == null)
            {
                return;
            }

            Vector3 direction = focus - anchor.position;
            if (direction.sqrMagnitude > 0.0001f)
            {
                anchor.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }
        }

        private static GameObject GetOrCreateSceneRoot(Scene scene)
        {
            GameObject[] matches = scene.GetRootGameObjects().Where(root => root.name == SceneRootName).ToArray();
            if (matches.Length > 1)
            {
                throw new InvalidOperationException("Multiple " + SceneRootName + " objects exist in " + ScenePath + ".");
            }

            if (matches.Length == 1)
            {
                return matches[0];
            }

            GameObject rootObject = new GameObject(SceneRootName);
            SceneManager.MoveGameObjectToScene(rootObject, scene);
            rootObject.transform.SetAsFirstSibling();
            return rootObject;
        }

        private static void RemovePreviousGeneratedRoot(Transform sceneRoot)
        {
            Transform existing = sceneRoot.Find(GeneratedRootName);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }
        }

        private static Scene FindLoadedScene(string assetPath)
        {
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                Scene scene = SceneManager.GetSceneAt(index);
                if (string.Equals(scene.path, assetPath, StringComparison.Ordinal))
                {
                    return scene;
                }
            }

            return default;
        }

        private static void ValidateScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                throw new InvalidOperationException("The GameShell scene is not loaded for validation.");
            }

            GameObject[] roots = scene.GetRootGameObjects();
            GameShellRoot[] compositionRoots = roots
                .Select(root => root.GetComponent<GameShellRoot>())
                .Where(component => component != null)
                .ToArray();
            if (compositionRoots.Length != 1)
            {
                throw new InvalidOperationException("The GameShell scene must have exactly one root-level GameShellRoot.");
            }

            SpawnPoint[] spawnPoints = roots.SelectMany(root => root.GetComponentsInChildren<SpawnPoint>(true)).ToArray();
            if (spawnPoints.Count(point => string.Equals(point.SpawnKey, "start", StringComparison.Ordinal)) != 1)
            {
                throw new InvalidOperationException("The GameShell scene must have exactly one start SpawnPoint.");
            }

            Camera[] cameras = roots.SelectMany(root => root.GetComponentsInChildren<Camera>(true)).ToArray();
            if (cameras.Length != 1 || !cameras[0].orthographic)
            {
                throw new InvalidOperationException("The GameShell scene must contain one orthographic camera.");
            }

            int missingScripts = 0;
            foreach (GameObject root in roots)
            {
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    missingScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject);
                }
            }

            if (missingScripts != 0)
            {
                throw new InvalidOperationException("The GameShell scene contains " + missingScripts + " missing script reference(s).");
            }

            if (roots.SelectMany(root => root.GetComponentsInChildren<PrototypeInteractable>(true)).Count() < 3)
            {
                throw new InvalidOperationException("The GameShell scene requires two NPCs and one inspectable object.");
            }

            if (roots.SelectMany(root => root.GetComponentsInChildren<CameraZone>(true)).Count() < 1)
            {
                throw new InvalidOperationException("The GameShell scene requires a CameraZone.");
            }
        }

        private static void EnsureAssetFolder(string path)
        {
            string[] segments = path.Split('/');
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

        private static GameObject CreateObject(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name);
            if (parent != null)
            {
                gameObject.transform.SetParent(parent, false);
            }

            return gameObject;
        }

        private static T GetOrAdd<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            root.layer = layer;
            foreach (Transform child in root.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private static void SetObject(SerializedObject serialized, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName)
                ?? throw new InvalidOperationException(serialized.targetObject.GetType().Name + "." + propertyName + " was not found.");
            property.objectReferenceValue = value;
        }

        private static void SetString(SerializedObject serialized, string propertyName, string value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName)
                ?? throw new InvalidOperationException(serialized.targetObject.GetType().Name + "." + propertyName + " was not found.");
            property.stringValue = value;
        }

        private static void SetStringArray(SerializedObject serialized, string propertyName, IReadOnlyList<string> values)
        {
            SerializedProperty property = serialized.FindProperty(propertyName)
                ?? throw new InvalidOperationException(serialized.targetObject.GetType().Name + "." + propertyName + " was not found.");
            property.arraySize = values == null ? 0 : values.Count;
            for (int index = 0; index < property.arraySize; index++)
            {
                property.GetArrayElementAtIndex(index).stringValue = values[index];
            }
        }

        private static void MarkDirty(params UnityEngine.Object[] objects)
        {
            foreach (UnityEngine.Object target in objects)
            {
                if (target != null)
                {
                    EditorUtility.SetDirty(target);
                }
            }
        }

        private readonly struct PrototypeMaterials
        {
            public PrototypeMaterials(
                Material ground,
                Material wall,
                Material ramp,
                Material stairs,
                Material zoneFloor,
                Material player,
                Material npc,
                Material inspect,
                Material facing,
                Material selection)
            {
                Ground = ground;
                Wall = wall;
                Ramp = ramp;
                Stairs = stairs;
                ZoneFloor = zoneFloor;
                Player = player;
                Npc = npc;
                Inspect = inspect;
                Facing = facing;
                Selection = selection;
            }

            public Material Ground { get; }
            public Material Wall { get; }
            public Material Ramp { get; }
            public Material Stairs { get; }
            public Material ZoneFloor { get; }
            public Material Player { get; }
            public Material Npc { get; }
            public Material Inspect { get; }
            public Material Facing { get; }
            public Material Selection { get; }
        }

        private readonly struct PrototypePrefabs
        {
            public PrototypePrefabs(GameObject player, GameObject npc, GameObject inspectObject, GameObject cameraRig, GameObject hud)
            {
                Player = player;
                Npc = npc;
                InspectObject = inspectObject;
                CameraRig = cameraRig;
                Hud = hud;
            }

            public GameObject Player { get; }
            public GameObject Npc { get; }
            public GameObject InspectObject { get; }
            public GameObject CameraRig { get; }
            public GameObject Hud { get; }
        }
    }
}
