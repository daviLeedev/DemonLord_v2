using DemonLord.Application;
using DemonLord.Presentation.Exploration;
using NUnit.Framework;

namespace DemonLord.Tests.EditMode
{
    public sealed class InGameMenuStateTests
    {
        [Test]
        public void Back_ClosesRootAndReturnsChildScreensToRootDeterministically()
        {
            InGameMenuStateMachine stateMachine = new InGameMenuStateMachine();

            Assert.That(stateMachine.TryOpenRoot(), Is.True);
            Assert.That(stateMachine.TryOpenSettings(), Is.True);
            Assert.That(stateMachine.TryBack(), Is.EqualTo(InGameMenuBackResult.ReturnToRoot));
            Assert.That(stateMachine.State, Is.EqualTo(InGameMenuState.Root));
            Assert.That(stateMachine.TryBack(), Is.EqualTo(InGameMenuBackResult.CloseMenu));
            Assert.That(stateMachine.State, Is.EqualTo(InGameMenuState.Closed));
        }

        [Test]
        public void Busy_RejectsDuplicateCommandsAndRestoresItsPreviousScreenOnFailure()
        {
            InGameMenuStateMachine stateMachine = new InGameMenuStateMachine();

            Assert.That(stateMachine.TryOpenRoot(), Is.True);
            Assert.That(stateMachine.TryOpenSettings(), Is.True);
            Assert.That(stateMachine.TryBeginBusy(), Is.True);
            Assert.That(stateMachine.TryBeginBusy(), Is.False);
            Assert.That(stateMachine.TryOpenControls(), Is.False);
            Assert.That(stateMachine.TryCompleteBusy(false), Is.True);
            Assert.That(stateMachine.State, Is.EqualTo(InGameMenuState.Settings));
        }

        [Test]
        public void CompletedBusy_ReturnsToRootInsteadOfLeavingAStaleSubscreen()
        {
            InGameMenuStateMachine stateMachine = new InGameMenuStateMachine();

            Assert.That(stateMachine.TryOpenRoot(), Is.True);
            Assert.That(stateMachine.TryConfirmReturnToTitle(), Is.True);
            Assert.That(stateMachine.TryBeginBusy(), Is.True);
            Assert.That(stateMachine.TryCompleteBusy(true), Is.True);
            Assert.That(stateMachine.State, Is.EqualTo(InGameMenuState.Root));
        }

        [Test]
        public void LocationPriority_HigherPriorityThenStableIdWins()
        {
            Assert.That(LocationTracker.IsCandidatePreferred(20, "archive", 10, "reception"), Is.True);
            Assert.That(LocationTracker.IsCandidatePreferred(10, "analysis_lab", 10, "archive"), Is.True);
            Assert.That(LocationTracker.IsCandidatePreferred(10, "tax_office", 10, "archive"), Is.False);
        }

        [Test]
        public void EmptyReadModels_KeepHudStateInExplicitPlaceholderMode()
        {
            using (InGameHudStateSource source = new InGameHudStateSource(
                       "세계조정국 연구실",
                       "중앙 접수실",
                       new EmptyWalletReadModel(),
                       new EmptyGameTimeReadModel()))
            {
                InGameHudState state = source.Current;

                Assert.That(state.HasCurrency, Is.False);
                Assert.That(state.HasGameTime, Is.False);
                Assert.That(state.RoomName, Is.EqualTo("중앙 접수실"));
            }
        }
    }
}
