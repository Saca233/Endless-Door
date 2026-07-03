using NUnit.Framework;
using UnityEngine;

namespace OwariNakiTobira.Tests
{
    public sealed class PlayerStateTransitionTests
    {
        [Test]
        public void TransitionRulesAllowExpectedLocomotionFlow()
        {
            Assert.IsTrue(PlayerStateTransitionRules.IsValid(PlayerStateId.Idle, PlayerStateId.Run));
            Assert.IsTrue(PlayerStateTransitionRules.IsValid(PlayerStateId.Run, PlayerStateId.Jump));
            Assert.IsTrue(PlayerStateTransitionRules.IsValid(PlayerStateId.Jump, PlayerStateId.Fall));
            Assert.IsTrue(PlayerStateTransitionRules.IsValid(PlayerStateId.Fall, PlayerStateId.Land));
            Assert.IsTrue(PlayerStateTransitionRules.IsValid(PlayerStateId.Land, PlayerStateId.Idle));
        }

        [Test]
        public void TransitionRulesRejectInvalidJumpToRunSkip()
        {
            Assert.IsFalse(PlayerStateTransitionRules.IsValid(PlayerStateId.Jump, PlayerStateId.Run));
        }

        [Test]
        public void StateMachineRejectsInvalidExplicitTransition()
        {
            GameObject player = new GameObject("StateMachineTestPlayer");
            try
            {
                player.AddComponent<Rigidbody>();
                player.AddComponent<CapsuleCollider>();
                player.AddComponent<SideScrollerMotor>();
                PlayerStateMachine stateMachine = player.AddComponent<PlayerStateMachine>();

                Assert.IsTrue(stateMachine.TransitionTo(PlayerStateId.Run));
                Assert.IsTrue(stateMachine.TransitionTo(PlayerStateId.Jump));
                Assert.IsFalse(stateMachine.TransitionTo(PlayerStateId.Run));
                Assert.AreEqual(PlayerStateId.Jump, stateMachine.CurrentStateId);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }
    }
}
