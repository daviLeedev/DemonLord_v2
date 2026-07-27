using System;
using DemonLord.Application;
using DemonLord.Domain;
using DemonLord.Presentation.Exploration;
using NUnit.Framework;
using UnityEngine;

namespace DemonLord.Tests.EditMode
{
    public sealed class AreaSystemTests
    {
        [TestCase("valid_id", true)]
        [TestCase("valid-id-2", true)]
        [TestCase("Upper", false)]
        [TestCase("space id", false)]
        [TestCase("", false)]
        public void StableWorldId_UsesPortableRestrictedAlphabet(string value, bool expected)
        {
            Assert.That(StableWorldId.IsValid(value), Is.EqualTo(expected));
        }

        [Test]
        public void ExplorationLocation_RejectsInvalidAreaAndSpawnIndependently()
        {
            Assert.That(
                ExplorationLocation.TryCreate("Bad Area", "spawn", out _, out string areaError),
                Is.False);
            Assert.That(areaError, Is.EqualTo("invalid_area_id"));
            Assert.That(
                ExplorationLocation.TryCreate("area", "Bad Spawn", out _, out string spawnError),
                Is.False);
            Assert.That(spawnError, Is.EqualTo("invalid_spawn_id"));
        }

        [Test]
        public void AreaTransitionStateMachine_AllowsOnlyTheDeclaredSequenceAndRollback()
        {
            AreaTransitionStateMachine state = new AreaTransitionStateMachine();
            Assert.That(state.TryBegin(), Is.True);
            Assert.That(state.TryBegin(), Is.False);
            Assert.That(state.TryAdvance(AreaTransitionState.FadingOut, AreaTransitionState.Validating), Is.False);
            Assert.That(state.TryAdvance(AreaTransitionState.FadingOut, AreaTransitionState.Loading), Is.True);
            Assert.That(state.TryAdvance(AreaTransitionState.Loading, AreaTransitionState.Validating), Is.True);
            Assert.That(state.TryBeginRollback(), Is.True);
            Assert.That(state.TryComplete(), Is.True);
            Assert.That(state.State, Is.EqualTo(AreaTransitionState.Idle));
        }

        [Test]
        public void AreaRegistry_ValidatesKnownAreasAndRejectsDuplicateIds()
        {
            AreaMapDefinition labMap = CreateMap("floor-1");
            AreaMapDefinition yardMap = CreateMap("ground");
            AreaDefinition lab = CreateArea(
                ExplorationAreaIds.WorldAdjustmentLabInterior,
                "91_LabInterior",
                ExplorationSpawnIds.ReceptionStart,
                labMap);
            AreaDefinition yard = CreateArea(
                ExplorationAreaIds.BureauCourtyard,
                "92_BureauCourtyard",
                ExplorationSpawnIds.LabExit,
                yardMap);
            AreaRegistry registry = ScriptableObject.CreateInstance<AreaRegistry>();
            registry.Configure(new[] { lab, yard });

            Assert.That(registry.TryValidate(out string errorCode), Is.True, errorCode);
            Assert.That(registry.TryGet(ExplorationAreaIds.BureauCourtyard, out AreaDefinition found), Is.True);
            Assert.That(found, Is.SameAs(yard));
            Assert.That(registry.TryGet("unknown_area", out _), Is.False);

            AreaDefinition duplicate = CreateArea(
                ExplorationAreaIds.BureauCourtyard,
                "93_Duplicate",
                ExplorationSpawnIds.LabExit,
                yardMap);
            registry.Configure(new[] { yard, duplicate });
            Assert.That(registry.TryValidate(out errorCode), Is.False);
            Assert.That(errorCode, Is.EqualTo("area_id_duplicate"));

            UnityEngine.Object.DestroyImmediate(registry);
            UnityEngine.Object.DestroyImmediate(duplicate);
            UnityEngine.Object.DestroyImmediate(yard);
            UnityEngine.Object.DestroyImmediate(lab);
            UnityEngine.Object.DestroyImmediate(yardMap);
            UnityEngine.Object.DestroyImmediate(labMap);
        }

        [Test]
        public void MapProjection_MapsCornersCenterOutsideAndRotatedAxes()
        {
            MapFloorDefinition floor = CreateFloor(
                "floor-1",
                Vector3.zero,
                Vector3.right,
                Vector3.forward,
                new Vector2(20f, 10f));

            AssertProjected(floor, Vector3.zero, new Vector2(0f, 0f));
            AssertProjected(floor, new Vector3(20f, 0f, 10f), new Vector2(1f, 1f));
            AssertProjected(floor, new Vector3(10f, 0f, 5f), new Vector2(0.5f, 0.5f));
            AssertProjected(floor, new Vector3(25f, 0f, -2f), new Vector2(1.25f, -0.2f));
            Assert.That(
                MapProjection.NormalizedToRect(new Vector2(1.25f, -0.2f), new Vector2(200f, 100f), true),
                Is.EqualTo(new Vector2(100f, -50f)));

            MapFloorDefinition rotated = CreateFloor(
                "rotated",
                new Vector3(10f, 0f, 10f),
                Vector3.forward,
                Vector3.left,
                new Vector2(10f, 20f));
            AssertProjected(rotated, new Vector3(0f, 0f, 15f), new Vector2(0.5f, 0.5f));
        }

        [Test]
        public void MapProjection_RejectsInvalidFloorSize()
        {
            MapFloorDefinition floor = CreateFloor(
                "invalid",
                Vector3.zero,
                Vector3.right,
                Vector3.forward,
                new Vector2(0f, 10f));
            Assert.That(MapProjection.TryWorldToNormalized(Vector3.zero, floor, out _), Is.False);
        }

        private static void AssertProjected(MapFloorDefinition floor, Vector3 world, Vector2 expected)
        {
            Assert.That(MapProjection.TryWorldToNormalized(world, floor, out Vector2 actual), Is.True);
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.0001f));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.0001f));
        }

        private static AreaMapDefinition CreateMap(string floorId)
        {
            AreaMapDefinition map = ScriptableObject.CreateInstance<AreaMapDefinition>();
            map.Configure(new[]
            {
                CreateFloor(floorId, Vector3.zero, Vector3.right, Vector3.forward, new Vector2(20f, 20f)),
            });
            return map;
        }

        private static MapFloorDefinition CreateFloor(
            string floorId,
            Vector3 origin,
            Vector3 axisX,
            Vector3 axisY,
            Vector2 size)
        {
            MapFloorDefinition floor = new MapFloorDefinition();
            floor.Configure(
                floorId,
                floorId,
                null,
                origin,
                axisX,
                axisY,
                size,
                new Vector2(10f, 8f));
            return floor;
        }

        private static AreaDefinition CreateArea(
            string areaId,
            string sceneKey,
            string spawnId,
            AreaMapDefinition map)
        {
            AreaDefinition definition = ScriptableObject.CreateInstance<AreaDefinition>();
            definition.Configure(areaId, sceneKey, "display.key", areaId, AreaKind.Interior, spawnId, map);
            return definition;
        }
    }
}
