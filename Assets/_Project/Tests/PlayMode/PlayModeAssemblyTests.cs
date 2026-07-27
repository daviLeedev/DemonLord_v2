using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using DemonLord.Application;
using DemonLord.Bootstrap;
using DemonLord.Domain;
using DemonLord.Infrastructure;
using DemonLord.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DemonLord.Tests.PlayMode
{
    public sealed class PlayModeAssemblyTests
    {
        [Test]
        public void PlayModeTestAssembly_Loads()
        {
            Assert.That(true, Is.True);
        }

        [UnityTest]
        public IEnumerator FrontendView_InitializesOneCameraCanvasAndEventSystem()
        {
            GameObject root = new GameObject("FrontendPlayModeTest");
            FrontendView view = root.AddComponent<FrontendView>();
            FrontendCoordinator coordinator = CreateCoordinator();
            SettingsService settings = new SettingsService(
                new FakeSettingsRepository(),
                new NoOpSettingsApplier());
            settings.LoadAndApply();

            view.Initialize(coordinator, new FakeSceneFlowService(), settings, FrontendEntryMode.Opening);
            yield return null;

            Assert.That(root.GetComponentsInChildren<Canvas>(true).Length, Is.EqualTo(1));
            Assert.That(root.GetComponentsInChildren<Camera>(true).Length, Is.EqualTo(1));
            Assert.That(root.GetComponentsInChildren<EventSystem>(true).Length, Is.EqualTo(1));

            CanvasScaler scaler = root.GetComponentInChildren<CanvasScaler>(true);
            Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1920f, 1080f)));

            Component inputModule = FindComponent(root, "UnityEngine.InputSystem.UI.InputSystemUIInputModule");
            Assert.That(inputModule, Is.Not.Null);
            Assert.That(HasAssignedAction(inputModule, "move"), Is.True);
            Assert.That(HasAssignedAction(inputModule, "submit"), Is.True);
            Assert.That(HasAssignedAction(inputModule, "cancel"), Is.True);
            Assert.That(HasAssignedAction(inputModule, "point"), Is.True);
            Assert.That(HasAssignedAction(inputModule, "leftClick"), Is.True);

            UnityEngine.Object.Destroy(root);
            yield return null;
        }

        private static Component FindComponent(GameObject root, string fullTypeName)
        {
            foreach (Component component in root.GetComponentsInChildren<Component>(true))
            {
                if (component != null && component.GetType().FullName == fullTypeName)
                {
                    return component;
                }
            }

            return null;
        }

        private static bool HasAssignedAction(Component inputModule, string propertyName)
        {
            if (inputModule == null)
            {
                return false;
            }

            PropertyInfo property = inputModule.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            return property != null && property.GetValue(inputModule) != null;
        }

        private static FrontendCoordinator CreateCoordinator()
        {
            return new FrontendCoordinator(
                new ListSaveSlotsUseCase(new EmptySaveRepository()),
                new CreateNewGameUseCase(new EmptySaveRepository(), new FixedClock()),
                new LoadGameUseCase(new EmptySaveRepository()),
                new InMemoryPlayerSession(),
                new EntryPointResolver());
        }

        private sealed class EmptySaveRepository : ISaveRepository
        {
            public IReadOnlyList<SaveSlotSummary> ListSlots()
            {
                List<SaveSlotSummary> slots = new List<SaveSlotSummary>();
                foreach (string value in SaveSlotId.AllValues)
                {
                    SaveSlotId.TryCreate(value, out SaveSlotId slotId);
                    slots.Add(SaveSlotSummary.Empty(slotId));
                }

                return slots;
            }

            public SaveReadResult Load(SaveSlotId slotId)
            {
                return SaveReadResult.Failure(SaveReadStatus.Empty, "save_not_found", null);
            }

            public SaveWriteResult Save(GameSave save)
            {
                return SaveWriteResult.Success();
            }

            public SaveWriteResult Delete(SaveSlotId slotId)
            {
                return SaveWriteResult.Success();
            }
        }

        private sealed class FixedClock : IClock
        {
            public DateTime UtcNow => DateTime.UtcNow;
        }

        private sealed class FakeSettingsRepository : ISettingsRepository
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

        private sealed class NoOpSettingsApplier : IGameSettingsRuntimeApplier
        {
            public void Apply(GameSettings settings)
            {
            }
        }

        private sealed class FakeSceneFlowService : ISceneFlowService
        {
            public Task LoadFrontendAsync(FrontendEntryMode entryMode)
            {
                return Task.CompletedTask;
            }

            public Task LoadEntryAsync(EntryDestination destination)
            {
                return Task.CompletedTask;
            }
        }
    }
}
