using DemonLord.Presentation.Exploration;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DemonLord.Tests.EditMode
{
    public sealed class DirectionalSpriteMathTests
    {
        private const string TaxOfficerAnimationSetPath =
            "Assets/_Project/ScriptableObjects/Exploration/TaxOfficerSdDirectionalAnimationSet.asset";

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

        [TestCase(FacingDirection8.North, FacingDirection8.South, FacingDirection8.North)]
        [TestCase(FacingDirection8.East, FacingDirection8.North, FacingDirection8.East)]
        [TestCase(FacingDirection8.NorthEast, FacingDirection8.East, FacingDirection8.East)]
        [TestCase(FacingDirection8.NorthEast, FacingDirection8.South, FacingDirection8.North)]
        [TestCase(FacingDirection8.SouthWest, FacingDirection8.West, FacingDirection8.West)]
        [TestCase(FacingDirection8.SouthWest, FacingDirection8.North, FacingDirection8.South)]
        public void CollapseToCardinal_UsesCardinalAxisAndPreservesDiagonalIntent(
            FacingDirection8 cameraRelative,
            FacingDirection8 lastCardinal,
            FacingDirection8 expected)
        {
            Assert.That(
                DirectionalSpriteMath.CollapseToCardinal(cameraRelative, lastCardinal),
                Is.EqualTo(expected));
        }

        [TestCase(FacingDirection8.North, "_up_")]
        [TestCase(FacingDirection8.NorthEast, "_up_right_")]
        [TestCase(FacingDirection8.East, "_right_")]
        [TestCase(FacingDirection8.SouthEast, "_down_right_")]
        [TestCase(FacingDirection8.South, "_down_")]
        [TestCase(FacingDirection8.SouthWest, "_down_left_")]
        [TestCase(FacingDirection8.West, "_left_")]
        [TestCase(FacingDirection8.NorthWest, "_up_left_")]
        public void TaxOfficerAnimationSet_UsesDirectScreenFacingWithoutMirroring(
            FacingDirection8 direction,
            string expectedSpriteNameToken)
        {
            DirectionalAnimationSet animationSet =
                AssetDatabase.LoadAssetAtPath<DirectionalAnimationSet>(TaxOfficerAnimationSetPath);

            Assert.That(animationSet, Is.Not.Null);
            Sprite sprite = animationSet.GetSprite(DirectionalAnimationState.Walk, direction, 0f);
            Assert.That(sprite, Is.Not.Null);
            Assert.That(sprite.name, Does.Contain(expectedSpriteNameToken));
        }

        [Test]
        public void LabNavigationPresentationContract_UsesExpectedFixedProjection()
        {
            Assert.That(LabNavigationPresentationContract.ReferenceYaw, Is.EqualTo(45f));
            Assert.That(LabNavigationPresentationContract.ReferencePitch, Is.EqualTo(35f));
            Assert.That(LabNavigationPresentationContract.ReferenceOrthographicSize, Is.EqualTo(18f));
            Assert.That(LabNavigationPresentationContract.OutputWidth, Is.EqualTo(1672));
            Assert.That(LabNavigationPresentationContract.OutputHeight, Is.EqualTo(941));
            Assert.That(LabNavigationPresentationContract.MapWorldSize.x, Is.EqualTo(30f));
            Assert.That(LabNavigationPresentationContract.MapWorldSize.y, Is.EqualTo(34f));
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
