using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DemonLord.Application;
using DemonLord.Bootstrap;
using DemonLord.Domain;
using DemonLord.Infrastructure;
using DemonLord.Presentation.Exploration;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DemonLord.Tests.PlayMode
{
    public sealed class ExplorationPrototypePlayModeTests : InputTestFixture
    {
        private const string GameShellSceneName = "90_GameShell";
        private Keyboard keyboard;
        private Mouse mouse;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
        }

        [TearDown]
        public override void TearDown()
        {
            keyboard = null;
            mouse = null;
            base.TearDown();
        }

        [UnityTearDown]
        public IEnumerator CleanupAfterUnityTest()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTest]
        public IEnumerator GameShell_InitializesAtStartWithSerializedComposition()
        {
            yield return LoadGameShell();
            Scene scene = SceneManager.GetActiveScene();
            GameShellRoot root = FindCompositionRoot(scene);
            InMemoryPlayerSession session = CreateSession();

            bool initialized = root.TryInitialize(
                session,
                new EntryDestination(GameShellSceneName, "start"),
                out string errorCode);

            Assert.That(initialized, Is.True, errorCode);
            Assert.That(root.IsInitialized, Is.True);
            Assert.That(root.InputReader.enabled, Is.True);
            Assert.That(root.PlayerMotor.enabled, Is.True);
            Assert.That(root.InteractionSensor.enabled, Is.True);
            Assert.That(session.CurrentSave, Is.Not.Null);

            SpawnPoint start = scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<SpawnPoint>(false))
                .Single(item => item.SpawnKey == "start");
            Assert.That(scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<SpawnPoint>(false))
                .Any(item => item.SpawnKey == LabCheckpointId.CombatLiaisonBriefed), Is.True);
            Assert.That(Vector3.Distance(root.PlayerRoot.position, start.transform.position), Is.LessThan(0.001f));
            Assert.That(root.CameraRig.MovementBasis, Is.Not.Null);
            Camera gameCamera = root.CameraRig.MovementBasis.GetComponent<Camera>();
            Assert.That(gameCamera, Is.Not.Null);
            Assert.That(gameCamera.orthographic, Is.True);
            Assert.That(gameCamera.orthographicSize, Is.InRange(6f, 12f));

            PrototypeInteractable[] activeInteractables = scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<PrototypeInteractable>(false))
                .ToArray();
            Assert.That(activeInteractables.Any(item => item.StableId == "worldline-researcher"), Is.True);
            Assert.That(activeInteractables.Any(item => item.StableId == "combat-liaison-officer"), Is.True);
            Assert.That(scene.GetRootGameObjects().SelectMany(item => item.GetComponentsInChildren<LabDoorController>(false)).Count(), Is.EqualTo(5));
            Assert.That(scene.GetRootGameObjects().SelectMany(item => item.GetComponentsInChildren<CameraZone>(false)).Count(), Is.EqualTo(3));
            Assert.That(FindTransform(scene, "ReceptionFloor"), Is.Not.Null);
            Assert.That(FindTransform(scene, "TaxOfficeFloor"), Is.Not.Null);
            Assert.That(FindTransform(scene, "AnalysisFloor"), Is.Not.Null);
            Assert.That(FindTransform(scene, "ArchiveFloor"), Is.Not.Null);
            Assert.That(FindTransform(scene, "RestrictedFloor"), Is.Not.Null);
            Assert.That(root.PlayerRoot.GetComponentInChildren<DirectionalSpritePresenter>(true), Is.Not.Null);
            Assert.That(scene.GetRootGameObjects().SelectMany(item => item.GetComponentsInChildren<DialogueView>(false)).Single(), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator GameShell_UnknownSpawnIsRejectedAndControlsStayDisabled()
        {
            yield return LoadGameShell();
            GameShellRoot root = FindCompositionRoot(SceneManager.GetActiveScene());

            bool initialized = root.TryInitialize(
                CreateSession(),
                new EntryDestination(GameShellSceneName, "missing-spawn"),
                out string errorCode);

            Assert.That(initialized, Is.False);
            Assert.That(errorCode, Is.EqualTo("spawn_point_not_found"));
            Assert.That(root.IsInitialized, Is.False);
            Assert.That(root.InputReader.enabled, Is.False);
            Assert.That(root.PlayerMotor.enabled, Is.False);
            Assert.That(root.InteractionSensor.enabled, Is.False);
        }

        [UnityTest]
        public IEnumerator Movement_WalkSprintDiagonalAndLaboratoryWallsAreStable()
        {
            keyboard = CreateIsolatedKeyboard();
            yield return LoadInitializedGameShell();
            GameShellRoot root = FindCompositionRoot(SceneManager.GetActiveScene());

            root.PlayerMotor.enabled = false;
            root.CameraRig.enabled = false;
            Camera gameCamera = root.CameraRig.MovementBasis.GetComponent<Camera>();
            gameCamera.transform.rotation = Quaternion.Euler(35f, 0f, 0f);
            CharacterController controller = root.PlayerMotor.CharacterController;

            PlacePlayer(controller, root.PlayerRoot, new Vector3(0f, 0.05f, -5f));
            root.PlayerMotor.ResetMotion();
            SetKeyboard(Key.W);
            Vector3 walkStart = root.PlayerRoot.position;
            root.PlayerMotor.Tick(0.1f, 1f);
            float walkDistance = HorizontalDistance(walkStart, root.PlayerRoot.position);
            float walkVelocity = root.PlayerMotor.CurrentHorizontalVelocity.magnitude;

            PlacePlayer(controller, root.PlayerRoot, new Vector3(0f, 0.05f, -5f));
            root.PlayerMotor.ResetMotion();
            SetKeyboard(Key.W, Key.LeftShift);
            Vector3 sprintStart = root.PlayerRoot.position;
            root.PlayerMotor.Tick(0.1f, 2f);
            float sprintDistance = HorizontalDistance(sprintStart, root.PlayerRoot.position);
            float sprintVelocity = root.PlayerMotor.CurrentHorizontalVelocity.magnitude;

            PlacePlayer(controller, root.PlayerRoot, new Vector3(0f, 0.05f, -5f));
            root.PlayerMotor.ResetMotion();
            SetKeyboard(Key.W, Key.D);
            Vector3 diagonalStart = root.PlayerRoot.position;
            root.PlayerMotor.Tick(0.1f, 3f);
            float diagonalDistance = HorizontalDistance(diagonalStart, root.PlayerRoot.position);
            float diagonalVelocity = root.PlayerMotor.CurrentHorizontalVelocity.magnitude;

            Assert.That(walkVelocity, Is.GreaterThan(0f));
            Assert.That(sprintVelocity, Is.GreaterThan(walkVelocity));
            Assert.That(diagonalVelocity, Is.EqualTo(walkVelocity).Within(0.01f));
            Assert.That(walkDistance, Is.GreaterThan(0f));
            Assert.That(sprintDistance, Is.GreaterThan(walkDistance));
            Assert.That(diagonalDistance, Is.GreaterThan(0f));

            PlacePlayer(controller, root.PlayerRoot, new Vector3(4f, 0.05f, -3.5f));
            root.PlayerMotor.ResetMotion();
            SetKeyboard(Key.W);
            for (int index = 0; index < 40; index++)
            {
                root.PlayerMotor.Tick(0.05f, 4f + index * 0.05f);
            }

            Assert.That(root.PlayerRoot.position.z, Is.LessThan(4.7f), "The player crossed the reception room's northern wall.");

            PlacePlayer(controller, root.PlayerRoot, new Vector3(4f, 0.05f, 3.8f));
            root.PlayerMotor.ResetMotion();
            PlayerFacing facing = root.PlayerRoot.GetComponent<PlayerFacing>();
            facing.SetFacing(FacingDirection8.North);
            SetKeyboard(Key.Space);
            for (int index = 0; index < 12; index++)
            {
                root.PlayerMotor.Tick(0.02f, 10f + index * 0.02f);
                SetKeyboard();
            }

            Assert.That(root.PlayerRoot.position.z, Is.LessThan(4.7f), "Dash crossed the laboratory collision wall.");
            Assert.That(root.PlayerMotor.IsDashing, Is.False);
            SetKeyboard();
        }

        [UnityTest]
        public IEnumerator Camera_KeepsFixedProjectionAndZoomClamps()
        {
            keyboard = CreateIsolatedKeyboard();
            mouse = CreateIsolatedMouse();
            yield return LoadInitializedGameShell();
            GameShellRoot root = FindCompositionRoot(SceneManager.GetActiveScene());
            Camera gameCamera = root.CameraRig.MovementBasis.GetComponent<Camera>();
            float initialYaw = gameCamera.transform.eulerAngles.y;

            SetKeyboard(Key.E);
            yield return null;
            SetKeyboard();
            yield return new WaitForSecondsRealtime(1.2f);

            float yawDelta = Mathf.Abs(Mathf.DeltaAngle(initialYaw, gameCamera.transform.eulerAngles.y));
            Assert.That(yawDelta, Is.LessThan(0.1f));

            root.PlayerMotor.enabled = false;
            CharacterController controller = root.PlayerMotor.CharacterController;
            PlacePlayer(controller, root.PlayerRoot, new Vector3(0f, 0.05f, -5f));
            root.PlayerMotor.ResetMotion();
            Vector3 movementStart = root.PlayerRoot.position;
            Vector3 expectedForward = ExplorationMath.CameraRelativeMove(
                Vector2.up,
                gameCamera.transform);
            SetKeyboard(Key.W);
            root.PlayerMotor.Tick(0.1f, 5f);
            SetKeyboard();
            Vector3 requestedVelocity = root.PlayerMotor.CurrentHorizontalVelocity;
            requestedVelocity.y = 0f;
            Assert.That(Vector3.Dot(requestedVelocity.normalized, expectedForward), Is.GreaterThan(0.99f));

            Vector3 cameraBeforeFollow = gameCamera.transform.position;
            PlacePlayer(controller, root.PlayerRoot, root.PlayerRoot.position + Vector3.right * 2f);
            yield return new WaitForSecondsRealtime(0.65f);
            Assert.That(
                HorizontalDistance(cameraBeforeFollow, gameCamera.transform.position),
                Is.GreaterThan(1.5f),
                "Camera did not follow the teleported player target.");

            QueueScroll(10000f);
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.That(gameCamera.orthographicSize, Is.EqualTo(6f).Within(0.05f));

            QueueScroll(-10000f);
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.That(gameCamera.orthographicSize, Is.EqualTo(12f).Within(0.05f));
        }

        [UnityTest]
        public IEnumerator CameraZone_DoesNotChangeImageMapProjection()
        {
            yield return LoadInitializedGameShell();
            GameShellRoot root = FindCompositionRoot(SceneManager.GetActiveScene());
            CameraZone zone = SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<CameraZone>(true))
                .Single(item => item.StableId == "analysis");
            Camera gameCamera = root.CameraRig.MovementBasis.GetComponent<Camera>();
            CharacterController controller = root.PlayerMotor.CharacterController;

            root.PlayerMotor.enabled = false;
            PlacePlayer(controller, root.PlayerRoot, zone.transform.position + Vector3.down * 1.2f);
            controller.Move(Vector3.zero);
            yield return new WaitForFixedUpdate();
            yield return new WaitForSecondsRealtime(1.5f);

            Assert.That(Mathf.Abs(Mathf.DeltaAngle(45f, gameCamera.transform.eulerAngles.y)), Is.LessThan(1.5f));
            Assert.That(gameCamera.orthographicSize, Is.EqualTo(8f).Within(0.05f));

            PlacePlayer(controller, root.PlayerRoot, new Vector3(0f, 0.05f, 0f));
            controller.Move(Vector3.zero);
            yield return new WaitForFixedUpdate();
            yield return new WaitForSecondsRealtime(1.5f);

            Assert.That(Mathf.Abs(Mathf.DeltaAngle(45f, gameCamera.transform.eulerAngles.y)), Is.LessThan(1.5f));
            Assert.That(gameCamera.orthographicSize, Is.EqualTo(8f).Within(0.05f));
        }

        [UnityTest]
        public IEnumerator ResolutionTargets_RenderCameraAndKeepHudReferenceLayout()
        {
            yield return LoadInitializedGameShell();
            Scene scene = SceneManager.GetActiveScene();
            GameShellRoot root = FindCompositionRoot(scene);
            Camera gameCamera = root.CameraRig.MovementBasis.GetComponent<Camera>();
            CanvasScaler scaler = scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<CanvasScaler>(false))
                .Single();

            Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1920f, 1080f)));

            CaptureCamera(gameCamera, 1280, 720);
            CaptureCamera(gameCamera, 1920, 1080);
            yield return null;
        }

        [UnityTest]
        public IEnumerator NpcSelectionAndDialogue_LockThenRestoreExploration()
        {
            yield return LoadInitializedGameShell();
            GameShellRoot root = FindCompositionRoot(SceneManager.GetActiveScene());
            PrototypeInteractable npc = SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<PrototypeInteractable>(false))
                .Single(item => item.StableId == "worldline-researcher");
            DialogueFocusController dialogue = SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<DialogueFocusController>(false))
                .Single();
            CharacterController controller = root.PlayerMotor.CharacterController;
            Vector3 approach = npc.transform.position - Vector3.forward * 1.5f;
            PlacePlayer(controller, root.PlayerRoot, approach);
            PlayerFacing facing = root.PlayerRoot.GetComponent<PlayerFacing>();
            facing.FaceTargetExact(npc.FocusPoint.position);
            root.InteractionSensor.RefreshSelection();
            Camera dialogueCamera = root.CameraRig.GameCamera;
            Vector3 cameraPositionBeforeDialogue = dialogueCamera.transform.position;
            Quaternion cameraRotationBeforeDialogue = dialogueCamera.transform.rotation;
            float cameraSizeBeforeDialogue = dialogueCamera.orthographicSize;

            Assert.That(root.InteractionSensor.Current, Is.SameAs(npc));
            Assert.That(root.InteractionSensor.TryInteractCurrent(), Is.True);
            Assert.That(dialogue.IsDialogueActive, Is.True);
            Assert.That(root.InputReader.Gate.IsBlocked(ExplorationInputChannel.Movement), Is.True);
            Assert.That(root.InputReader.Gate.IsBlocked(ExplorationInputChannel.Dash), Is.True);
            Assert.That(root.InputReader.Gate.IsBlocked(ExplorationInputChannel.Interaction), Is.True);
            Assert.That(root.InputReader.Gate.IsBlocked(ExplorationInputChannel.Camera), Is.True);
            Assert.That(Vector3.Distance(dialogueCamera.transform.position, cameraPositionBeforeDialogue), Is.LessThan(0.001f));
            Assert.That(Quaternion.Angle(dialogueCamera.transform.rotation, cameraRotationBeforeDialogue), Is.LessThan(0.01f));
            Assert.That(dialogueCamera.orthographicSize, Is.EqualTo(cameraSizeBeforeDialogue).Within(0.001f));

            yield return null;
            Assert.That(root.PlayerMotor.CurrentHorizontalVelocity.magnitude, Is.LessThan(0.001f));

            dialogue.EndDialogue();
            Assert.That(dialogue.IsDialogueActive, Is.False);
            Assert.That(root.InputReader.Gate.LockedChannels, Is.EqualTo(ExplorationInputChannel.None));
            PlacePlayer(controller, root.PlayerRoot, approach);
            facing.FaceTargetExact(npc.FocusPoint.position);
            root.InteractionSensor.RefreshSelection();
            Assert.That(root.InteractionSensor.Current, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator DialoguePresentationDisable_ReleasesInputLocks()
        {
            yield return LoadInitializedGameShell();
            Scene scene = SceneManager.GetActiveScene();
            GameShellRoot root = FindCompositionRoot(scene);
            PrototypeInteractable researcher = scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<PrototypeInteractable>(false))
                .Single(item => item.StableId == "worldline-researcher");
            DialogueFocusController dialogue = scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<DialogueFocusController>(false))
                .Single();
            DialogueView view = scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<DialogueView>(false))
                .Single();

            PlacePlayer(root.PlayerMotor.CharacterController, root.PlayerRoot, researcher.transform.position - Vector3.forward * 1.5f);
            root.PlayerRoot.GetComponent<PlayerFacing>().FaceTargetExact(researcher.FocusPoint.position);
            root.InteractionSensor.RefreshSelection();
            Assert.That(root.InteractionSensor.TryInteractCurrent(), Is.True);
            Assert.That(dialogue.IsDialogueActive, Is.True);

            view.gameObject.SetActive(false);
            yield return null;

            Assert.That(dialogue.IsDialogueActive, Is.False);
            Assert.That(root.InputReader.Gate.LockedChannels, Is.EqualTo(ExplorationInputChannel.None));
            view.gameObject.SetActive(true);
        }

        [UnityTest]
        public IEnumerator InGamePause_EscapeOpensAndClosesMenuWithInputAndTimeRestored()
        {
            keyboard = CreateIsolatedKeyboard();
            yield return LoadFullyInitializedGameShell();
            GameShellRoot root = FindCompositionRoot(SceneManager.GetActiveScene());
            Assert.That(root.InGameUiCoordinator.isActiveAndEnabled, Is.True);
            Assert.That(root.InputReader.IsMapEnabled, Is.True);

            SetKeyboard(Key.Escape);
            yield return null;
            SetKeyboard();

            Assert.That(root.InGameUiCoordinator.State, Is.EqualTo(InGameMenuState.Root));
            Assert.That(Time.timeScale, Is.EqualTo(0f));
            Assert.That(root.InputReader.Gate.IsBlocked(ExplorationInputChannel.Movement), Is.True);
            Assert.That(root.InputReader.Gate.IsBlocked(ExplorationInputChannel.Camera), Is.True);

            SetKeyboard(Key.Escape);
            yield return null;
            SetKeyboard();

            Assert.That(root.InGameUiCoordinator.State, Is.EqualTo(InGameMenuState.Closed));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(root.InputReader.Gate.LockedChannels, Is.EqualTo(ExplorationInputChannel.None));
        }

        [UnityTest]
        public IEnumerator InGamePause_EscapeDuringDialogueClosesOnlyDialogue()
        {
            keyboard = CreateIsolatedKeyboard();
            yield return LoadFullyInitializedGameShell();
            Scene scene = SceneManager.GetActiveScene();
            GameShellRoot root = FindCompositionRoot(scene);
            PrototypeInteractable researcher = scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<PrototypeInteractable>(false))
                .Single(item => item.StableId == "worldline-researcher");
            DialogueFocusController dialogue = scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<DialogueFocusController>(false))
                .Single();
            PlacePlayer(root.PlayerMotor.CharacterController, root.PlayerRoot, researcher.transform.position - Vector3.forward * 1.5f);
            root.PlayerRoot.GetComponent<PlayerFacing>().FaceTargetExact(researcher.FocusPoint.position);
            root.InteractionSensor.RefreshSelection();
            Assert.That(root.InteractionSensor.TryInteractCurrent(), Is.True);
            Assert.That(dialogue.IsDialogueActive, Is.True);

            SetKeyboard(Key.Escape);
            yield return null;
            SetKeyboard();

            Assert.That(dialogue.IsDialogueActive, Is.False);
            Assert.That(root.InGameUiCoordinator.State, Is.EqualTo(InGameMenuState.Closed));
            Assert.That(root.InputReader.Gate.LockedChannels, Is.EqualTo(ExplorationInputChannel.None));
        }

        [UnityTest]
        public IEnumerator AreaSystem_LoadsLabMapWithMAndRejectsRepeatedPortalTransition()
        {
            keyboard = CreateIsolatedKeyboard();
            yield return LoadGameShell();
            GameShellRoot root = FindCompositionRoot(SceneManager.GetActiveScene());
            InMemoryPlayerSession session = CreateSession();
            SettingsService settings = CreateSettingsService(out _);
            SaveGameProgressUseCase progress = new SaveGameProgressUseCase(
                new FileSaveRepository(
                    Path.Combine(UnityEngine.Application.temporaryCachePath, "DemonLordAreaSystemPlayMode"),
                    new UnityJsonSaveSerializer(),
                    new NoSaveMigrationPipeline()),
                new SystemClock());
            Task<string> initialization = root.InitializeAsync(
                session,
                new EntryDestination(
                    GameShellSceneName,
                    ExplorationAreaIds.WorldAdjustmentLabInterior,
                    ExplorationSpawnIds.ReceptionStart),
                progress,
                settings,
                new TestSceneFlowService(),
                new TestApplicationQuitter());
            while (!initialization.IsCompleted)
            {
                yield return null;
            }

            Assert.That(initialization.IsFaulted, Is.False, initialization.Exception?.ToString());
            Assert.That(initialization.Result, Is.Null);
            Assert.That(SceneManager.GetSceneByName("91_LabInterior").isLoaded, Is.True);
            Assert.That(root.AreaTransitionCoordinator.CurrentAreaRoot.Definition.AreaId,
                Is.EqualTo(ExplorationAreaIds.WorldAdjustmentLabInterior));
            Assert.That(root.AreaTransitionCoordinator.CurrentAreaRoot.ImageMapRenderer, Is.Not.Null);
            Assert.That(root.AreaTransitionCoordinator.CurrentAreaRoot.ImageMapRenderer.Definition.BaseSprite, Is.Not.Null);

            SetKeyboard(Key.M);
            yield return null;
            SetKeyboard();
            Assert.That(root.InGameUiCoordinator.IsMapOpen, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f));

            SetKeyboard(Key.Escape);
            yield return null;
            SetKeyboard();
            Assert.That(root.InGameUiCoordinator.IsMapOpen, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            AreaPortal portal = root.AreaTransitionCoordinator.CurrentAreaRoot.Portals.Single();
            Assert.That(portal.TryInteract(null), Is.True);
            Assert.That(portal.TryInteract(null), Is.False, "A repeated interaction must not start a second transition.");
            float deadline = Time.realtimeSinceStartup + 5f;
            while (root.AreaTransitionCoordinator.IsBusy && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(root.AreaTransitionCoordinator.IsBusy, Is.False);
            Assert.That(root.AreaTransitionCoordinator.CurrentAreaRoot.Definition.AreaId,
                Is.EqualTo(ExplorationAreaIds.BureauCourtyard));
            Assert.That(root.AreaTransitionCoordinator.CurrentAreaRoot.ImageMapRenderer, Is.Not.Null);
            Assert.That(root.AreaTransitionCoordinator.CurrentAreaRoot.ImageMapRenderer.Definition.BaseSprite, Is.Not.Null);
            Assert.That(SceneManager.GetSceneByName("92_BureauCourtyard").isLoaded, Is.True);
            Assert.That(SceneManager.GetSceneByName("91_LabInterior").isLoaded, Is.False);
        }

        [UnityTest]
        public IEnumerator InGamePause_SaveWritesCurrentCheckpointAndKeepsMenuPaused()
        {
            keyboard = CreateIsolatedKeyboard();
            InMemoryPlayerSession session = CreateSession();
            RecordingSaveRepository repository = new RecordingSaveRepository(SaveWriteResult.Success());
            SettingsService settings = CreateSettingsService(out _);
            TestSceneFlowService flow = new TestSceneFlowService();
            yield return LoadGameShellWithPauseServices(
                session,
                new SaveGameProgressUseCase(repository, new SystemClock()),
                settings,
                flow);
            GameShellRoot root = FindCompositionRoot(SceneManager.GetActiveScene());
            yield return OpenPauseMenu();

            FindPauseButton("RootButton_1").onClick.Invoke();

            Assert.That(repository.LastSaved, Is.Not.Null);
            Assert.That(repository.LastSaved.Progress.EntryId, Is.EqualTo(session.CurrentSave.Progress.EntryId));
            Assert.That(repository.LastSaved.Progress.CheckpointId, Is.EqualTo(session.CurrentSave.Progress.CheckpointId));
            Assert.That(session.CurrentSave, Is.SameAs(repository.LastSaved));
            Assert.That(root.InGameUiCoordinator.State, Is.EqualTo(InGameMenuState.Root));
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(FindPauseText("Status").text, Is.EqualTo("기록을 저장했습니다."));
        }

        [UnityTest]
        public IEnumerator InGamePause_SettingsCancelRestoresPersistedRuntimeSettings()
        {
            keyboard = CreateIsolatedKeyboard();
            InMemoryPlayerSession session = CreateSession();
            SettingsService settings = CreateSettingsService(out TestSettingsApplier applier);
            yield return LoadGameShellWithPauseServices(
                session,
                new SaveGameProgressUseCase(new RecordingSaveRepository(SaveWriteResult.Success()), new SystemClock()),
                settings,
                new TestSceneFlowService());
            GameShellRoot root = FindCompositionRoot(SceneManager.GetActiveScene());
            yield return OpenPauseMenu();

            FindPauseButton("RootButton_2").onClick.Invoke();
            FindPauseButton("SettingsPrevious_0").onClick.Invoke();
            Assert.That(settings.Working.MasterVolume, Is.LessThan(settings.Persisted.MasterVolume));

            FindPauseButton("SettingsCancel").onClick.Invoke();

            Assert.That(root.InGameUiCoordinator.State, Is.EqualTo(InGameMenuState.Root));
            Assert.That(settings.Working.SemanticallyEquals(settings.Persisted), Is.True);
            Assert.That(applier.LastApplied.SemanticallyEquals(settings.Persisted), Is.True);
            Assert.That(Time.timeScale, Is.Zero);
        }

        [UnityTest]
        public IEnumerator InGamePause_ReturnToTitleCancelReturnsToRootWithoutLoading()
        {
            keyboard = CreateIsolatedKeyboard();
            InMemoryPlayerSession session = CreateSession();
            TestSceneFlowService flow = new TestSceneFlowService();
            yield return LoadGameShellWithPauseServices(
                session,
                new SaveGameProgressUseCase(new RecordingSaveRepository(SaveWriteResult.Success()), new SystemClock()),
                CreateSettingsService(out _),
                flow);
            GameShellRoot root = FindCompositionRoot(SceneManager.GetActiveScene());
            yield return OpenPauseMenu();

            FindPauseButton("RootButton_4").onClick.Invoke();
            Assert.That(root.InGameUiCoordinator.State, Is.EqualTo(InGameMenuState.ConfirmReturnToTitle));
            FindPauseButton("Cancel").onClick.Invoke();

            Assert.That(root.InGameUiCoordinator.State, Is.EqualTo(InGameMenuState.Root));
            Assert.That(flow.FrontendLoadCount, Is.Zero);
            Assert.That(session.CurrentSave, Is.Not.Null);
            Assert.That(Time.timeScale, Is.Zero);
        }

        [UnityTest]
        public IEnumerator InGamePause_ReturnToTitleUsesMainMenuEntryAndRestoresSessionAfterFailure()
        {
            keyboard = CreateIsolatedKeyboard();
            InMemoryPlayerSession session = CreateSession();
            GameSave originalSave = session.CurrentSave;
            TestSceneFlowService flow = new TestSceneFlowService
            {
                FrontendLoadException = new InvalidOperationException("scene flow failed"),
            };
            yield return LoadGameShellWithPauseServices(
                session,
                new SaveGameProgressUseCase(new RecordingSaveRepository(SaveWriteResult.Success()), new SystemClock()),
                CreateSettingsService(out _),
                flow);
            GameShellRoot root = FindCompositionRoot(SceneManager.GetActiveScene());
            yield return OpenPauseMenu();

            FindPauseButton("RootButton_4").onClick.Invoke();
            LogAssert.Expect(
                LogType.Exception,
                new System.Text.RegularExpressions.Regex("InvalidOperationException: scene flow failed"));
            FindPauseButton("Confirm").onClick.Invoke();
            yield return null;

            Assert.That(flow.FrontendLoadCount, Is.EqualTo(1));
            Assert.That(flow.LastFrontendEntryMode, Is.EqualTo(FrontendEntryMode.MainMenu));
            Assert.That(session.CurrentSave, Is.SameAs(originalSave));
            Assert.That(root.InGameUiCoordinator.State, Is.EqualTo(InGameMenuState.Root));
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(root.InputReader.Gate.IsBlocked(ExplorationInputChannel.Movement), Is.True);
        }

        [UnityTest]
        public IEnumerator InGamePause_ReturnToTitleRejectsDuplicateConfirmationWhileBusy()
        {
            keyboard = CreateIsolatedKeyboard();
            InMemoryPlayerSession session = CreateSession();
            TestSceneFlowService flow = new TestSceneFlowService
            {
                PendingFrontendLoad = new TaskCompletionSource<bool>(),
            };
            yield return LoadGameShellWithPauseServices(
                session,
                new SaveGameProgressUseCase(new RecordingSaveRepository(SaveWriteResult.Success()), new SystemClock()),
                CreateSettingsService(out _),
                flow);
            yield return OpenPauseMenu();

            FindPauseButton("RootButton_4").onClick.Invoke();
            FindPauseButton("Confirm").onClick.Invoke();
            FindPauseButton("Confirm").onClick.Invoke();

            Assert.That(flow.FrontendLoadCount, Is.EqualTo(1));
            Assert.That(session.CurrentSave, Is.Null);
            flow.PendingFrontendLoad.TrySetResult(true);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LabDoors_OpenCloseAndKeepRestrictedDoorLocked()
        {
            yield return LoadInitializedGameShell();
            Scene scene = SceneManager.GetActiveScene();
            GameShellRoot root = FindCompositionRoot(scene);
            LabDoorController openable = scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<LabDoorController>(true))
                .Single(item => item.StableId == "door_taxoffice");
            LabDoorController locked = scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<LabDoorController>(true))
                .Single(item => item.StableId == "door_restricted");

            Assert.That(openable.TryInteract(null), Is.True);
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(openable.State, Is.EqualTo(LabDoorState.Open));

            PlacePlayer(root.PlayerMotor.CharacterController, root.PlayerRoot, openable.transform.position);
            Assert.That(openable.TryInteract(null), Is.False, "A door must not close through the player.");
            Assert.That(openable.State, Is.EqualTo(LabDoorState.Open));

            PlacePlayer(root.PlayerMotor.CharacterController, root.PlayerRoot, new Vector3(-3f, 0.05f, 0f));
            Assert.That(openable.TryInteract(null), Is.True);
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(openable.State, Is.EqualTo(LabDoorState.Closed));

            Assert.That(locked.TryInteract(null), Is.True);
            Assert.That(locked.State, Is.EqualTo(LabDoorState.Locked));
        }

        private IEnumerator LoadInitializedGameShell()
        {
            yield return LoadGameShell();
            GameShellRoot root = FindCompositionRoot(SceneManager.GetActiveScene());
            Assert.That(root.TryInitialize(
                CreateSession(),
                new EntryDestination(GameShellSceneName, "start"),
                out string errorCode), Is.True, errorCode);
            yield return null;
        }

        private IEnumerator LoadFullyInitializedGameShell()
        {
            yield return LoadGameShell();
            GameShellRoot root = FindCompositionRoot(SceneManager.GetActiveScene());
            SettingsService settings = new SettingsService(new TestSettingsRepository(), new TestSettingsApplier());
            settings.LoadAndApply();
            SaveGameProgressUseCase progress = new SaveGameProgressUseCase(
                new FileSaveRepository(
                    Path.Combine(UnityEngine.Application.temporaryCachePath, "DemonLordPauseMenuPlayMode"),
                    new UnityJsonSaveSerializer(),
                    new NoSaveMigrationPipeline()),
                new SystemClock());
            Assert.That(root.TryInitialize(
                CreateSession(),
                new EntryDestination(GameShellSceneName, "start"),
                progress,
                settings,
                new TestSceneFlowService(),
                new TestApplicationQuitter(),
                out string errorCode), Is.True, errorCode);
            yield return null;
        }

        private IEnumerator LoadGameShellWithPauseServices(
            InMemoryPlayerSession session,
            SaveGameProgressUseCase progress,
            SettingsService settings,
            TestSceneFlowService flow)
        {
            yield return LoadGameShell();
            GameShellRoot root = FindCompositionRoot(SceneManager.GetActiveScene());
            Assert.That(root.TryInitialize(
                session,
                new EntryDestination(GameShellSceneName, "start"),
                progress,
                settings,
                flow,
                new TestApplicationQuitter(),
                out string errorCode), Is.True, errorCode);
            yield return null;
        }

        private IEnumerator OpenPauseMenu()
        {
            if (keyboard == null || !keyboard.added)
            {
                keyboard = CreateIsolatedKeyboard();
            }
            GameShellRoot root = FindCompositionRoot(SceneManager.GetActiveScene());
            Assert.That(root.InGameUiCoordinator.isActiveAndEnabled, Is.True);
            Assert.That(root.InputReader.IsMapEnabled, Is.True);
            SetKeyboard(Key.Escape);
            yield return null;
            SetKeyboard();
            Assert.That(FindCompositionRoot(SceneManager.GetActiveScene()).InGameUiCoordinator.State, Is.EqualTo(InGameMenuState.Root));
        }

        private static SettingsService CreateSettingsService(out TestSettingsApplier applier)
        {
            applier = new TestSettingsApplier();
            SettingsService settings = new SettingsService(new TestSettingsRepository(), applier);
            settings.LoadAndApply();
            return settings;
        }

        private static PauseMenuView FindPauseView()
        {
            return SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<PauseMenuView>(true))
                .Single(item => item.gameObject.activeInHierarchy);
        }

        private static Keyboard CreateIsolatedKeyboard()
        {
            foreach (Keyboard existing in InputSystem.devices.OfType<Keyboard>().ToArray())
            {
                InputSystem.RemoveDevice(existing);
            }

            Keyboard created = InputSystem.AddDevice<Keyboard>();
            InputSystem.Update();
            return created;
        }

        private static Mouse CreateIsolatedMouse()
        {
            foreach (Mouse existing in InputSystem.devices.OfType<Mouse>().ToArray())
            {
                InputSystem.RemoveDevice(existing);
            }

            Mouse created = InputSystem.AddDevice<Mouse>();
            InputSystem.Update();
            return created;
        }

        private static Button FindPauseButton(string name)
        {
            return FindPauseView().GetComponentsInChildren<Button>(true)
                .Single(item => item.name == name);
        }

        private static Text FindPauseText(string name)
        {
            return FindPauseView().GetComponentsInChildren<Text>(true)
                .Single(item => item.name == name);
        }

        private static IEnumerator LoadGameShell()
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(GameShellSceneName, LoadSceneMode.Single);
            Assert.That(operation, Is.Not.Null);
            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;
        }

        private static GameShellRoot FindCompositionRoot(Scene scene)
        {
            GameShellRoot[] roots = scene.GetRootGameObjects()
                .Select(item => item.GetComponent<GameShellRoot>())
                .Where(item => item != null)
                .ToArray();
            Assert.That(roots.Length, Is.EqualTo(1));
            return roots[0];
        }

        private static Transform FindTransform(Scene scene, string objectName)
        {
            return scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(item => item.name == objectName);
        }

        private static InMemoryPlayerSession CreateSession()
        {
            Assert.That(SaveSlotId.TryCreate(SaveSlotId.Slot01Value, out SaveSlotId slotId), Is.True);
            Assert.That(NewGameSettings.TryCreate(
                "테스트 마왕",
                DifficultyId.NormalValue,
                TutorialMode.CoreValue,
                out NewGameSettings settings,
                out string errorCode), Is.True, errorCode);
            InMemoryPlayerSession session = new InMemoryPlayerSession();
            session.SetCurrentSave(GameSave.CreateNew(slotId, settings, "playmode-test", DateTime.UtcNow));
            return session;
        }

        private void SetKeyboard(params Key[] pressedKeys)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(pressedKeys));
            InputSystem.Update();
        }

        private void QueueScroll(float y)
        {
            InputSystem.QueueStateEvent(mouse, new MouseState { scroll = new Vector2(0f, y) });
            InputSystem.Update();
            InputSystem.QueueStateEvent(mouse, new MouseState());
            InputSystem.Update();
        }

        private static void PlacePlayer(CharacterController controller, Transform player, Vector3 position)
        {
            bool wasEnabled = controller.enabled;
            controller.enabled = false;
            player.position = position;
            controller.enabled = wasEnabled;
            Physics.SyncTransforms();
        }

        private static float HorizontalDistance(Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            delta.y = 0f;
            return delta.magnitude;
        }

        private static void CaptureCamera(Camera gameCamera, int width, int height)
        {
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = gameCamera.targetTexture;
            RenderTexture target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            Texture2D capture = new Texture2D(width, height, TextureFormat.RGB24, false);

            try
            {
                gameCamera.targetTexture = target;
                RenderTexture.active = target;
                gameCamera.Render();
                capture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                capture.Apply(false, false);

                Color32[] pixels = capture.GetPixels32();
                int minimum = 255;
                int maximum = 0;
                int stride = Mathf.Max(1, pixels.Length / 4096);
                for (int index = 0; index < pixels.Length; index += stride)
                {
                    Color32 pixel = pixels[index];
                    int luminance = (pixel.r + pixel.g + pixel.b) / 3;
                    minimum = Mathf.Min(minimum, luminance);
                    maximum = Mathf.Max(maximum, luminance);
                }

                Assert.That(maximum - minimum, Is.GreaterThan(12), $"{width}x{height} render was blank or uniform.");
                string outputPath = Path.Combine(
                    UnityEngine.Application.persistentDataPath,
                    $"ExplorationQa_{width}x{height}.png");
                File.WriteAllBytes(outputPath, capture.EncodeToPNG());
            }
            finally
            {
                gameCamera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.Destroy(target);
                UnityEngine.Object.Destroy(capture);
            }
        }

        private sealed class TestSettingsApplier : IGameSettingsRuntimeApplier
        {
            public GameSettings LastApplied { get; private set; }

            public void Apply(GameSettings settings)
            {
                LastApplied = settings;
            }
        }

        private sealed class TestSettingsRepository : ISettingsRepository
        {
            public SettingsReadResult Load()
            {
                return SettingsReadResult.Success(GameSettings.Default, false);
            }

            public SettingsWriteResult Save(GameSettings settings)
            {
                return SettingsWriteResult.Success();
            }
        }

        private sealed class TestSceneFlowService : ISceneFlowService
        {
            public int FrontendLoadCount { get; private set; }

            public FrontendEntryMode LastFrontendEntryMode { get; private set; }

            public Exception FrontendLoadException { get; set; }

            public TaskCompletionSource<bool> PendingFrontendLoad { get; set; }

            public Task LoadFrontendAsync(FrontendEntryMode entryMode)
            {
                FrontendLoadCount++;
                LastFrontendEntryMode = entryMode;
                if (FrontendLoadException != null)
                {
                    return Task.FromException(FrontendLoadException);
                }

                return PendingFrontendLoad != null
                    ? PendingFrontendLoad.Task
                    : Task.CompletedTask;
            }

            public Task LoadEntryAsync(EntryDestination destination)
            {
                return Task.CompletedTask;
            }
        }

        private sealed class RecordingSaveRepository : ISaveRepository
        {
            private readonly SaveWriteResult writeResult;

            public RecordingSaveRepository(SaveWriteResult writeResult)
            {
                this.writeResult = writeResult;
            }

            public GameSave LastSaved { get; private set; }

            public System.Collections.Generic.IReadOnlyList<SaveSlotSummary> ListSlots()
            {
                return Array.Empty<SaveSlotSummary>();
            }

            public SaveReadResult Load(SaveSlotId slotId)
            {
                return SaveReadResult.Failure(SaveReadStatus.Empty, "not_used", null);
            }

            public SaveWriteResult Save(GameSave save)
            {
                LastSaved = save;
                return writeResult;
            }

            public SaveWriteResult Delete(SaveSlotId slotId)
            {
                return SaveWriteResult.Success();
            }
        }

        private sealed class TestApplicationQuitter : IApplicationQuitter
        {
            public void Quit()
            {
            }
        }
    }
}
