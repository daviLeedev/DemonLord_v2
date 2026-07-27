using System;
using DemonLord.Presentation.Exploration;
using NUnit.Framework;
using UnityEngine;

namespace DemonLord.Tests.EditMode
{
    public sealed class ExplorationMovementTests
    {
        [TestCase(0f)]
        [TestCase(90f)]
        [TestCase(180f)]
        [TestCase(270f)]
        public void CameraRelativeMove_ForwardInputTracksPitchedCameraYaw(float yaw)
        {
            Quaternion rotation = Quaternion.Euler(35f, yaw, 0f);
            Vector3 cameraForward = rotation * Vector3.forward;
            Vector3 cameraRight = rotation * Vector3.right;

            Vector3 movement = ExplorationMath.CameraRelativeMove(
                Vector2.up,
                cameraForward,
                cameraRight);
            Vector3 expected = Vector3.ProjectOnPlane(cameraForward, Vector3.up).normalized;

            Assert.That(movement.y, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(Vector3.Dot(movement, expected), Is.GreaterThan(0.9999f));
        }

        [Test]
        public void CameraRelativeMove_DiagonalIsNotFasterThanCardinal()
        {
            Vector3 cardinal = ExplorationMath.CameraRelativeMove(
                Vector2.up,
                new Vector3(0f, -0.5f, 1f),
                Vector3.right);
            Vector3 diagonal = ExplorationMath.CameraRelativeMove(
                new Vector2(1f, 1f),
                new Vector3(0f, -0.5f, 1f),
                Vector3.right);

            Assert.That(cardinal.magnitude, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(diagonal.magnitude, Is.EqualTo(cardinal.magnitude).Within(0.0001f));
        }

        [TestCase(0f, FacingDirection8.North)]
        [TestCase(45f, FacingDirection8.NorthEast)]
        [TestCase(90f, FacingDirection8.East)]
        [TestCase(135f, FacingDirection8.SouthEast)]
        [TestCase(180f, FacingDirection8.South)]
        [TestCase(225f, FacingDirection8.SouthWest)]
        [TestCase(270f, FacingDirection8.West)]
        [TestCase(315f, FacingDirection8.NorthWest)]
        public void QuantizeFacing_ReturnsExpectedEightWayDirection(
            float yaw,
            FacingDirection8 expected)
        {
            Vector3 direction = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;

            Assert.That(ExplorationMath.QuantizeFacing(direction), Is.EqualTo(expected));
        }

        [Test]
        public void PlayerFacing_ZeroInputKeepsLastDirection_AndExactTargetCanBeRestored()
        {
            GameObject root = new GameObject("FacingTestRoot");
            GameObject visual = new GameObject("FacingTestVisual");
            visual.transform.SetParent(root.transform, false);

            try
            {
                PlayerFacing facing = root.AddComponent<PlayerFacing>();
                facing.SetVisualRoot(visual.transform);
                Assert.That(facing.FaceMovementDirection(Vector3.right), Is.True);
                Assert.That(facing.CurrentDirection, Is.EqualTo(FacingDirection8.East));

                Assert.That(facing.FaceMovementDirection(Vector3.zero), Is.False);
                Assert.That(facing.CurrentDirection, Is.EqualTo(FacingDirection8.East));

                PlayerFacingState snapshot = facing.CaptureState();
                Assert.That(facing.FaceTargetExact(new Vector3(1f, 0f, 1f)), Is.True);
                Assert.That(facing.IsExactFacing, Is.True);
                Assert.That(facing.CurrentYaw, Is.EqualTo(45f).Within(0.001f));

                facing.RestoreState(snapshot);
                Assert.That(facing.CurrentDirection, Is.EqualTo(FacingDirection8.East));
                Assert.That(facing.IsExactFacing, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Dash_UsesCurrentMovementBeforeLastFacing()
        {
            DashRuntimeState state = new DashRuntimeState();

            Assert.That(state.TryStart(Vector3.right, Vector3.forward, 0f, 3f, 0.18f, 0.65f), Is.True);
            Assert.That(Vector3.Dot(state.Direction, Vector3.right), Is.GreaterThan(0.9999f));
        }

        [Test]
        public void Dash_UsesLastFacingWhenMovementIsZero()
        {
            DashRuntimeState state = new DashRuntimeState();

            Assert.That(state.TryStart(Vector3.zero, Vector3.back, 0f, 3f, 0.18f, 0.65f), Is.True);
            Assert.That(Vector3.Dot(state.Direction, Vector3.back), Is.GreaterThan(0.9999f));
        }

        [TestCase(1f / 30f)]
        [TestCase(1f / 60f)]
        [TestCase(1f / 144f)]
        public void Dash_DistanceBudgetIsFrameRateIndependent(float deltaTime)
        {
            DashRuntimeState state = new DashRuntimeState();
            Assert.That(state.TryStart(Vector3.forward, Vector3.back, 0f, 3f, 0.18f, 0.65f), Is.True);

            float travelled = 0f;
            int guard = 0;
            while (state.IsActive && guard++ < 1000)
            {
                travelled += state.Tick(deltaTime).magnitude;
            }

            Assert.That(guard, Is.LessThan(1000));
            Assert.That(travelled, Is.EqualTo(3f).Within(0.001f));
        }

        [Test]
        public void Dash_RejectsOverlapAndCooldownThenRecovers()
        {
            DashRuntimeState state = new DashRuntimeState();
            Assert.That(state.TryStart(Vector3.forward, Vector3.forward, 0f, 3f, 0.18f, 0.65f), Is.True);
            Assert.That(state.TryStart(Vector3.forward, Vector3.forward, 0.1f, 3f, 0.18f, 0.65f), Is.False);

            state.Tick(0.18f);
            Assert.That(state.IsActive, Is.False);
            Assert.That(state.TryStart(Vector3.forward, Vector3.forward, 0.64f, 3f, 0.18f, 0.65f), Is.False);
            Assert.That(state.TryStart(Vector3.forward, Vector3.forward, 0.65f, 3f, 0.18f, 0.65f), Is.True);
        }

        [Test]
        public void InputGate_NestedTokensRestoreOnlyTheirOwnChannels()
        {
            ExplorationInputGate gate = new ExplorationInputGate();
            IDisposable movementLock = gate.AcquireLock(ExplorationInputChannel.Locomotion);
            IDisposable cameraLock = gate.AcquireLock(ExplorationInputChannel.Camera);

            Assert.That(gate.IsBlocked(ExplorationInputChannel.Movement), Is.True);
            Assert.That(gate.IsBlocked(ExplorationInputChannel.Dash), Is.True);
            Assert.That(gate.IsBlocked(ExplorationInputChannel.Camera), Is.True);
            Assert.That(gate.IsAllowed(ExplorationInputChannel.Interaction), Is.True);

            movementLock.Dispose();
            Assert.That(gate.IsAllowed(ExplorationInputChannel.Movement), Is.True);
            Assert.That(gate.IsAllowed(ExplorationInputChannel.Dash), Is.True);
            Assert.That(gate.IsBlocked(ExplorationInputChannel.Camera), Is.True);

            cameraLock.Dispose();
            Assert.That(gate.LockedChannels, Is.EqualTo(ExplorationInputChannel.None));
        }
    }
}
