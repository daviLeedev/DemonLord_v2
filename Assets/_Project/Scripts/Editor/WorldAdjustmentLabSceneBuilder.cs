using System;
using System.Collections.Generic;
using System.Linq;
using DemonLord.Presentation;
using DemonLord.Presentation.Combat;
using DemonLord.Presentation.Exploration;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DemonLord.Editor
{
    public static class WorldAdjustmentLabSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/90_GameShell.unity";
        private const string SceneRootName = "GameShellSceneRoot";
        private const string GeneratedRootName = "__WorldAdjustmentLabGenerated";
        private const string PreviousPrototypeRootName = "__ExplorationPrototypeGenerated";
        private const string PrefabFolder = "Assets/_Project/Prefabs/Exploration/WorldAdjustmentLab";
        private const string ScriptableObjectFolder = "Assets/_Project/ScriptableObjects/Exploration";
        private const string MaterialFolder = "Assets/_Project/Art/Prototype/WorldAdjustmentLab/Materials";
        private const string TaxOfficerSpriteFolder = "Assets/_Project/Art/Characters/TaxOfficer/Placeholder";
        private const string DialoguePortraitFolder = "Assets/_Project/Art/Dialogue/Portraits";
        private const string DialogueArtFolder = "Assets/_Project/Art/UI/Dialogue";
        private const string CombatTrainingLineupPath = "Assets/_Project/Art/Combat/combat_training_lineup_v1.png";
        private const string CombatTrainingBackdropPath = "Assets/_Project/Art/Combat/combat_training_backdrop_v1.png";
        private const string TaxOfficerPortraitPath = DialoguePortraitFolder + "/tax_officer_dialogue_profile_v2.png";
        private const string ResearcherPortraitPath = DialoguePortraitFolder + "/worldline_researcher_dialogue_profile_v2.png";
        private const string SealPath = "Assets/_Project/Art/Prototype/WorldAdjustmentLab/bureau_seal_decal.png";
        private const string TextureKitFolder = "Assets/_Project/Art/Prototype/WorldAdjustmentLab/TextureKit";
        private const string SaveCompleteSfxPath = "Assets/_Project/Resources/Audio/Ui/ui_save_complete_01.wav";
        private const float CameraZoomSensitivity = 0.03f;

        [MenuItem("DemonLord/Exploration/Build World Adjustment Lab")]
        public static void BuildWorldAdjustmentLab()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Stop Play Mode before building the World Adjustment Lab.");
            }

            Scene previousScene = SceneManager.GetActiveScene();
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
                EnsureAssetFolder(ScriptableObjectFolder);
                EnsureAssetFolder(MaterialFolder);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                LabAssets assets = CreateAssets();
                GameObject sceneRoot = GetOrCreateSceneRoot(targetScene);
                RemoveGeneratedRoot(sceneRoot.transform, GeneratedRootName);
                GameObject generatedRoot = CreateObject(GeneratedRootName, sceneRoot.transform);
                BuildScene(targetScene, sceneRoot, generatedRoot, assets);

                Transform previousPrototype = sceneRoot.transform.Find(PreviousPrototypeRootName);
                if (previousPrototype != null)
                {
                    previousPrototype.gameObject.SetActive(false);
                }

                EditorSceneManager.MarkSceneDirty(targetScene);
                if (!EditorSceneManager.SaveScene(targetScene, ScenePath, false))
                {
                    throw new InvalidOperationException("Unity could not save the World Adjustment Lab scene.");
                }

                AssetDatabase.SaveAssets();
                ValidateScene(targetScene);
                Debug.Log("World Adjustment Lab built and validated: " + ScenePath);
            }
            finally
            {
                if (previousScene.IsValid() && previousScene.isLoaded)
                {
                    SceneManager.SetActiveScene(previousScene);
                }

                if (openedForBuild && targetScene.IsValid() && targetScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(targetScene, true);
                }
            }
        }

        [MenuItem("DemonLord/Exploration/Validate World Adjustment Lab")]
        public static void ValidateWorldAdjustmentLab()
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
                Debug.Log("World Adjustment Lab validation passed: " + ScenePath);
            }
            finally
            {
                if (openedForValidation && targetScene.IsValid() && targetScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(targetScene, true);
                }
            }
        }

        private static LabAssets CreateAssets()
        {
            DialogueVisualAssets dialogueVisuals = CreateDialogueVisualAssets();
            Sprite bureauSeal = LoadSingleSprite(SealPath);
            DirectionalAnimationSet animationSet = CreateDirectionalAnimationSet();
            DialogueTheme dialogueTheme = CreateDialogueTheme();
            DialogueSequence researcherSequence = CreateResearcherDialogue(dialogueVisuals.TaxOfficerPortrait, dialogueVisuals.ResearcherPortrait);
            DialogueSequence combatLiaisonSequence = CreateCombatLiaisonDialogue(dialogueVisuals.TaxOfficerPortrait);
            LabMaterials materials = CreateMaterials();
            TaxOfficerModelPackage taxOfficerModel = TaxOfficerModelAssetBuilder.Prepare();
            LabPrefabs prefabs = CreatePrefabs(materials, taxOfficerModel);
            return new LabAssets(animationSet, dialogueTheme, researcherSequence, combatLiaisonSequence, dialogueVisuals, bureauSeal, materials, prefabs);
        }

        private static DialogueVisualAssets CreateDialogueVisualAssets()
        {
            return new DialogueVisualAssets(
                LoadSingleSprite(TaxOfficerPortraitPath),
                LoadSingleSprite(ResearcherPortraitPath),
                LoadSingleSprite(DialogueArtFolder + "/dialogue_panel_wide_v2.png"),
                LoadSingleSprite(DialogueArtFolder + "/dialogue_nameplate_v2.png"),
                LoadSingleSprite(DialogueArtFolder + "/dialogue_continue_prompt_v2.png"),
                LoadSingleSprite(DialogueArtFolder + "/dialogue_choice_button_normal_v2.png"),
                LoadSingleSprite(DialogueArtFolder + "/dialogue_choice_button_selected_v2.png"),
                LoadSingleSprite(DialogueArtFolder + "/dialogue_choice_button_disabled_v2.png"),
                LoadSingleSprite(DialogueArtFolder + "/dialogue_back_button_v2.png"));
        }

        private static DirectionalAnimationSet CreateDirectionalAnimationSet()
        {
            const string assetPath = ScriptableObjectFolder + "/TaxOfficerPlaceholderDirectionalAnimationSet.asset";
            DirectionalAnimationSet set = AssetDatabase.LoadAssetAtPath<DirectionalAnimationSet>(assetPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<DirectionalAnimationSet>();
                AssetDatabase.CreateAsset(set, assetPath);
            }

            string[] states = { "idle", "walk", "run", "dash" };
            string[] directions = { "n", "ne", "e", "se", "s", "sw", "w", "nw" };
            Sprite[][][] stateFrames = new Sprite[states.Length][][];
            for (int stateIndex = 0; stateIndex < states.Length; stateIndex++)
            {
                stateFrames[stateIndex] = new Sprite[directions.Length][];
                for (int directionIndex = 0; directionIndex < directions.Length; directionIndex++)
                {
                    string path = TaxOfficerSpriteFolder + "/tax_officer_" + states[stateIndex] + "_" + directions[directionIndex] + ".png";
                    Sprite sprite = LoadSingleSprite(path);
                    stateFrames[stateIndex][directionIndex] = new[] { sprite };
                }
            }

            set.Configure(stateFrames[0], stateFrames[1], stateFrames[2], stateFrames[3]);
            EditorUtility.SetDirty(set);
            return set;
        }

        private static DialogueTheme CreateDialogueTheme()
        {
            const string assetPath = ScriptableObjectFolder + "/WorldAdjustmentLabDialogueTheme.asset";
            DialogueTheme theme = AssetDatabase.LoadAssetAtPath<DialogueTheme>(assetPath);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<DialogueTheme>();
                AssetDatabase.CreateAsset(theme, assetPath);
            }

            theme.Configure(new FrontendUiTheme().Font);
            EditorUtility.SetDirty(theme);
            return theme;
        }

        private static DialogueSequence CreateResearcherDialogue(Sprite taxOfficerPortrait, Sprite researcherPortrait)
        {
            const string assetPath = ScriptableObjectFolder + "/WorldlineResearcherIntroduction.asset";
            DialogueSequence sequence = AssetDatabase.LoadAssetAtPath<DialogueSequence>(assetPath);
            if (sequence == null)
            {
                sequence = ScriptableObject.CreateInstance<DialogueSequence>();
                AssetDatabase.CreateAsset(sequence, assetPath);
            }

            DialogueParticipant player = new DialogueParticipant();
            player.Configure("tax-officer", "세무관", taxOfficerPortrait);
            DialogueParticipant partner = new DialogueParticipant();
            partner.Configure("worldline-researcher", "세계선 분석 연구원", researcherPortrait);
            DialogueLine[] lines =
            {
                CreateDialogueLine(DialogueSpeakerSide.Partner, "세무관님, 제3관측실의 세계선 변동치가 다시 상승하고 있습니다."),
                CreateDialogueLine(DialogueSpeakerSide.Player, "승인되지 않은 조정 기록부터 분리해 주세요. 현장은 제가 확인하겠습니다."),
                CreateDialogueLine(DialogueSpeakerSide.Partner, "격리 연구실은 아직 봉쇄 상태입니다. 접근 허가 없이 문을 열 수 없습니다."),
                CreateDialogueLine(DialogueSpeakerSide.Player, "알겠습니다. 우선 공개된 기록부터 검토하죠."),
            };
            sequence.Configure(player, partner, lines);
            EditorUtility.SetDirty(sequence);
            return sequence;
        }

        private static DialogueSequence CreateCombatLiaisonDialogue(Sprite taxOfficerPortrait)
        {
            const string assetPath = ScriptableObjectFolder + "/CombatLiaisonBriefing.asset";
            DialogueSequence sequence = AssetDatabase.LoadAssetAtPath<DialogueSequence>(assetPath);
            if (sequence == null)
            {
                sequence = ScriptableObject.CreateInstance<DialogueSequence>();
                AssetDatabase.CreateAsset(sequence, assetPath);
            }

            DialogueParticipant player = new DialogueParticipant();
            player.Configure("tax-officer", "세무관", taxOfficerPortrait);
            DialogueParticipant partner = new DialogueParticipant();
            // The combat liaison portrait is intentionally left empty until its final art is supplied.
            partner.Configure("combat-liaison-officer", "전투 대응 집행관", null);
            DialogueLine[] lines =
            {
                CreateDialogueLine(DialogueSpeakerSide.Partner, "세무관님, 조정 대상의 적대 반응이 확인됐습니다."),
                CreateDialogueLine(DialogueSpeakerSide.Partner, "좌측의 작은 아군 목록에서 대상을 고르면 바로 옆에 그 인원의 기술이 표시됩니다. 붉은 곡선 화살은 적의 공개 행동입니다."),
                CreateDialogueLine(DialogueSpeakerSide.Partner, "공유 SP 안에서 각자 기술을 고르면, 기술의 속도에 따라 아군과 적의 행동선이 다시 정렬됩니다. 다음을 누르면 시작합니다."),
            };
            sequence.Configure(player, partner, lines);
            EditorUtility.SetDirty(sequence);
            return sequence;
        }

        private static DialogueLine CreateDialogueLine(DialogueSpeakerSide side, string text)
        {
            DialogueLine line = new DialogueLine();
            line.Configure(side, text);
            return line;
        }

        private static LabMaterials CreateMaterials()
        {
            return new LabMaterials(
                CreateTexturedMaterial("ReceptionFloor", TextureKitFolder + "/tiles_slate_brass.png", new Vector2(2.5f, 2f), true),
                CreateTexturedMaterial("OfficeFloor", TextureKitFolder + "/tiles_slate_brass.png", new Vector2(2f, 2f), true),
                CreateTexturedMaterial("AnalysisFloor", TextureKitFolder + "/tiles_basalt_cyan.png", new Vector2(2f, 2f), true),
                CreateTexturedMaterial("ArchiveFloor", TextureKitFolder + "/tiles_burgundy_marble.png", new Vector2(2f, 2f), true),
                CreateTexturedMaterial("RestrictedFloor", TextureKitFolder + "/tiles_basalt_cyan.png", new Vector2(2f, 2f), true),
                CreateTexturedMaterial("WallStoneBrass", TextureKitFolder + "/walls_stone_brass.png", new Vector2(2.25f, 1f), true),
                CreateTexturedMaterial("WallArchiveBrick", TextureKitFolder + "/walls_archive_brick.png", new Vector2(2f, 1f), true),
                CreateTexturedMaterial("WallContainmentMetal", TextureKitFolder + "/walls_containment_metal.png", new Vector2(2f, 1f), true),
                CreateMaterial("Trim", new Color(0.38f, 0.32f, 0.20f)),
                CreateTexturedMaterial("DoorOfficial", TextureKitFolder + "/door_single_official.png", Vector2.one, false),
                CreateTexturedMaterial("DoorArchive", TextureKitFolder + "/door_double_archive.png", Vector2.one, false),
                CreateTexturedMaterial("DoorContainment", TextureKitFolder + "/door_containment_locked.png", Vector2.one, false),
                CreateTexturedMaterial("StairsSlateBrass", TextureKitFolder + "/stairs_slate_brass.png", new Vector2(1.2f, 1.2f), true),
                CreateTexturedMaterial("StairsIronCyan", TextureKitFolder + "/stairs_iron_cyan.png", new Vector2(1.2f, 1.2f), true),
                CreateTexturedMaterial("StairsMarbleIvory", TextureKitFolder + "/stairs_marble_ivory.png", new Vector2(1.2f, 1.2f), true),
                CreateMaterial("Furniture", new Color(0.20f, 0.16f, 0.12f)),
                CreateMaterial("Research", new Color(0.18f, 0.16f, 0.30f), true),
                CreateMaterial("Npc", new Color(0.52f, 0.34f, 0.69f)),
                CreateMaterial("Selection", new Color(0.92f, 0.72f, 0.22f), true));
        }

        private static LabPrefabs CreatePrefabs(LabMaterials materials, TaxOfficerModelPackage taxOfficerModel)
        {
            GameObject playerTemplate = CreatePlayerTemplate(taxOfficerModel);
            GameObject player = SavePrefab(playerTemplate, PrefabFolder + "/WorldAdjustmentLabPlayer.prefab");
            GameObject npcTemplate = CreateNpcTemplate(materials);
            GameObject npc = SavePrefab(npcTemplate, PrefabFolder + "/WorldAdjustmentLabNpc.prefab");
            return new LabPrefabs(player, npc);
        }

        private static GameObject CreatePlayerTemplate(TaxOfficerModelPackage taxOfficerModel)
        {
            GameObject root = new GameObject("WorldAdjustmentLabPlayer");
            CharacterController controller = root.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.slopeLimit = 45f;
            controller.stepOffset = 0.3f;
            root.AddComponent<ExplorationInputReader>();
            root.AddComponent<PlayerFacing>();
            root.AddComponent<PlayerMotor>();
            root.AddComponent<InteractionSensor>();

            Transform visual = CreateObject("ModelVisual", root.transform).transform;
            PlayerFacing facing = root.GetComponent<PlayerFacing>();
            facing.SetVisualRoot(visual);
            GameObject model = PrefabUtility.InstantiatePrefab(taxOfficerModel.ModelPrefab) as GameObject;
            if (model == null)
            {
                throw new InvalidOperationException("Could not instantiate the tax officer 3D model prefab.");
            }

            model.name = "TaxOfficer3DModel";
            model.transform.SetParent(visual, false);
            ApplyMaterial(model, taxOfficerModel.Material);
            Animator animator = model.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                throw new InvalidOperationException("The tax officer 3D model requires an Animator component.");
            }

            animator.runtimeAnimatorController = taxOfficerModel.AnimatorController;
            animator.applyRootMotion = false;
            TaxOfficerModelAnimator modelAnimator = animator.GetComponent<TaxOfficerModelAnimator>();
            if (modelAnimator == null)
            {
                modelAnimator = animator.gameObject.AddComponent<TaxOfficerModelAnimator>();
            }

            modelAnimator.Configure(
                animator,
                root.GetComponent<PlayerMotor>(),
                root.GetComponent<ExplorationInputReader>());

            Transform sensorOrigin = CreateObject("SensorOrigin", root.transform).transform;
            sensorOrigin.localPosition = new Vector3(0f, 1.05f, 0f);
            return root;
        }

        private static GameObject CreateNpcTemplate(LabMaterials materials)
        {
            GameObject root = new GameObject("WorldAdjustmentLabNpc");
            root.layer = Physics.IgnoreRaycastLayer;
            CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 1f, 0f);
            collider.height = 2f;
            collider.radius = 0.42f;
            PlayerFacing facing = root.AddComponent<PlayerFacing>();
            root.AddComponent<PrototypeInteractable>();

            Transform visualRoot = CreateObject("VisualRoot", root.transform).transform;
            GameObject body = CreateVisualCube("ResearcherBody", visualRoot, new Vector3(0f, 1f, 0f), new Vector3(0.7f, 1.7f, 0.55f), materials.Npc);
            GameObject marker = CreateVisualCube("FacingMarker", visualRoot, new Vector3(0f, 1.14f, 0.34f), new Vector3(0.1f, 0.1f, 0.32f), materials.Selection);
            facing.SetVisualRoot(visualRoot);
            Transform focus = CreateObject("FocusPoint", root.transform).transform;
            focus.localPosition = new Vector3(0f, 1.3f, 0f);
            Transform anchor = CreateObject("DialogueCameraAnchor", root.transform).transform;
            anchor.localPosition = new Vector3(0f, 3f, -4.5f);
            GameObject selection = CreatePrimitiveWithoutCollider("SelectionMarker", PrimitiveType.Cylinder, root.transform, materials.Selection);
            selection.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            selection.transform.localScale = new Vector3(0.8f, 0.025f, 0.8f);
            selection.SetActive(false);

            SetLayerRecursively(root, Physics.IgnoreRaycastLayer);
            body.layer = Physics.IgnoreRaycastLayer;
            marker.layer = Physics.IgnoreRaycastLayer;
            SerializedObject serialized = new SerializedObject(root.GetComponent<PrototypeInteractable>());
            SetObject(serialized, "focusPoint", focus);
            SetObject(serialized, "selectionMarker", selection);
            SetObject(serialized, "facing", facing);
            SetObject(serialized, "dialogueCameraAnchor", anchor);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        private static void BuildScene(Scene scene, GameObject sceneRoot, GameObject generatedRoot, LabAssets assets)
        {
            GameShellRoot shell = GetOrAdd<GameShellRoot>(sceneRoot);
            GameShellSessionView diagnostics = GetOrAdd<GameShellSessionView>(sceneRoot);

            Transform entryPoints = CreateObject("EntryPoints", generatedRoot.transform).transform;
            SpawnPoint start = CreateSpawnPoint(entryPoints, "start", new Vector3(0f, 0.05f, -3.25f));
            SpawnPoint researcherCheckpoint = CreateSpawnPoint(entryPoints, "researcher_briefed", new Vector3(11.2f, 0.05f, -2.55f));
            SpawnPoint ledgerCheckpoint = CreateSpawnPoint(entryPoints, "tax_ledger_reviewed", new Vector3(-11.2f, 0.05f, -2.35f));
            SpawnPoint archiveCheckpoint = CreateSpawnPoint(entryPoints, "archive_catalogued", new Vector3(0f, 0.05f, 7.15f));

            Transform gameplay = CreateObject("Gameplay", generatedRoot.transform).transform;
            GameObject player = InstantiatePrefab(assets.Prefabs.Player, scene, gameplay, "TaxOfficer");
            player.transform.SetPositionAndRotation(start.transform.position, start.transform.rotation);
            CharacterController controller = player.GetComponent<CharacterController>();
            ExplorationInputReader input = player.GetComponent<ExplorationInputReader>();
            PlayerFacing facing = player.GetComponent<PlayerFacing>();
            PlayerMotor motor = player.GetComponent<PlayerMotor>();
            InteractionSensor sensor = player.GetComponent<InteractionSensor>();
            LocationTracker locationTracker = GetOrAdd<LocationTracker>(sceneRoot);

            Transform cameraArea = CreateObject("CameraRig", generatedRoot.transform).transform;
            GameObject cameraObject = CreateObject("QuarterViewCameraRig", cameraArea);
            QuarterViewCameraRig cameraRig = cameraObject.AddComponent<QuarterViewCameraRig>();
            GameObject childCamera = CreateObject("Main Camera", cameraObject.transform);
            Camera gameCamera = childCamera.AddComponent<Camera>();
            gameCamera.tag = "MainCamera";
            gameCamera.orthographic = true;
            gameCamera.orthographicSize = 8f;
            gameCamera.nearClipPlane = 0.1f;
            gameCamera.farClipPlane = 100f;
            gameCamera.clearFlags = CameraClearFlags.SolidColor;
            gameCamera.backgroundColor = new Color(0.018f, 0.024f, 0.04f);
            childCamera.AddComponent<AudioListener>();
            ConfigureCameraAuthoring(cameraRig);
            cameraRig.Configure(gameCamera, player.transform, input, input.Gate);
            motor.ConfigureGroundSafety(0.02f);
            motor.Initialize(input, cameraRig, facing);

            HudReferences hud = BuildHud(scene, generatedRoot.transform, assets.DialogueTheme, assets.DialogueVisuals);
            hud.InGameUiCoordinator.Configure(input, hud.DialogueController, hud.PauseMenuView);
            hud.CombatTrainingCoordinator.Configure(
                hud.DialogueController,
                input,
                hud.InGameUiCoordinator,
                hud.CombatTrainingView);
            ConfigureSensor(sensor, input, facing, player.transform, player.transform.Find("SensorOrigin"), hud.Prompt);
            ConfigureDialogue(hud.DialogueController, input, facing, player.transform, cameraRig, hud.DialogueView, hud.DialogueCanvasGroup, hud.FallbackSpeaker, hud.Body);

            Transform environment = CreateObject("Environment", generatedRoot.transform).transform;
            BuildEnvironment(environment, assets.Materials, assets.BureauSeal);
            BuildLocationVolumes(environment, locationTracker);
            LabDoorController archiveAnnexDoor = BuildDoors(gameplay, assets.Materials, hud.Notification);
            PrototypeInteractable researcher = CreateResearcher(assets.Prefabs.Npc, scene, gameplay, hud.DialogueController, assets.ResearcherSequence);
            PrototypeInteractable combatLiaison = CreateCombatLiaisonOfficer(assets.Prefabs.Npc, scene, gameplay, hud.DialogueController, assets.CombatLiaisonSequence);
            PrototypeInteractable ledger = CreateLedger(gameplay, assets.Materials, hud.DialogueController);
            PrototypeInteractable analysisConsole = CreateInspectionNode(
                "WorldlineAnalysisConsole",
                "worldline-analysis-console",
                "세계선 분석 콘솔",
                gameplay,
                assets.Materials,
                hud.DialogueController,
                new Vector3(11.3f, 0f, 1.6f),
                new[] { "분석 파형이 불안정합니다. 연구원의 보고와 세무 기록을 교차 확인해야 합니다." });
            PrototypeInteractable archiveCatalog = CreateInspectionNode(
                "ArchiveCatalog",
                "archive-catalog",
                "기록보관실 분류 장부",
                gameplay,
                assets.Materials,
                hud.DialogueController,
                new Vector3(0f, 0f, 10.25f),
                new[] { "회수 기록이 세무 기록부의 누락 항목과 일치합니다.", "기록보관실 분류 장부에 조사 결과를 남겼습니다." });
            PrototypeInteractable annexCabinet = CreateInspectionNode(
                "SealedRecoveryCabinet",
                "sealed-recovery-cabinet",
                "봉인 회수함",
                gameplay,
                assets.Materials,
                hud.DialogueController,
                new Vector3(0f, 0f, 16.4f),
                new[] { "봉인 회수함은 상위 등급의 결재가 필요합니다." });
            FaceAnchorToward(researcher.DialogueCameraAnchor, researcher.FocusPoint.position);
            FaceAnchorToward(combatLiaison.DialogueCameraAnchor, combatLiaison.FocusPoint.position);

            LabProgressController progress = GetOrAdd<LabProgressController>(sceneRoot);
            progress.Configure(hud.DialogueController, hud.Notification, researcher, ledger, archiveCatalog, archiveAnnexDoor);

            BuildCameraZones(environment, cameraRig);
            BuildLighting(generatedRoot.transform);

            shell.Configure(
                player.transform,
                controller,
                gameCamera,
                input,
                facing,
                motor,
                cameraRig,
                sensor,
                diagnostics,
                new[] { start, researcherCheckpoint, ledgerCheckpoint, archiveCheckpoint },
                progress,
                hud.InGameHudView,
                locationTracker,
                hud.InGameUiCoordinator,
                configuredCombatTrainingCoordinator: hud.CombatTrainingCoordinator);

            MarkDirty(shell, diagnostics, progress, locationTracker, start, researcherCheckpoint, ledgerCheckpoint, archiveCheckpoint, controller, input, facing, motor, sensor, gameCamera, cameraRig, hud.DialogueController, hud.DialogueView, hud.Notification, hud.InGameHudView, hud.PauseMenuView, hud.InGameUiCoordinator, hud.CombatTrainingView, hud.CombatTrainingCoordinator, researcher, combatLiaison, ledger, analysisConsole, archiveCatalog, annexCabinet);
        }

        private static SpawnPoint CreateSpawnPoint(Transform parent, string key, Vector3 position)
        {
            SpawnPoint spawn = CreateObject("Spawn_" + key, parent).AddComponent<SpawnPoint>();
            spawn.Configure(key);
            spawn.transform.SetPositionAndRotation(position, Quaternion.identity);
            return spawn;
        }

        private static void BuildEnvironment(Transform parent, LabMaterials materials, Sprite bureauSeal)
        {
            Transform rooms = CreateObject("Rooms", parent).transform;
            CreateFloor("ReceptionFloor", rooms, new Vector3(0f, -0.25f, 0f), new Vector3(12f, 0.5f, 10f), materials.ReceptionFloor);
            CreateFloor("TaxOfficeFloor", rooms, new Vector3(-10f, -0.25f, 0f), new Vector3(8f, 0.5f, 8f), materials.OfficeFloor);
            CreateFloor("AnalysisFloor", rooms, new Vector3(10f, -0.25f, 0f), new Vector3(8f, 0.5f, 8f), materials.AnalysisFloor);
            CreateFloor("ArchiveFloor", rooms, new Vector3(0f, -0.25f, 9f), new Vector3(10f, 0.5f, 8f), materials.ArchiveFloor);
            CreateFloor("ArchiveAnnexFloor", rooms, new Vector3(0f, -0.25f, 16f), new Vector3(8f, 0.5f, 6f), materials.ArchiveFloor);
            CreateFloor("RestrictedFloor", rooms, new Vector3(0f, -0.25f, -9f), new Vector3(10f, 0.5f, 8f), materials.RestrictedFloor);

            CreateReceptionWalls(rooms, materials.WallStoneBrass, materials.Trim);
            CreateOfficeWalls(rooms, materials.WallStoneBrass, materials.Trim);
            CreateAnalysisWalls(rooms, materials.WallContainmentMetal, materials.Trim);
            CreateArchiveWalls(rooms, materials.WallArchiveBrick, materials.Trim);
            CreateRestrictedWalls(rooms, materials.WallContainmentMetal, materials.Trim);
            BuildFurniture(rooms, materials);

            if (bureauSeal != null)
            {
                GameObject seal = CreateObject("WorldAdjustmentBureauSeal", rooms);
                SpriteRenderer renderer = seal.AddComponent<SpriteRenderer>();
                renderer.sprite = bureauSeal;
                renderer.sortingOrder = 2;
                seal.transform.SetPositionAndRotation(new Vector3(0f, 2.45f, 4.73f), Quaternion.identity);
                seal.transform.localScale = Vector3.one * 2.1f;
            }
        }

        private static void BuildLocationVolumes(Transform parent, LocationTracker tracker)
        {
            const string AreaName = "세계조정국 연구실";
            CreateLocationVolume("Location_Reception", parent, new Vector3(0f, 1.5f, 0f), new Vector3(11.5f, 3f, 9.5f), "reception", AreaName, "중앙 접수실", 10, tracker);
            CreateLocationVolume("Location_TaxOffice", parent, new Vector3(-10f, 1.5f, 0f), new Vector3(7.5f, 3f, 7.5f), "tax_office", AreaName, "세무 집행실", 10, tracker);
            CreateLocationVolume("Location_Analysis", parent, new Vector3(10f, 1.5f, 0f), new Vector3(7.5f, 3f, 7.5f), "analysis_lab", AreaName, "세계선 분석실", 10, tracker);
            CreateLocationVolume("Location_Archive", parent, new Vector3(0f, 1.5f, 9f), new Vector3(9.5f, 3f, 7.5f), "archive", AreaName, "기록보관실", 10, tracker);
            CreateLocationVolume("Location_ArchiveAnnex", parent, new Vector3(0f, 1.5f, 16f), new Vector3(7.5f, 3f, 5.5f), "archive_annex", AreaName, "기록보관실 별관", 10, tracker);
            CreateLocationVolume("Location_Restricted", parent, new Vector3(0f, 1.5f, -9f), new Vector3(9.5f, 3f, 7.5f), "restricted", AreaName, "격리실", 10, tracker);
        }

        private static void CreateLocationVolume(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 size,
            string stableId,
            string areaName,
            string roomName,
            int priority,
            LocationTracker tracker)
        {
            GameObject root = CreateObject(name, parent);
            root.transform.localPosition = position;
            BoxCollider volume = root.AddComponent<BoxCollider>();
            volume.isTrigger = true;
            volume.size = size;
            LocationVolume locationVolume = root.AddComponent<LocationVolume>();
            locationVolume.Configure(stableId, areaName, roomName, priority, tracker);
        }

        private static void CreateReceptionWalls(Transform parent, Material wall, Material trim)
        {
            CreateWall("ReceptionWestSouth", parent, new Vector3(-6f, 1.5f, -3f), new Vector3(0.35f, 3f, 4f), wall);
            CreateWall("ReceptionWestNorth", parent, new Vector3(-6f, 1.5f, 3f), new Vector3(0.35f, 3f, 4f), wall);
            CreateWall("ReceptionEastSouth", parent, new Vector3(6f, 1.5f, -3f), new Vector3(0.35f, 3f, 4f), wall);
            CreateWall("ReceptionEastNorth", parent, new Vector3(6f, 1.5f, 3f), new Vector3(0.35f, 3f, 4f), wall);
            CreateWall("ReceptionSouthWest", parent, new Vector3(-3.5f, 1.5f, -5f), new Vector3(5f, 3f, 0.35f), wall);
            CreateWall("ReceptionSouthEast", parent, new Vector3(3.5f, 1.5f, -5f), new Vector3(5f, 3f, 0.35f), wall);
            CreateWall("ReceptionNorthWest", parent, new Vector3(-3.5f, 1.5f, 5f), new Vector3(5f, 3f, 0.35f), wall);
            CreateWall("ReceptionNorthEast", parent, new Vector3(3.5f, 1.5f, 5f), new Vector3(5f, 3f, 0.35f), wall);
            CreateWall("ReceptionLintelWest", parent, new Vector3(-6f, 3.1f, 0f), new Vector3(0.35f, 0.45f, 2f), trim);
            CreateWall("ReceptionLintelEast", parent, new Vector3(6f, 3.1f, 0f), new Vector3(0.35f, 0.45f, 2f), trim);
            CreateWall("ReceptionLintelNorth", parent, new Vector3(0f, 3.1f, 5f), new Vector3(2f, 0.45f, 0.35f), trim);
            CreateWall("ReceptionLintelSouth", parent, new Vector3(0f, 3.1f, -5f), new Vector3(2f, 0.45f, 0.35f), trim);
        }

        private static void CreateOfficeWalls(Transform parent, Material wall, Material trim)
        {
            CreateWall("OfficeWest", parent, new Vector3(-14f, 1.5f, 0f), new Vector3(0.35f, 3f, 8f), wall);
            CreateWall("OfficeNorth", parent, new Vector3(-10f, 1.5f, 4f), new Vector3(8f, 3f, 0.35f), wall);
            CreateWall("OfficeSouth", parent, new Vector3(-10f, 1.5f, -4f), new Vector3(8f, 3f, 0.35f), wall);
            CreateWall("OfficeLintel", parent, new Vector3(-6f, 3.1f, 0f), new Vector3(0.35f, 0.45f, 2f), trim);
        }

        private static void CreateAnalysisWalls(Transform parent, Material wall, Material trim)
        {
            CreateWall("AnalysisEast", parent, new Vector3(14f, 1.5f, 0f), new Vector3(0.35f, 3f, 8f), wall);
            CreateWall("AnalysisNorth", parent, new Vector3(10f, 1.5f, 4f), new Vector3(8f, 3f, 0.35f), wall);
            CreateWall("AnalysisSouth", parent, new Vector3(10f, 1.5f, -4f), new Vector3(8f, 3f, 0.35f), wall);
            CreateWall("AnalysisLintel", parent, new Vector3(6f, 3.1f, 0f), new Vector3(0.35f, 0.45f, 2f), trim);
        }

        private static void CreateArchiveWalls(Transform parent, Material wall, Material trim)
        {
            CreateWall("ArchiveWest", parent, new Vector3(-5f, 1.5f, 9f), new Vector3(0.35f, 3f, 8f), wall);
            CreateWall("ArchiveEast", parent, new Vector3(5f, 1.5f, 9f), new Vector3(0.35f, 3f, 8f), wall);
            CreateWall("ArchiveAnnexWest", parent, new Vector3(-4f, 1.5f, 16f), new Vector3(0.35f, 3f, 6f), wall);
            CreateWall("ArchiveAnnexEast", parent, new Vector3(4f, 1.5f, 16f), new Vector3(0.35f, 3f, 6f), wall);
            CreateWall("ArchiveNorthWest", parent, new Vector3(-3f, 1.5f, 13f), new Vector3(4f, 3f, 0.35f), wall);
            CreateWall("ArchiveNorthEast", parent, new Vector3(3f, 1.5f, 13f), new Vector3(4f, 3f, 0.35f), wall);
            CreateWall("ArchiveAnnexNorth", parent, new Vector3(0f, 1.5f, 19f), new Vector3(8f, 3f, 0.35f), wall);
            CreateWall("ArchiveSouthLintel", parent, new Vector3(0f, 3.1f, 5f), new Vector3(2f, 0.45f, 0.35f), trim);
            CreateWall("ArchiveNorthLintel", parent, new Vector3(0f, 3.1f, 13f), new Vector3(2f, 0.45f, 0.35f), trim);
        }

        private static void CreateRestrictedWalls(Transform parent, Material wall, Material trim)
        {
            CreateWall("RestrictedWest", parent, new Vector3(-5f, 1.5f, -9f), new Vector3(0.35f, 3f, 8f), wall);
            CreateWall("RestrictedEast", parent, new Vector3(5f, 1.5f, -9f), new Vector3(0.35f, 3f, 8f), wall);
            CreateWall("RestrictedSouth", parent, new Vector3(0f, 1.5f, -13f), new Vector3(10f, 3f, 0.35f), wall);
            CreateWall("RestrictedLintel", parent, new Vector3(0f, 3.1f, -5f), new Vector3(2f, 0.45f, 0.35f), trim);
        }

        private static void BuildFurniture(Transform parent, LabMaterials materials)
        {
            CreateVisualCube("ReceptionDesk", parent, new Vector3(0f, 0.7f, 2.8f), new Vector3(4.3f, 1.3f, 0.85f), materials.Furniture);
            CreateVisualCube("OfficeDesk", parent, new Vector3(-10.4f, 0.7f, 1.4f), new Vector3(2.8f, 1.3f, 1.1f), materials.Furniture);
            CreateVisualCube("OfficeLedgerShelf", parent, new Vector3(-12.8f, 1.25f, -1.6f), new Vector3(0.55f, 2.5f, 3.8f), materials.Furniture);
            CreateVisualCube("AnalysisConsole", parent, new Vector3(11.3f, 0.85f, 1.6f), new Vector3(2.4f, 1.6f, 1.2f), materials.Research);
            CreateVisualCube("AnalysisArcaneCore", parent, new Vector3(9.1f, 0.9f, -1.3f), new Vector3(1.2f, 1.8f, 1.2f), materials.Research);
            CreateVisualCube("ArchiveShelfA", parent, new Vector3(-3.5f, 1.3f, 8.7f), new Vector3(0.6f, 2.6f, 4.8f), materials.Furniture);
            CreateVisualCube("ArchiveShelfB", parent, new Vector3(3.5f, 1.3f, 8.7f), new Vector3(0.6f, 2.6f, 4.8f), materials.Furniture);
            CreateVisualCube("ArchiveShelfC", parent, new Vector3(-2.5f, 1.3f, 16f), new Vector3(0.6f, 2.6f, 3.4f), materials.Furniture);
            CreateVisualCube("ArchiveShelfD", parent, new Vector3(2.5f, 1.3f, 16f), new Vector3(0.6f, 2.6f, 3.4f), materials.Furniture);
            CreateVisualCube("RestrictedPlinth", parent, new Vector3(0f, 0.7f, -9.7f), new Vector3(2.6f, 1.3f, 2.6f), materials.Research);
            CreateDecorativeStairSet("OfficeSlateBrassSteps", parent, new Vector3(-10f, 0f, -2.85f), Vector3.forward, materials.StairsSlateBrass);
            CreateDecorativeStairSet("AnalysisIronCyanSteps", parent, new Vector3(9.1f, 0f, -2.5f), Vector3.forward, materials.StairsIronCyan);
            CreateDecorativeStairSet("ArchiveMarbleIvorySteps", parent, new Vector3(0f, 0f, 14.1f), Vector3.back, materials.StairsMarbleIvory);
        }

        private static void CreateDecorativeStairSet(
            string name,
            Transform parent,
            Vector3 basePosition,
            Vector3 forward,
            Material material)
        {
            Transform root = CreateObject(name, parent).transform;
            Vector3 direction = forward.sqrMagnitude <= 0.001f ? Vector3.forward : forward.normalized;
            for (int index = 0; index < 3; index++)
            {
                float height = 0.16f * (index + 1);
                Vector3 position = basePosition + direction * (0.42f * index) + Vector3.up * (height * 0.5f);
                GameObject step = CreateVisualCube(
                    "Step_" + (index + 1),
                    root,
                    position,
                    new Vector3(2.5f, height, 0.48f),
                    material);
                step.transform.localRotation = Quaternion.LookRotation(direction, Vector3.up);
            }
        }

        private static LabDoorController BuildDoors(Transform parent, LabMaterials materials, NotificationView notification)
        {
            CreateLabDoor("Door_TaxOffice", parent, new Vector3(-6f, 1.3f, 0f), false, materials.DoorOfficial, materials.Selection, notification, DoorAccessRequirement.None);
            CreateLabDoor("Door_Analysis", parent, new Vector3(6f, 1.3f, 0f), false, materials.DoorOfficial, materials.Selection, notification, DoorAccessRequirement.None);
            CreateLabDoor("Door_Archive", parent, new Vector3(0f, 1.3f, 5f), true, materials.DoorArchive, materials.Selection, notification, DoorAccessRequirement.None);
            LabDoorController archiveAnnexDoor = CreateLabDoor("Door_ArchiveAnnex", parent, new Vector3(0f, 1.3f, 13f), true, materials.DoorArchive, materials.Selection, notification, DoorAccessRequirement.AlwaysLocked);
            CreateLabDoor("Door_Restricted", parent, new Vector3(0f, 1.3f, -5f), true, materials.DoorContainment, materials.Selection, notification, DoorAccessRequirement.AlwaysLocked);
            return archiveAnnexDoor;
        }

        private static LabDoorController CreateLabDoor(
            string name,
            Transform parent,
            Vector3 position,
            bool horizontalWall,
            Material doorMaterial,
            Material selectionMaterial,
            NotificationView notification,
            DoorAccessRequirement accessRequirement)
        {
            GameObject root = CreateObject(name, parent);
            root.layer = Physics.IgnoreRaycastLayer;
            root.transform.position = position;
            SphereCollider interactionCollider = root.AddComponent<SphereCollider>();
            interactionCollider.isTrigger = true;
            interactionCollider.radius = 1.55f;
            interactionCollider.center = new Vector3(0f, 0f, 0f);
            Transform focus = CreateObject("FocusPoint", root.transform).transform;
            focus.localPosition = new Vector3(0f, 0.25f, 0f);
            GameObject leaf = CreateVisualCube("DoorLeaf", root.transform, Vector3.zero, horizontalWall ? new Vector3(1.75f, 2.6f, 0.18f) : new Vector3(0.18f, 2.6f, 1.75f), doorMaterial);
            BoxCollider blocker = leaf.AddComponent<BoxCollider>();
            GameObject obstruction = CreateObject("ClosingObstruction", root.transform);
            obstruction.layer = 0;
            BoxCollider obstructionCollider = obstruction.AddComponent<BoxCollider>();
            obstructionCollider.isTrigger = true;
            obstructionCollider.size = horizontalWall ? new Vector3(1.9f, 2.6f, 1.1f) : new Vector3(1.1f, 2.6f, 1.9f);
            GameObject selection = CreatePrimitiveWithoutCollider("SelectionMarker", PrimitiveType.Cylinder, root.transform, selectionMaterial);
            selection.transform.localPosition = new Vector3(0f, -1.22f, 0f);
            selection.transform.localScale = new Vector3(0.95f, 0.02f, 0.95f);
            selection.SetActive(false);
            LabDoorController controller = root.AddComponent<LabDoorController>();
            controller.Configure(
                name.ToLowerInvariant(),
                accessRequirement == DoorAccessRequirement.AlwaysLocked ? "격리 연구실 봉쇄문" : "연구실 문",
                focus,
                selection,
                leaf.transform,
                blocker,
                obstructionCollider,
                notification,
                horizontalWall ? new Vector3(1.7f, 0f, 0f) : new Vector3(0f, 0f, 1.7f),
                accessRequirement,
                "접근 권한이 없습니다. 문이 잠겨 있습니다.");
            return controller;
        }

        private static PrototypeInteractable CreateResearcher(
            GameObject prefab,
            Scene scene,
            Transform parent,
            DialogueFocusController dialogueController,
            DialogueSequence sequence)
        {
            GameObject instance = InstantiatePrefab(prefab, scene, parent, "WorldlineResearcher");
            instance.transform.SetPositionAndRotation(new Vector3(9.25f, 0f, -0.35f), Quaternion.Euler(0f, 180f, 0f));
            PrototypeInteractable interactable = instance.GetComponent<PrototypeInteractable>();
            ConfigureInteractable(interactable, "worldline-researcher", "세계선 분석 연구원", "대화", dialogueController, instance.GetComponent<PlayerFacing>(), sequence, null);
            return interactable;
        }

        private static PrototypeInteractable CreateCombatLiaisonOfficer(
            GameObject prefab,
            Scene scene,
            Transform parent,
            DialogueFocusController dialogueController,
            DialogueSequence sequence)
        {
            GameObject instance = InstantiatePrefab(prefab, scene, parent, "CombatLiaisonOfficer");
            instance.transform.SetPositionAndRotation(new Vector3(-12.4f, 0f, 2.55f), Quaternion.Euler(0f, 135f, 0f));
            PrototypeInteractable interactable = instance.GetComponent<PrototypeInteractable>();
            ConfigureInteractable(
                interactable,
                "combat-liaison-officer",
                "전투 대응 집행관",
                "대화",
                dialogueController,
                instance.GetComponent<PlayerFacing>(),
                sequence,
                null);

            // CombatTrainingCoordinator listens for completed dialogue and filters this stable ID.
            return interactable;
        }

        private static PrototypeInteractable CreateLedger(Transform parent, LabMaterials materials, DialogueFocusController dialogueController)
        {
            GameObject root = CreateObject("InspectableTaxLedger", parent);
            root.layer = Physics.IgnoreRaycastLayer;
            root.transform.position = new Vector3(-9.7f, 0f, -0.8f);
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.5f, 0f);
            collider.size = new Vector3(1.2f, 1f, 0.9f);
            GameObject body = CreateVisualCube("LedgerBody", root.transform, new Vector3(0f, 0.5f, 0f), new Vector3(1.15f, 0.95f, 0.82f), materials.Furniture);
            GameObject marker = CreatePrimitiveWithoutCollider("SelectionMarker", PrimitiveType.Cylinder, root.transform, materials.Selection);
            marker.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            marker.transform.localScale = new Vector3(0.7f, 0.02f, 0.7f);
            marker.SetActive(false);
            Transform focus = CreateObject("FocusPoint", root.transform).transform;
            focus.localPosition = new Vector3(0f, 1f, 0f);
            PrototypeInteractable interactable = root.AddComponent<PrototypeInteractable>();
            ConfigureInteractable(
                interactable,
                "tax-ledger",
                "세계선 세무 장부",
                "조사",
                dialogueController,
                null,
                null,
                new[] { "승인 대기 중인 세계선 조정 세금이 빼곡하게 기록되어 있다." });
            SerializedObject serialized = new SerializedObject(interactable);
            SetObject(serialized, "focusPoint", focus);
            SetObject(serialized, "selectionMarker", marker);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            body.layer = Physics.IgnoreRaycastLayer;
            return interactable;
        }

        private static PrototypeInteractable CreateInspectionNode(
            string name,
            string stableId,
            string displayName,
            Transform parent,
            LabMaterials materials,
            DialogueFocusController dialogueController,
            Vector3 position,
            string[] lines)
        {
            GameObject root = CreateObject(name, parent);
            root.layer = Physics.IgnoreRaycastLayer;
            root.transform.position = position;
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.5f, 0f);
            collider.size = new Vector3(1.15f, 1f, 0.9f);
            GameObject body = CreateVisualCube("InspectableBody", root.transform, new Vector3(0f, 0.5f, 0f), new Vector3(1.05f, 0.9f, 0.78f), materials.Furniture);
            GameObject marker = CreatePrimitiveWithoutCollider("SelectionMarker", PrimitiveType.Cylinder, root.transform, materials.Selection);
            marker.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            marker.transform.localScale = new Vector3(0.7f, 0.02f, 0.7f);
            marker.SetActive(false);
            Transform focus = CreateObject("FocusPoint", root.transform).transform;
            focus.localPosition = new Vector3(0f, 1f, 0f);
            PrototypeInteractable interactable = root.AddComponent<PrototypeInteractable>();
            ConfigureInteractable(interactable, stableId, displayName, "조사", dialogueController, null, null, lines);
            SerializedObject serialized = new SerializedObject(interactable);
            SetObject(serialized, "focusPoint", focus);
            SetObject(serialized, "selectionMarker", marker);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            body.layer = Physics.IgnoreRaycastLayer;
            return interactable;
        }

        private static void BuildCameraZones(Transform parent, QuarterViewCameraRig rig)
        {
            CreateCameraZone("CameraZone_Analysis", parent, rig, new Vector3(10f, 1.5f, 0f), new Vector3(7.4f, 3f, 7.4f), new QuarterViewCameraProfile(45f, 37f, 7.2f, new Vector3(0f, 1f, 0f), 0.25f), 20, "analysis");
            CreateCameraZone("CameraZone_Archive", parent, rig, new Vector3(0f, 1.5f, 10f), new Vector3(9.4f, 3f, 15f), new QuarterViewCameraProfile(135f, 38f, 7.4f, new Vector3(0f, 1.2f, 0.2f), 0.25f), 10, "archive");
            CreateCameraZone("CameraZone_RestrictedExterior", parent, rig, new Vector3(0f, 1.5f, -4.0f), new Vector3(4.5f, 3f, 2.5f), new QuarterViewCameraProfile(45f, 34f, 7.1f, new Vector3(0f, 1f, -0.35f), 0.25f), 15, "restricted-exterior");
        }

        private static void CreateCameraZone(string name, Transform parent, QuarterViewCameraRig rig, Vector3 position, Vector3 size, QuarterViewCameraProfile profile, int priority, string stableId)
        {
            GameObject zoneObject = CreateObject(name, parent);
            zoneObject.transform.position = position;
            BoxCollider collider = zoneObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = size;
            CameraZone zone = zoneObject.AddComponent<CameraZone>();
            zone.Configure(rig, profile, priority, stableId);
        }

        private static void BuildLighting(Transform parent)
        {
            Transform lighting = CreateObject("Lighting", parent).transform;
            GameObject directional = CreateObject("Directional Light", lighting);
            Light directionalLight = directional.AddComponent<Light>();
            directionalLight.type = LightType.Directional;
            directionalLight.intensity = 0.8f;
            directionalLight.color = new Color(0.60f, 0.72f, 1f);
            directional.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            CreatePointLight("ReceptionLamp", lighting, new Vector3(0f, 3.2f, 0f), new Color(0.48f, 0.62f, 1f), 4.2f, 8f);
            CreatePointLight("AnalysisLamp", lighting, new Vector3(10f, 3.2f, 0f), new Color(0.62f, 0.38f, 1f), 4.5f, 8f);
            CreatePointLight("ArchiveLamp", lighting, new Vector3(0f, 3.2f, 10f), new Color(0.96f, 0.68f, 0.36f), 3.8f, 8f);
            CreatePointLight("RestrictedLamp", lighting, new Vector3(0f, 3.2f, -9f), new Color(0.96f, 0.24f, 0.32f), 3.6f, 8f);
        }

        private static void CreatePointLight(string name, Transform parent, Vector3 position, Color color, float intensity, float range)
        {
            GameObject objectRoot = CreateObject(name, parent);
            objectRoot.transform.position = position;
            Light light = objectRoot.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
        }

        private static EventSystem GetOrCreateEventSystem(Scene scene, Transform generatedParent)
        {
            EventSystem[] existing = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<EventSystem>(true))
                .ToArray();
            if (existing.Length > 1)
            {
                throw new InvalidOperationException("The scene already has multiple EventSystem components.");
            }

            EventSystem eventSystem;
            if (existing.Length == 1)
            {
                eventSystem = existing[0];
            }
            else
            {
                GameObject eventSystemObject = CreateObject("WorldAdjustmentLabEventSystem", generatedParent);
                eventSystem = eventSystemObject.AddComponent<EventSystem>();
            }

            InputSystemUiBootstrap uiBootstrap = GetOrAdd<InputSystemUiBootstrap>(eventSystem.gameObject);
            uiBootstrap.Configure(eventSystem);
            return eventSystem;
        }

        private static HudReferences BuildHud(Scene scene, Transform parent, DialogueTheme theme, DialogueVisualAssets dialogueArt)
        {
            FrontendUiTheme frontendTheme = new FrontendUiTheme();
            GameObject canvasObject = new GameObject("WorldAdjustmentLabHud", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            EventSystem eventSystem = GetOrCreateEventSystem(scene, parent);

            GameObject safeAreaObject = new GameObject("SafeAreaRoot", typeof(RectTransform));
            safeAreaObject.transform.SetParent(canvasObject.transform, false);
            SetStretch(safeAreaObject.GetComponent<RectTransform>());
            SafeAreaLayout safeAreaLayout = safeAreaObject.AddComponent<SafeAreaLayout>();
            safeAreaLayout.Configure(safeAreaObject.GetComponent<RectTransform>());

            GameObject locationPanel = CreateUiPanel("LocationHud", safeAreaObject.transform, new Color(0.06f, 0.09f, 0.13f, 0.90f));
            SetRect(locationPanel.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(42f, -42f), new Vector2(420f, 92f), new Vector2(0f, 1f));
            Text areaLabel = CreateText("Area", locationPanel.transform, frontendTheme, 24, TextAnchor.MiddleLeft, new Vector2(20f, 63f), new Vector2(380f, 34f));
            Text roomLabel = CreateText("Room", locationPanel.transform, frontendTheme, 21, TextAnchor.MiddleLeft, new Vector2(20f, 27f), new Vector2(380f, 30f));
            roomLabel.color = frontendTheme.MenuSecondary;

            GameObject statusPanel = CreateUiPanel("ResourceTimeHud", safeAreaObject.transform, new Color(0.06f, 0.09f, 0.13f, 0.90f));
            SetRect(statusPanel.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-42f, -42f), new Vector2(330f, 92f), new Vector2(1f, 1f));
            Text currencyCaption = CreateText("CurrencyCaption", statusPanel.transform, frontendTheme, 18, TextAnchor.MiddleLeft, new Vector2(20f, 62f), new Vector2(150f, 28f));
            currencyCaption.text = "재화";
            Text currencyValue = CreateText("CurrencyValue", statusPanel.transform, frontendTheme, 22, TextAnchor.MiddleRight, new Vector2(290f, 62f), new Vector2(120f, 30f));
            Text timeCaption = CreateText("TimeCaption", statusPanel.transform, frontendTheme, 18, TextAnchor.MiddleLeft, new Vector2(20f, 27f), new Vector2(150f, 28f));
            timeCaption.text = "시간";
            Text timeValue = CreateText("TimeValue", statusPanel.transform, frontendTheme, 22, TextAnchor.MiddleRight, new Vector2(290f, 27f), new Vector2(120f, 30f));
            InGameHudView inGameHudView = canvasObject.AddComponent<InGameHudView>();
            inGameHudView.Configure(areaLabel, roomLabel, currencyValue, timeValue);

            DialogueFocusController controller = canvasObject.AddComponent<DialogueFocusController>();

            GameObject promptPanel = CreateUiPanel("InteractionPrompt", canvasObject.transform, new Color(0.025f, 0.035f, 0.055f, 0.92f));
            SetRect(promptPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 72f), new Vector2(480f, 94f));
            CanvasGroup promptGroup = promptPanel.AddComponent<CanvasGroup>();
            Text promptTarget = CreateText("Target", promptPanel.transform, frontendTheme, 24, TextAnchor.MiddleLeft, new Vector2(30f, 47f), new Vector2(290f, 36f));
            Text promptAction = CreateText("Prompt", promptPanel.transform, frontendTheme, 23, TextAnchor.MiddleRight, new Vector2(320f, 47f), new Vector2(130f, 36f));
            InteractionPromptView prompt = promptPanel.AddComponent<InteractionPromptView>();
            SerializedObject promptSerialized = new SerializedObject(prompt);
            SetObject(promptSerialized, "canvasGroup", promptGroup);
            SetObject(promptSerialized, "targetLabel", promptTarget);
            SetObject(promptSerialized, "promptLabel", promptAction);
            promptSerialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject dialogueOverlay = new GameObject("DialogueOverlay", typeof(RectTransform));
            dialogueOverlay.transform.SetParent(canvasObject.transform, false);
            SetStretch(dialogueOverlay.GetComponent<RectTransform>());
            CanvasGroup dialogueGroup = dialogueOverlay.AddComponent<CanvasGroup>();

            Image playerPortrait = CreateImage("TaxOfficerPortrait", dialogueOverlay.transform, Vector2.zero, new Vector2(600f, 700f));
            SetRect(playerPortrait.rectTransform, Vector2.zero, Vector2.zero, new Vector2(64f, 560f), new Vector2(600f, 700f), new Vector2(0f, 0.5f));
            playerPortrait.sprite = dialogueArt.TaxOfficerPortrait;
            playerPortrait.preserveAspect = true;

            Image partnerPortrait = CreateImage("PartnerPortrait", dialogueOverlay.transform, Vector2.zero, new Vector2(600f, 700f));
            SetRect(partnerPortrait.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-64f, 560f), new Vector2(600f, 700f), new Vector2(1f, 0.5f));
            partnerPortrait.sprite = dialogueArt.ResearcherPortrait;
            partnerPortrait.preserveAspect = true;

            GameObject dialoguePanel = CreateUiPanel("DialoguePanel", dialogueOverlay.transform, Color.white);
            Image dialoguePanelImage = dialoguePanel.GetComponent<Image>();
            dialoguePanelImage.sprite = dialogueArt.Panel;
            dialoguePanelImage.preserveAspect = false;
            SetRect(dialoguePanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 205f), new Vector2(1760f, 340f));

            Image playerNameplate = CreateImage("TaxOfficerNameplate", dialogueOverlay.transform, Vector2.zero, new Vector2(410f, 82f));
            SetRect(playerNameplate.rectTransform, Vector2.zero, Vector2.zero, new Vector2(98f, 372f), new Vector2(410f, 82f), new Vector2(0f, 0.5f));
            playerNameplate.sprite = dialogueArt.Nameplate;
            playerNameplate.preserveAspect = false;
            Text playerName = CreateText("TaxOfficerName", playerNameplate.transform, frontendTheme, 30, TextAnchor.MiddleCenter, new Vector2(0f, 41f), new Vector2(410f, 70f));

            Image partnerNameplate = CreateImage("PartnerNameplate", dialogueOverlay.transform, Vector2.zero, new Vector2(410f, 82f));
            SetRect(partnerNameplate.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-98f, 372f), new Vector2(410f, 82f), new Vector2(1f, 0.5f));
            partnerNameplate.sprite = dialogueArt.Nameplate;
            partnerNameplate.preserveAspect = false;
            Text partnerName = CreateText("PartnerName", partnerNameplate.transform, frontendTheme, 30, TextAnchor.MiddleCenter, new Vector2(0f, 41f), new Vector2(410f, 70f));

            Text fallbackSpeaker = CreateText("FallbackSpeaker", dialoguePanel.transform, frontendTheme, 25, TextAnchor.MiddleLeft, new Vector2(270f, 275f), new Vector2(980f, 32f));
            fallbackSpeaker.color = Color.clear;
            Text body = CreateText("DialogueBody", dialoguePanel.transform, frontendTheme, 34, TextAnchor.UpperLeft, new Vector2(270f, 185f), new Vector2(1120f, 130f));
            Image hintDecoration = CreateImage("ContinuePromptDecoration", dialoguePanel.transform, new Vector2(1090f, 56f), new Vector2(390f, 54f));
            hintDecoration.sprite = dialogueArt.ContinuePrompt;
            hintDecoration.preserveAspect = false;
            Text hint = CreateText("DialogueHint", hintDecoration.transform, frontendTheme, 18, TextAnchor.MiddleCenter, new Vector2(0f, 27f), new Vector2(390f, 44f));
            CreateDialogueButton("AdvanceButton", dialoguePanel.transform, new Vector2(1440f, 55f), new Vector2(200f, 54f), dialogueArt.ChoiceNormal, dialogueArt.ChoiceSelected, dialogueArt.ChoiceDisabled, "다음", controller, false, frontendTheme);
            CreateDialogueButton("CloseButton", dialoguePanel.transform, new Vector2(1225f, 55f), new Vector2(200f, 54f), dialogueArt.Back, dialogueArt.ChoiceSelected, dialogueArt.ChoiceDisabled, "닫기", controller, true, frontendTheme);

            DialogueView dialogueView = dialogueOverlay.AddComponent<DialogueView>();
            SerializedObject dialogueViewSerialized = new SerializedObject(dialogueView);
            SetObject(dialogueViewSerialized, "panelRoot", dialogueOverlay);
            SetObject(dialogueViewSerialized, "canvasGroup", dialogueGroup);
            SetObject(dialogueViewSerialized, "playerPortrait", playerPortrait);
            SetObject(dialogueViewSerialized, "partnerPortrait", partnerPortrait);
            SetObject(dialogueViewSerialized, "playerNameplate", playerNameplate);
            SetObject(dialogueViewSerialized, "partnerNameplate", partnerNameplate);
            SetObject(dialogueViewSerialized, "playerNameLabel", playerName);
            SetObject(dialogueViewSerialized, "partnerNameLabel", partnerName);
            SetObject(dialogueViewSerialized, "bodyLabel", body);
            SetObject(dialogueViewSerialized, "hintLabel", hint);
            SetObject(dialogueViewSerialized, "theme", theme);
            dialogueViewSerialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject notificationPanel = CreateUiPanel("Notification", canvasObject.transform, new Color(0.10f, 0.025f, 0.035f, 0.94f));
            SetRect(notificationPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(690f, 72f));
            CanvasGroup notificationGroup = notificationPanel.AddComponent<CanvasGroup>();
            Text notificationText = CreateText("Message", notificationPanel.transform, frontendTheme, 25, TextAnchor.MiddleCenter, new Vector2(20f, 36f), new Vector2(650f, 56f));
            NotificationView notification = notificationPanel.AddComponent<NotificationView>();
            SerializedObject notificationSerialized = new SerializedObject(notification);
            SetObject(notificationSerialized, "canvasGroup", notificationGroup);
            SetObject(notificationSerialized, "messageLabel", notificationText);
            notificationSerialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject pauseOverlayObject = new GameObject("PauseOverlay", typeof(RectTransform), typeof(CanvasGroup));
            pauseOverlayObject.transform.SetParent(canvasObject.transform, false);
            SetStretch(pauseOverlayObject.GetComponent<RectTransform>());
            AudioSource pauseAudio = canvasObject.AddComponent<AudioSource>();
            pauseAudio.playOnAwake = false;
            pauseAudio.spatialBlend = 0f;
            PauseMenuView pauseMenuView = pauseOverlayObject.AddComponent<PauseMenuView>();
            pauseMenuView.Configure(
                frontendTheme.Font,
                eventSystem,
                pauseAudio,
                AssetDatabase.LoadAssetAtPath<AudioClip>(SaveCompleteSfxPath));
            InGameUiCoordinator inGameUiCoordinator = canvasObject.AddComponent<InGameUiCoordinator>();

            GameObject combatOverlayObject = new GameObject("CombatTrainingOverlay", typeof(RectTransform), typeof(CanvasGroup));
            combatOverlayObject.transform.SetParent(canvasObject.transform, false);
            SetStretch(combatOverlayObject.GetComponent<RectTransform>());
            CombatTrainingView combatTrainingView = combatOverlayObject.AddComponent<CombatTrainingView>();
            combatTrainingView.Configure(
                frontendTheme.Font,
                eventSystem,
                LoadSingleSprite(CombatTrainingLineupPath),
                LoadSingleSprite(CombatTrainingBackdropPath));
            CombatTrainingCoordinator combatTrainingCoordinator = canvasObject.AddComponent<CombatTrainingCoordinator>();

            return new HudReferences(
                prompt,
                controller,
                dialogueView,
                notification,
                dialogueGroup,
                fallbackSpeaker,
                body,
                inGameHudView,
                pauseMenuView,
                inGameUiCoordinator,
                combatTrainingView,
                combatTrainingCoordinator);
        }

        private static void ConfigureSensor(InteractionSensor sensor, ExplorationInputReader input, PlayerFacing facing, Transform playerRoot, Transform sensorOrigin, InteractionPromptView prompt)
        {
            SerializedObject serialized = new SerializedObject(sensor);
            SetObject(serialized, "inputReader", input);
            SetObject(serialized, "playerFacing", facing);
            SetObject(serialized, "playerRoot", playerRoot);
            SetObject(serialized, "sensorOrigin", sensorOrigin);
            SetObject(serialized, "promptView", prompt);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureCameraAuthoring(QuarterViewCameraRig cameraRig)
        {
            SerializedObject serialized = new SerializedObject(cameraRig);
            SerializedProperty sensitivity = serialized.FindProperty("zoomSensitivity");
            if (sensitivity == null)
            {
                throw new InvalidOperationException("QuarterViewCameraRig zoom sensitivity property is missing.");
            }

            sensitivity.floatValue = CameraZoomSensitivity;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureDialogue(DialogueFocusController controller, ExplorationInputReader input, PlayerFacing facing, Transform playerRoot, QuarterViewCameraRig cameraRig, DialogueView view, CanvasGroup group, Text speaker, Text line)
        {
            SerializedObject serialized = new SerializedObject(controller);
            SetObject(serialized, "inputReader", input);
            SetObject(serialized, "playerFacing", facing);
            SetObject(serialized, "playerRoot", playerRoot);
            SetObject(serialized, "cameraRig", cameraRig);
            SetObject(serialized, "dialogueView", view);
            SetObject(serialized, "dialogueCanvasGroup", group);
            SetObject(serialized, "speakerLabel", speaker);
            SetObject(serialized, "lineLabel", line);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureInteractable(PrototypeInteractable interactable, string stableId, string displayName, string actionLabel, DialogueFocusController dialogueController, PlayerFacing facing, DialogueSequence sequence, string[] lines)
        {
            SerializedObject serialized = new SerializedObject(interactable);
            SetString(serialized, "stableId", stableId);
            SetString(serialized, "displayName", displayName);
            SetString(serialized, "actionLabel", actionLabel);
            SetObject(serialized, "dialogueController", dialogueController);
            SetObject(serialized, "facing", facing);
            SetObject(serialized, "dialogueSequence", sequence);
            SetStringArray(serialized, "dialogueLines", lines);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Sprite LoadSingleSprite(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("Required image is missing: " + path);
            }

            if (importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single
                || !importer.alphaIsTransparency
                || importer.mipmapEnabled)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                throw new InvalidOperationException("Image did not import as a sprite: " + path);
            }

            return sprite;
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

        private static Material CreateTexturedMaterial(
            string name,
            string texturePath,
            Vector2 textureTiling,
            bool repeat)
        {
            Texture2D texture = LoadWorldTexture(texturePath, repeat);
            Material material = CreateMaterial(name, Color.white);
            string textureProperty = material.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
            material.SetTexture(textureProperty, texture);
            material.SetTextureScale(textureProperty, textureTiling);
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", Color.white);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Texture2D LoadWorldTexture(string texturePath, bool repeat)
        {
            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("World Adjustment Lab texture is missing: " + texturePath);
            }

            TextureWrapMode wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            if (importer.textureType != TextureImporterType.Default
                || importer.wrapMode != wrapMode
                || !importer.mipmapEnabled)
            {
                importer.textureType = TextureImporterType.Default;
                importer.wrapMode = wrapMode;
                importer.mipmapEnabled = true;
                importer.SaveAndReimport();
            }

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                throw new InvalidOperationException("World Adjustment Lab texture did not import: " + texturePath);
            }

            return texture;
        }

        private static void ValidateScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                throw new InvalidOperationException("World Adjustment Lab validation requires a loaded scene.");
            }

            GameObject sceneRoot = GetOrCreateSceneRoot(scene);
            Transform generated = sceneRoot.transform.Find(GeneratedRootName);
            if (generated == null)
            {
                throw new InvalidOperationException("World Adjustment Lab generated root is missing.");
            }

            GameShellRoot[] gameShellRoots = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<GameShellRoot>(true))
                .ToArray();
            if (gameShellRoots.Length != 1)
            {
                throw new InvalidOperationException("The scene must contain exactly one GameShellRoot.");
            }

            GameShellRoot gameShellRoot = gameShellRoots[0];
            if (gameShellRoot.PlayerRoot == null
                || gameShellRoot.PlayerMotor == null
                || gameShellRoot.InputReader == null
                || gameShellRoot.InteractionSensor == null
                || gameShellRoot.CameraRig == null
                || gameShellRoot.GameCamera == null
                || gameShellRoot.InGameHudView == null
                || gameShellRoot.LocationTracker == null
                || gameShellRoot.InGameUiCoordinator == null
                || gameShellRoot.CombatTrainingCoordinator == null)
            {
                throw new InvalidOperationException("GameShellRoot has missing exploration or in-game UI references.");
            }

            SpawnPoint[] spawns = generated.GetComponentsInChildren<SpawnPoint>(true);
            int startCount = 0;
            foreach (SpawnPoint spawn in spawns)
            {
                if (spawn != null && string.Equals(spawn.SpawnKey, "start", StringComparison.Ordinal))
                {
                    startCount++;
                }
            }

            if (startCount != 1)
            {
                throw new InvalidOperationException("World Adjustment Lab requires exactly one start SpawnPoint.");
            }

            string[] requiredCheckpointSpawns =
            {
                "researcher_briefed",
                "tax_ledger_reviewed",
                "archive_catalogued",
            };
            foreach (string requiredSpawn in requiredCheckpointSpawns)
            {
                if (!spawns.Any(spawn => spawn != null && string.Equals(spawn.SpawnKey, requiredSpawn, StringComparison.Ordinal)))
                {
                    throw new InvalidOperationException("World Adjustment Lab checkpoint spawn is missing: " + requiredSpawn);
                }
            }

            if (generated.GetComponentsInChildren<PrototypeInteractable>(true).Length < 5)
            {
                throw new InvalidOperationException("World Adjustment Lab requires room-by-room inspection content.");
            }

            if (generated.GetComponentsInChildren<LabDoorController>(true).Length < 5)
            {
                throw new InvalidOperationException("World Adjustment Lab requires at least five doors.");
            }

            string[] requiredRoomFloorNames =
            {
                "ReceptionFloor",
                "TaxOfficeFloor",
                "AnalysisFloor",
                "ArchiveFloor",
                "RestrictedFloor",
            };
            foreach (string roomFloorName in requiredRoomFloorNames)
            {
                if (!HasDescendantNamed(generated, roomFloorName))
                {
                    throw new InvalidOperationException("World Adjustment Lab room floor is missing: " + roomFloorName);
                }
            }

            bool hasLockedDoor = false;
            foreach (LabDoorController door in generated.GetComponentsInChildren<LabDoorController>(true))
            {
                if (door.StartsLocked)
                {
                    hasLockedDoor = true;
                    break;
                }
            }

            if (!hasLockedDoor)
            {
                throw new InvalidOperationException("World Adjustment Lab requires a locked door.");
            }

            Animator taxOfficerAnimator = generated.GetComponentsInChildren<Animator>(true)
                .FirstOrDefault(candidate => candidate.name == "TaxOfficer3DModel");
            if (taxOfficerAnimator == null || taxOfficerAnimator.runtimeAnimatorController == null)
            {
                throw new InvalidOperationException("The tax officer 3D model requires an Animator and motion controller.");
            }

            if (generated.GetComponentsInChildren<DirectionalSpritePresenter>(true).Length != 0)
            {
                throw new InvalidOperationException("The temporary 2D tax officer presenter must not remain in the 3D lab slice.");
            }

            DialogueSequence sequence = AssetDatabase.LoadAssetAtPath<DialogueSequence>(ScriptableObjectFolder + "/WorldlineResearcherIntroduction.asset");
            if (sequence == null || !sequence.IsValid() || sequence.Player.Portrait == null || sequence.Partner.Portrait == null)
            {
                throw new InvalidOperationException("The researcher dialogue sequence and both portrait slots are required.");
            }

            DialogueSequence combatLiaisonSequence = AssetDatabase.LoadAssetAtPath<DialogueSequence>(ScriptableObjectFolder + "/CombatLiaisonBriefing.asset");
            if (combatLiaisonSequence == null
                || !combatLiaisonSequence.IsValid()
                || combatLiaisonSequence.Partner == null
                || !string.Equals(combatLiaisonSequence.Partner.SpeakerId, "combat-liaison-officer", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The combat liaison dialogue sequence is missing or invalid.");
            }

            PrototypeInteractable[] interactables = generated.GetComponentsInChildren<PrototypeInteractable>(true);
            if (interactables.Count(candidate => candidate != null
                    && string.Equals(candidate.StableId, "combat-liaison-officer", StringComparison.Ordinal)) != 1)
            {
                throw new InvalidOperationException("World Adjustment Lab requires exactly one combat liaison interaction point.");
            }

            DialogueTheme dialogueTheme = AssetDatabase.LoadAssetAtPath<DialogueTheme>(ScriptableObjectFolder + "/WorldAdjustmentLabDialogueTheme.asset");
            if (dialogueTheme == null
                || dialogueTheme.Font == null
                || !HasRequiredKoreanGlyphs(dialogueTheme.Font))
            {
                throw new InvalidOperationException("A Korean-capable dialogue font must be configured before validation.");
            }

            if (generated.GetComponentsInChildren<DialogueView>(true).Length != 1
                || generated.GetComponentsInChildren<NotificationView>(true).Length != 1)
            {
                throw new InvalidOperationException("World Adjustment Lab requires exactly one dialogue view and notification view.");
            }

            if (generated.GetComponentsInChildren<InGameHudView>(true).Length != 1
                || generated.GetComponentsInChildren<PauseMenuView>(true).Length != 1
                || generated.GetComponentsInChildren<InGameUiCoordinator>(true).Length != 1
                || generated.GetComponentsInChildren<CombatTrainingView>(true).Length != 1
                || generated.GetComponentsInChildren<CombatTrainingCoordinator>(true).Length != 1)
            {
                throw new InvalidOperationException("World Adjustment Lab requires exactly one HUD, pause menu and combat training coordinator.");
            }

            CombatTrainingView combatTrainingView = generated.GetComponentInChildren<CombatTrainingView>(true);
            if (combatTrainingView.OverlayGroup == null
                || combatTrainingView.OverlayGroup.alpha != 0f
                || combatTrainingView.OverlayGroup.interactable
                || combatTrainingView.OverlayGroup.blocksRaycasts)
            {
                throw new InvalidOperationException("Combat training overlay must be serialized hidden and non-blocking.");
            }

            if (!combatTrainingView.TryValidateConfiguration(out string combatViewError))
            {
                throw new InvalidOperationException("Combat training view is invalid: " + combatViewError);
            }

            string[] requiredLocationIds =
            {
                "reception",
                "tax_office",
                "analysis_lab",
                "archive",
                "archive_annex",
                "restricted",
            };
            LocationVolume[] locationVolumes = generated.GetComponentsInChildren<LocationVolume>(true);
            HashSet<string> locationIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (LocationVolume locationVolume in locationVolumes)
            {
                if (locationVolume == null || string.IsNullOrWhiteSpace(locationVolume.StableId) || !locationIds.Add(locationVolume.StableId))
                {
                    throw new InvalidOperationException("World Adjustment Lab location volume IDs must be non-empty and unique.");
                }
            }

            foreach (string locationId in requiredLocationIds)
            {
                if (!locationIds.Contains(locationId))
                {
                    throw new InvalidOperationException("World Adjustment Lab location volume is missing: " + locationId);
                }
            }

            if (CountEnabledComponents<Canvas>(scene) != 1
                || CountEnabledComponents<AudioListener>(scene) != 1
                || CountEnabledComponents<EventSystem>(scene) != 1)
            {
                throw new InvalidOperationException("The scene must have exactly one Canvas, AudioListener and EventSystem component.");
            }
        }

        private static int CountComponents<T>(Scene scene) where T : Component
        {
            int count = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                count += root.GetComponentsInChildren<T>(true).Length;
            }

            return count;
        }

        private static bool HasDescendantNamed(Transform root, string objectName)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (string.Equals(child.name, objectName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasRequiredKoreanGlyphs(Font font)
        {
            const string ValidationCharacters = "세무관세계선분석연구원접근권한문잠겨있습니다";
            foreach (char character in ValidationCharacters)
            {
                if (!font.HasCharacter(character))
                {
                    return false;
                }
            }

            return true;
        }

        private static int CountEnabledComponents<T>(Scene scene) where T : Behaviour
        {
            int count = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (T component in root.GetComponentsInChildren<T>(true))
                {
                    if (component.isActiveAndEnabled)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static GameObject GetOrCreateSceneRoot(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (string.Equals(root.name, SceneRootName, StringComparison.Ordinal))
                {
                    return root;
                }
            }

            GameObject sceneRoot = new GameObject(SceneRootName);
            SceneManager.MoveGameObjectToScene(sceneRoot, scene);
            return sceneRoot;
        }

        private static void RemoveGeneratedRoot(Transform sceneRoot, string rootName)
        {
            Transform existing = sceneRoot.Find(rootName);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }
        }

        private static Scene FindLoadedScene(string path)
        {
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                Scene scene = SceneManager.GetSceneAt(index);
                if (string.Equals(scene.path, path, StringComparison.Ordinal))
                {
                    return scene;
                }
            }

            return default;
        }

        private static T GetOrAdd<T>(GameObject root) where T : Component
        {
            T component = root.GetComponent<T>();
            return component != null ? component : root.AddComponent<T>();
        }

        private static GameObject CreateObject(string name, Transform parent)
        {
            GameObject created = new GameObject(name);
            created.transform.SetParent(parent, false);
            return created;
        }

        private static void CreateFloor(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            CreateCube(name, parent, position, scale, material);
        }

        private static void CreateWall(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            CreateCube(name, parent, position, scale, material);
        }

        private static GameObject CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = position;
            cube.transform.localScale = scale;
            MeshRenderer renderer = cube.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return cube;
        }

        private static GameObject CreateVisualCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject cube = CreateCube(name, parent, position, scale, material);
            Collider collider = cube.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            return cube;
        }

        private static void ApplyMaterial(GameObject root, Material material)
        {
            if (root == null || material == null)
            {
                return;
            }

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterial = material;
            }
        }

        private static GameObject CreatePrimitiveWithoutCollider(string name, PrimitiveType type, Transform parent, Material material)
        {
            GameObject primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            Collider collider = primitive.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            MeshRenderer renderer = primitive.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            return primitive;
        }

        private static Image CreateImage(string name, Transform parent, Vector2 position, Vector2 size)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            SetRect(imageObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero, position, size);
            return image;
        }

        private static void CreateDialogueButton(
            string name,
            Transform parent,
            Vector2 position,
            Vector2 size,
            Sprite normalSprite,
            Sprite selectedSprite,
            Sprite disabledSprite,
            string label,
            DialogueFocusController controller,
            bool closeDialogue,
            FrontendUiTheme theme)
        {
            GameObject objectRoot = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            objectRoot.transform.SetParent(parent, false);
            Image image = objectRoot.GetComponent<Image>();
            image.sprite = normalSprite;
            image.preserveAspect = false;
            image.color = Color.white;
            Button button = objectRoot.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            SpriteState spriteState = button.spriteState;
            spriteState.highlightedSprite = selectedSprite;
            spriteState.pressedSprite = selectedSprite;
            spriteState.selectedSprite = selectedSprite;
            spriteState.disabledSprite = disabledSprite;
            button.spriteState = spriteState;
            SetRect(objectRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero, position, size);

            Text text = CreateText("Label", objectRoot.transform, theme, 22, TextAnchor.MiddleCenter, new Vector2(0f, size.y * 0.5f), size - new Vector2(28f, 8f));
            text.color = new Color(0.97f, 0.92f, 0.78f, 1f);
            if (closeDialogue)
            {
                UnityEventTools.AddPersistentListener(button.onClick, controller.EndDialogue);
            }
            else
            {
                UnityEventTools.AddPersistentListener(button.onClick, controller.AdvanceDialogue);
            }
        }

        private static Text CreateText(string name, Transform parent, FrontendUiTheme theme, int fontSize, TextAnchor alignment, Vector2 position, Vector2 size)
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
            Shadow shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.92f);
            shadow.effectDistance = new Vector2(2f, -2f);
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

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = anchorMin == Vector2.zero && anchorMax == Vector2.zero ? new Vector2(0f, 0.5f) : new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Vector2 pivot)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetStretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private static GameObject SavePrefab(GameObject template, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(template, path);
            UnityEngine.Object.DestroyImmediate(template);
            if (prefab == null)
            {
                throw new InvalidOperationException("Could not save prefab: " + path);
            }

            return prefab;
        }

        private static GameObject InstantiatePrefab(GameObject prefab, Scene scene, Transform parent, string name)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            return instance;
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

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            root.layer = layer;
            foreach (Transform child in root.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
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

        private static void SetObject(SerializedObject serialized, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException("Serialized property was not found: " + propertyName);
            }

            property.objectReferenceValue = value;
        }

        private static void SetString(SerializedObject serialized, string propertyName, string value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException("Serialized property was not found: " + propertyName);
            }

            property.stringValue = value ?? string.Empty;
        }

        private static void SetStringArray(SerializedObject serialized, string propertyName, string[] values)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException("Serialized property was not found: " + propertyName);
            }

            string[] source = values ?? Array.Empty<string>();
            property.arraySize = source.Length;
            for (int index = 0; index < source.Length; index++)
            {
                property.GetArrayElementAtIndex(index).stringValue = source[index] ?? string.Empty;
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

        private readonly struct LabAssets
        {
            public LabAssets(DirectionalAnimationSet animationSet, DialogueTheme dialogueTheme, DialogueSequence researcherSequence, DialogueSequence combatLiaisonSequence, DialogueVisualAssets dialogueVisuals, Sprite bureauSeal, LabMaterials materials, LabPrefabs prefabs)
            {
                AnimationSet = animationSet;
                DialogueTheme = dialogueTheme;
                ResearcherSequence = researcherSequence;
                CombatLiaisonSequence = combatLiaisonSequence;
                DialogueVisuals = dialogueVisuals;
                BureauSeal = bureauSeal;
                Materials = materials;
                Prefabs = prefabs;
            }

            public DirectionalAnimationSet AnimationSet { get; }
            public DialogueTheme DialogueTheme { get; }
            public DialogueSequence ResearcherSequence { get; }
            public DialogueSequence CombatLiaisonSequence { get; }
            public DialogueVisualAssets DialogueVisuals { get; }
            public Sprite BureauSeal { get; }
            public LabMaterials Materials { get; }
            public LabPrefabs Prefabs { get; }
        }

        private readonly struct DialogueVisualAssets
        {
            public DialogueVisualAssets(
                Sprite taxOfficerPortrait,
                Sprite researcherPortrait,
                Sprite panel,
                Sprite nameplate,
                Sprite continuePrompt,
                Sprite choiceNormal,
                Sprite choiceSelected,
                Sprite choiceDisabled,
                Sprite back)
            {
                TaxOfficerPortrait = taxOfficerPortrait;
                ResearcherPortrait = researcherPortrait;
                Panel = panel;
                Nameplate = nameplate;
                ContinuePrompt = continuePrompt;
                ChoiceNormal = choiceNormal;
                ChoiceSelected = choiceSelected;
                ChoiceDisabled = choiceDisabled;
                Back = back;
            }

            public Sprite TaxOfficerPortrait { get; }
            public Sprite ResearcherPortrait { get; }
            public Sprite Panel { get; }
            public Sprite Nameplate { get; }
            public Sprite ContinuePrompt { get; }
            public Sprite ChoiceNormal { get; }
            public Sprite ChoiceSelected { get; }
            public Sprite ChoiceDisabled { get; }
            public Sprite Back { get; }
        }

        private readonly struct LabPrefabs
        {
            public LabPrefabs(GameObject player, GameObject npc)
            {
                Player = player;
                Npc = npc;
            }

            public GameObject Player { get; }
            public GameObject Npc { get; }
        }

        private readonly struct LabMaterials
        {
            public LabMaterials(
                Material receptionFloor,
                Material officeFloor,
                Material analysisFloor,
                Material archiveFloor,
                Material restrictedFloor,
                Material wallStoneBrass,
                Material wallArchiveBrick,
                Material wallContainmentMetal,
                Material trim,
                Material doorOfficial,
                Material doorArchive,
                Material doorContainment,
                Material stairsSlateBrass,
                Material stairsIronCyan,
                Material stairsMarbleIvory,
                Material furniture,
                Material research,
                Material npc,
                Material selection)
            {
                ReceptionFloor = receptionFloor;
                OfficeFloor = officeFloor;
                AnalysisFloor = analysisFloor;
                ArchiveFloor = archiveFloor;
                RestrictedFloor = restrictedFloor;
                WallStoneBrass = wallStoneBrass;
                WallArchiveBrick = wallArchiveBrick;
                WallContainmentMetal = wallContainmentMetal;
                Trim = trim;
                DoorOfficial = doorOfficial;
                DoorArchive = doorArchive;
                DoorContainment = doorContainment;
                StairsSlateBrass = stairsSlateBrass;
                StairsIronCyan = stairsIronCyan;
                StairsMarbleIvory = stairsMarbleIvory;
                Furniture = furniture;
                Research = research;
                Npc = npc;
                Selection = selection;
            }

            public Material ReceptionFloor { get; }
            public Material OfficeFloor { get; }
            public Material AnalysisFloor { get; }
            public Material ArchiveFloor { get; }
            public Material RestrictedFloor { get; }
            public Material WallStoneBrass { get; }
            public Material WallArchiveBrick { get; }
            public Material WallContainmentMetal { get; }
            public Material Trim { get; }
            public Material DoorOfficial { get; }
            public Material DoorArchive { get; }
            public Material DoorContainment { get; }
            public Material StairsSlateBrass { get; }
            public Material StairsIronCyan { get; }
            public Material StairsMarbleIvory { get; }
            public Material Furniture { get; }
            public Material Research { get; }
            public Material Npc { get; }
            public Material Selection { get; }
        }

        private readonly struct HudReferences
        {
            public HudReferences(
                InteractionPromptView prompt,
                DialogueFocusController dialogueController,
                DialogueView dialogueView,
                NotificationView notification,
                CanvasGroup dialogueCanvasGroup,
                Text fallbackSpeaker,
                Text body,
                InGameHudView inGameHudView,
                PauseMenuView pauseMenuView,
                InGameUiCoordinator inGameUiCoordinator,
                CombatTrainingView combatTrainingView,
                CombatTrainingCoordinator combatTrainingCoordinator)
            {
                Prompt = prompt;
                DialogueController = dialogueController;
                DialogueView = dialogueView;
                Notification = notification;
                DialogueCanvasGroup = dialogueCanvasGroup;
                FallbackSpeaker = fallbackSpeaker;
                Body = body;
                InGameHudView = inGameHudView;
                PauseMenuView = pauseMenuView;
                InGameUiCoordinator = inGameUiCoordinator;
                CombatTrainingView = combatTrainingView;
                CombatTrainingCoordinator = combatTrainingCoordinator;
            }

            public InteractionPromptView Prompt { get; }
            public DialogueFocusController DialogueController { get; }
            public DialogueView DialogueView { get; }
            public NotificationView Notification { get; }
            public CanvasGroup DialogueCanvasGroup { get; }
            public Text FallbackSpeaker { get; }
            public Text Body { get; }
            public InGameHudView InGameHudView { get; }
            public PauseMenuView PauseMenuView { get; }
            public InGameUiCoordinator InGameUiCoordinator { get; }
            public CombatTrainingView CombatTrainingView { get; }
            public CombatTrainingCoordinator CombatTrainingCoordinator { get; }
        }
    }

    internal readonly struct TaxOfficerModelPackage
    {
        public TaxOfficerModelPackage(GameObject modelPrefab, RuntimeAnimatorController animatorController, Material material)
        {
            ModelPrefab = modelPrefab;
            AnimatorController = animatorController;
            Material = material;
        }

        public GameObject ModelPrefab { get; }
        public RuntimeAnimatorController AnimatorController { get; }
        public Material Material { get; }
    }

    internal static class TaxOfficerModelAssetBuilder
    {
        private const string ModelFolder = "Assets/_Project/Art/Characters/TaxOfficer/Model3D";
        private const string CharacterModelPath = ModelFolder + "/tax_officer_model_v1.fbx";
        private const string MotionLibraryPath = ModelFolder + "/tax_officer_motion_library_v1.fbx";
        private const string AlbedoPath = ModelFolder + "/tax_officer_albedo_v1.png";
        private const string NormalPath = ModelFolder + "/tax_officer_normal_v1.png";
        private const string MetallicPath = ModelFolder + "/tax_officer_metallic_v1.png";
        private const string MaterialPath = ModelFolder + "/TaxOfficer3D.mat";
        private const string ControllerPath = ModelFolder + "/TaxOfficer3D.controller";

        public static TaxOfficerModelPackage Prepare()
        {
            ConfigureCharacterModelImporter();
            Avatar avatar = LoadAvatar();
            ConfigureMotionLibraryImporter(avatar);
            ConfigureTextureImporters();

            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterModelPath);
            if (modelPrefab == null)
            {
                throw new InvalidOperationException("The tax officer model FBX could not be imported: " + CharacterModelPath);
            }

            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(MotionLibraryPath)
                .OfType<AnimationClip>()
                .Where(clip => clip != null && !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (clips.Length == 0)
            {
                throw new InvalidOperationException("The tax officer motion library has no usable animation clips: " + MotionLibraryPath);
            }

            RuntimeAnimatorController controller = CreateAnimatorController(clips);
            Material material = CreateMaterial();
            return new TaxOfficerModelPackage(modelPrefab, controller, material);
        }

        private static void ConfigureCharacterModelImporter()
        {
            ModelImporter importer = AssetImporter.GetAtPath(CharacterModelPath) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("The tax officer model FBX is missing: " + CharacterModelPath);
            }

            bool changed = importer.animationType != ModelImporterAnimationType.Human
                || importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel
                || !importer.importAnimation
                || importer.importCameras
                || importer.importLights;
            if (!changed)
            {
                return;
            }

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.SaveAndReimport();
        }

        private static Avatar LoadAvatar()
        {
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(CharacterModelPath).OfType<Avatar>().FirstOrDefault();
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
            {
                throw new InvalidOperationException("The tax officer model requires a valid humanoid avatar.");
            }

            return avatar;
        }

        private static void ConfigureMotionLibraryImporter(Avatar avatar)
        {
            ModelImporter importer = AssetImporter.GetAtPath(MotionLibraryPath) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("The tax officer motion FBX is missing: " + MotionLibraryPath);
            }

            bool requiresReimport = importer.animationType != ModelImporterAnimationType.Human
                || importer.avatarSetup != ModelImporterAvatarSetup.CopyFromOther
                || importer.sourceAvatar != avatar
                || !importer.importAnimation
                || importer.importCameras
                || importer.importLights;
            if (requiresReimport)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                importer.sourceAvatar = avatar;
                importer.importAnimation = true;
                importer.importCameras = false;
                importer.importLights = false;
                importer.SaveAndReimport();
            }

            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            bool changed = false;
            foreach (ModelImporterClipAnimation clip in clips)
            {
                bool shouldLoop = clip.name.IndexOf("jump", StringComparison.OrdinalIgnoreCase) < 0;
                if (clip.loopTime != shouldLoop
                    || !clip.lockRootPositionXZ
                    || !clip.lockRootHeightY
                    || !clip.lockRootRotation)
                {
                    clip.loopTime = shouldLoop;
                    clip.lockRootPositionXZ = true;
                    clip.lockRootHeightY = true;
                    clip.lockRootRotation = true;
                    changed = true;
                }
            }

            if (changed)
            {
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
            }
        }

        private static void ConfigureTextureImporters()
        {
            ConfigureTextureImporter(AlbedoPath, TextureImporterType.Default, true);
            ConfigureTextureImporter(NormalPath, TextureImporterType.NormalMap, false);
            ConfigureTextureImporter(MetallicPath, TextureImporterType.Default, false);
        }

        private static void ConfigureTextureImporter(string path, TextureImporterType type, bool useSrgb)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("The tax officer texture is missing: " + path);
            }

            if (importer.textureType == type && importer.sRGBTexture == useSrgb)
            {
                return;
            }

            importer.textureType = type;
            importer.sRGBTexture = useSrgb;
            importer.SaveAndReimport();
        }

        private static RuntimeAnimatorController CreateAnimatorController(AnimationClip[] clips)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState childState in stateMachine.states.ToArray())
            {
                stateMachine.RemoveState(childState.state);
            }

            // The source package does not include a named idle. Its in-place idle-style
            // turn is the closest authored fallback until a dedicated idle arrives.
            AnimatorState idle = AddState(stateMachine, "Idle", FindClip(clips, "idle_style", "idle"));
            AddState(stateMachine, "Walk", FindClip(clips, "walking", "walk"));
            AddState(stateMachine, "Run", FindClip(clips, "running", "run_02", "run"));
            AnimatorState dash = AddState(stateMachine, "Dash", FindClip(clips, "dash", "sprint", "running", "run_02", "run"));
            // The supplied library does not contain a dedicated dash action, so use
            // the running action at a higher playback rate until one is authored.
            dash.speed = 1.35f;
            stateMachine.defaultState = idle;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimatorState AddState(AnimatorStateMachine stateMachine, string name, AnimationClip motion)
        {
            AnimatorState state = stateMachine.AddState(name);
            state.motion = motion;
            state.writeDefaultValues = false;
            return state;
        }

        private static AnimationClip FindClip(AnimationClip[] clips, params string[] nameFragments)
        {
            foreach (string fragment in nameFragments)
            {
                AnimationClip match = clips.FirstOrDefault(clip => clip.name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0);
                if (match != null)
                {
                    return match;
                }
            }

            return clips[0];
        }

        private static Material CreateMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    throw new InvalidOperationException("The URP Lit shader is unavailable for the tax officer material.");
                }

                material = new Material(shader)
                {
                    name = "TaxOfficer3D",
                };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture>(AlbedoPath));
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture>(NormalPath));
            material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture>(MetallicPath));
            material.SetFloat("_Metallic", 0.35f);
            material.SetFloat("_Smoothness", 0.38f);
            if (material.HasProperty("_EmissionColor"))
            {
                material.DisableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Color.black);
            }

            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }
    }
}
