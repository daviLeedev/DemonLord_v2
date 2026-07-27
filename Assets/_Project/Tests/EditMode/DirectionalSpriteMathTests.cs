using DemonLord.Presentation.Exploration;
using NUnit.Framework;

namespace DemonLord.Tests.EditMode
{
    public sealed class DirectionalSpriteMathTests
    {
        [TestCase(FacingDirection8.North, 0f, FacingDirection8.North)]
        [TestCase(FacingDirection8.East, 0f, FacingDirection8.East)]
        [TestCase(FacingDirection8.North, 90f, FacingDirection8.West)]
        [TestCase(FacingDirection8.East, 90f, FacingDirection8.North)]
        [TestCase(FacingDirection8.SouthWest, 45f, FacingDirection8.South)]
        [TestCase(FacingDirection8.NorthWest, 315f, FacingDirection8.North)]
        public void ToCameraRelative_MapsEightWayFacingFromCameraYaw(
            FacingDirection8 worldDirection,
            float cameraYaw,
            FacingDirection8 expected)
        {
            Assert.That(DirectionalSpriteMath.ToCameraRelative(worldDirection, cameraYaw), Is.EqualTo(expected));
        }

        [Test]
        public void DoorAccessPolicy_AlwaysLockedUsesConfiguredKoreanMessage()
        {
            DoorAccessPolicy policy = new DoorAccessPolicy();
            policy.Configure(DoorAccessRequirement.AlwaysLocked, "접근 권한이 없습니다. 문이 잠겨 있습니다.");

            Assert.That(policy.IsLocked, Is.True);
            Assert.That(policy.DeniedMessage, Is.EqualTo("접근 권한이 없습니다. 문이 잠겨 있습니다."));
        }
    }
}
