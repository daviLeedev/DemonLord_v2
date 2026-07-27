using DemonLord.Presentation.Exploration;
using NUnit.Framework;
using UnityEngine;

namespace DemonLord.Tests.EditMode
{
    public sealed class QuarterViewCameraProfileTests
    {
        [TestCase(0, 45f)]
        [TestCase(1, 135f)]
        [TestCase(2, 225f)]
        [TestCase(3, 315f)]
        [TestCase(4, 45f)]
        [TestCase(-1, 315f)]
        public void ResolveYawDegrees_UsesExactNormalizedQuarterTurns(int quarterIndex, float expectedYaw)
        {
            QuarterViewCameraProfile profile = new QuarterViewCameraProfile(
                45f,
                35f,
                8f,
                Vector3.zero,
                0.25f);

            Assert.That(profile.ResolveYawDegrees(quarterIndex), Is.EqualTo(expectedYaw).Within(0.0001f));
        }
    }
}
