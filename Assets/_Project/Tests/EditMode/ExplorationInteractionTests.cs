using DemonLord.Presentation.Exploration;
using NUnit.Framework;
using UnityEngine;

namespace DemonLord.Tests.EditMode
{
    public sealed class ExplorationInteractionTests
    {
        [Test]
        public void CandidateOutsideForwardCone_IsRejected()
        {
            bool accepted = InteractionSelection.TryCreateScore(
                Vector3.zero,
                Vector3.forward,
                Vector3.back,
                2.2f,
                100f,
                "behind",
                out _);

            Assert.That(accepted, Is.False);
        }

        [Test]
        public void CandidateOutsideRadius_IsRejected()
        {
            bool accepted = InteractionSelection.TryCreateScore(
                Vector3.zero,
                Vector3.forward,
                new Vector3(0f, 0f, 2.3f),
                2.2f,
                100f,
                "far",
                out _);

            Assert.That(accepted, Is.False);
        }

        [Test]
        public void BetterAlignment_WinsBeforeDistance()
        {
            Assert.That(InteractionSelection.TryCreateScore(
                Vector3.zero,
                Vector3.forward,
                new Vector3(0f, 0f, 2f),
                3f,
                120f,
                "aligned",
                out InteractionCandidateScore aligned), Is.True);
            Assert.That(InteractionSelection.TryCreateScore(
                Vector3.zero,
                Vector3.forward,
                new Vector3(0.4f, 0f, 0.8f),
                3f,
                120f,
                "near",
                out InteractionCandidateScore near), Is.True);

            Assert.That(InteractionSelection.IsBetter(aligned, near), Is.True);
        }

        [Test]
        public void EqualAlignment_UsesShorterDistance()
        {
            InteractionCandidateScore far = new InteractionCandidateScore("far", 1f, 4f);
            InteractionCandidateScore near = new InteractionCandidateScore("near", 1f, 1f);

            Assert.That(InteractionSelection.IsBetter(near, far), Is.True);
        }

        [Test]
        public void EqualScore_UsesStableIdOrdinalTieBreak()
        {
            InteractionCandidateScore second = new InteractionCandidateScore("npc-b", 0.9f, 1f);
            InteractionCandidateScore first = new InteractionCandidateScore("npc-a", 0.9f, 1f);

            Assert.That(InteractionSelection.IsBetter(first, second), Is.True);
            Assert.That(InteractionSelection.IsBetter(second, first), Is.False);
        }
    }
}
