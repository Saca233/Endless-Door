using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace OwariNakiTobira.Tests
{
    public sealed class Level03EndingTests
    {
        [Test]
        public void PlayerTransferActivatesDesktopRepresentationAndDisablesWindowRepresentation()
        {
            GameObject host = new GameObject("TransferHost");
            GameObject windowPlayer = CreateInputRepresentation("WindowPlayer");
            GameObject desktopPlayer = CreateInputRepresentation("DesktopPlayer");
            PlayerRepresentationTransfer transfer = host.AddComponent<PlayerRepresentationTransfer>();
            try
            {
                AssignObject(transfer, "windowRepresentation", windowPlayer);
                AssignObject(transfer, "desktopRepresentation", desktopPlayer);
                transfer.ResetToWindowRepresentation();

                Assert.AreSame(windowPlayer, transfer.ActiveRepresentation);
                Assert.AreEqual(1, transfer.CountActiveInputReceivers());

                Assert.IsTrue(transfer.TransferToDesktop());

                Assert.AreSame(desktopPlayer, transfer.ActiveRepresentation);
                Assert.IsFalse(windowPlayer.activeSelf);
                Assert.IsTrue(desktopPlayer.activeSelf);
                Assert.AreEqual(1, transfer.CountActiveInputReceivers());
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(windowPlayer);
                Object.DestroyImmediate(desktopPlayer);
            }
        }

        [Test]
        public void FormattingStateTransitionsFollowRequiredOrder()
        {
            Assert.IsTrue(FormattingSequenceController.CanTransition(FormattingSequenceState.Idle, FormattingSequenceState.Warning));
            Assert.IsTrue(FormattingSequenceController.CanTransition(FormattingSequenceState.Warning, FormattingSequenceState.DoorAppearing));
            Assert.IsTrue(FormattingSequenceController.CanTransition(FormattingSequenceState.Blackout, FormattingSequenceState.Complete));
            Assert.IsFalse(FormattingSequenceController.CanTransition(FormattingSequenceState.Warning, FormattingSequenceState.Sacrifice));
            Assert.IsFalse(FormattingSequenceController.CanTransition(FormattingSequenceState.Complete, FormattingSequenceState.Warning));
        }

        [Test]
        public void DesktopDataObjectRuntimeResetRestoresOriginalTransform()
        {
            GameObject data = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject target = new GameObject("PullTarget");
            try
            {
                data.transform.position = new Vector3(1f, 2f, 3f);
                data.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
                data.transform.localScale = new Vector3(2f, 1f, 1f);
                DesktopDataObject dataObject = data.AddComponent<DesktopDataObject>();
                dataObject.CaptureOriginalTransform();
                target.transform.position = new Vector3(6f, 2f, 3f);

                dataObject.BeginPull(target.transform);
                dataObject.StepPull(10f);
                Assert.AreNotEqual(new Vector3(1f, 2f, 3f), data.transform.position);

                dataObject.RuntimeReset();

                Assert.AreEqual(new Vector3(1f, 2f, 3f), data.transform.position);
                Assert.AreEqual(new Vector3(2f, 1f, 1f), data.transform.localScale);
                Assert.IsFalse(dataObject.IsPulling);
            }
            finally
            {
                Object.DestroyImmediate(data);
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void TransientResetClearsTemporaryFlagsAndPreservesLoopCount()
        {
            GameObject host = new GameObject("ResetHost");
            try
            {
                StoryFlagService flags = host.AddComponent<StoryFlagService>();
                LoopStateService loopState = host.AddComponent<LoopStateService>();
                RuntimeResetCoordinator coordinator = host.AddComponent<RuntimeResetCoordinator>();
                coordinator.SetCoreServices(flags, null, null, null, null);

                flags.SetBool("temporary", true, true);
                flags.SetBool("persistent", true, false);
                loopState.IncrementLoop();

                coordinator.ExecuteTransientReset();
                loopState.RuntimeReset();

                Assert.IsFalse(flags.TryGetBool("temporary", out _));
                Assert.IsTrue(flags.GetBool("persistent"));
                Assert.AreEqual(1, loopState.LoopCount);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void RuntimeResetCoordinatorClearsPlayerInputLocks()
        {
            GameObject host = new GameObject("LockHost");
            try
            {
                PlayerControlGate gate = host.AddComponent<PlayerControlGate>();
                RuntimeResetCoordinator coordinator = host.AddComponent<RuntimeResetCoordinator>();
                coordinator.SetCoreServices(null, null, null, gate, null);

                gate.AcquireLock("A");
                gate.AcquireLock("B");
                coordinator.ExecuteTransientReset();

                Assert.AreEqual(0, gate.ActiveLockCount);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void Level01StoryTriggerCanReactivateAfterLoopReset()
        {
            GameObject triggerObject = new GameObject("Level01Trigger");
            try
            {
                triggerObject.AddComponent<BoxCollider>();
                StoryTriggerVolume trigger = triggerObject.AddComponent<StoryTriggerVolume>();
                RuntimeResetCoordinator coordinator = triggerObject.AddComponent<RuntimeResetCoordinator>();
                coordinator.SetResetTargets(new[] { trigger }, null, null, null, null, null, null);

                Assert.IsTrue(trigger.TryFire(triggerObject));
                Assert.IsFalse(trigger.TryFire(triggerObject));

                coordinator.ExecuteTransientReset();

                Assert.IsFalse(trigger.FiredThisLoop);
                Assert.IsTrue(trigger.TryFire(triggerObject));
            }
            finally
            {
                Object.DestroyImmediate(triggerObject);
            }
        }

        [Test]
        public void LoopMenuStaticEndingLocksInputAndSetsStaticState()
        {
            GameObject host = new GameObject("LoopMenuHost");
            try
            {
                PlayerControlGate gate = host.AddComponent<PlayerControlGate>();
                GameFlowController flow = host.AddComponent<GameFlowController>();
                LoopMenuController menu = host.AddComponent<LoopMenuController>();
                AssignObject(menu, "playerControlGate", gate);
                AssignObject(menu, "gameFlowController", flow);

                menu.ChooseStaticEnding();

                Assert.IsTrue(menu.StaticEndingChosen);
                Assert.IsTrue(gate.IsLocked);
                Assert.AreEqual(GameFlowState.StaticEnding, flow.CurrentState);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        private static GameObject CreateInputRepresentation(string name)
        {
            GameObject representation = new GameObject(name);
            representation.AddComponent<PlayerInputReader>();
            return representation;
        }

        private static void AssignObject(Object target, string propertyName, Object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
