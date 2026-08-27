using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace jlinkdev.UnityUtilities.IK.Tests
{
    public sealed class TwoBoneIKTests
    {
        private readonly List<GameObject> _createdObjects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null)
                {
                    Object.DestroyImmediate(_createdObjects[i]);
                }
            }

            _createdObjects.Clear();
        }

        [Test]
        public void ZeroWeights_PreserveTheAnimatedPoseExactly()
        {
            Rig rig = CreateRig("Arm", Vector3.zero);
            rig.Root.localRotation = Quaternion.Euler(13f, -17f, 21f);
            rig.Mid.localRotation = Quaternion.Euler(-28f, 9f, 14f);
            rig.Tip.localRotation = Quaternion.Euler(4f, 31f, -8f);
            Quaternion rootRotation = rig.Root.localRotation;
            Quaternion midRotation = rig.Mid.localRotation;
            Quaternion tipRotation = rig.Tip.localRotation;

            rig.Target.position = new Vector3(0.5f, 1.25f, 0.75f);
            rig.Target.rotation = Quaternion.Euler(70f, 20f, -35f);
            rig.Solver.PositionWeight = 0f;
            rig.Solver.RotationWeight = 0f;

            rig.Solver.Solve();

            AssertQuaternionEqual(rootRotation, rig.Root.localRotation);
            AssertQuaternionEqual(midRotation, rig.Mid.localRotation);
            AssertQuaternionEqual(tipRotation, rig.Tip.localRotation);
        }

        [Test]
        public void PositionAndRotationWeights_AreIndependent()
        {
            Rig rig = CreateRig("Arm", Vector3.zero);
            Quaternion rootRotation = rig.Root.localRotation;
            Quaternion midRotation = rig.Mid.localRotation;
            Quaternion targetRotation = Quaternion.Euler(25f, 70f, -15f);
            rig.Target.position = new Vector3(1f, 1f, 0f);
            rig.Target.rotation = targetRotation;
            rig.Solver.PositionWeight = 0f;
            rig.Solver.RotationWeight = 1f;

            rig.Solver.Solve();

            AssertQuaternionEqual(rootRotation, rig.Root.localRotation);
            AssertQuaternionEqual(midRotation, rig.Mid.localRotation);
            AssertQuaternionEqual(targetRotation, rig.Tip.rotation);

            rig.Root.localRotation = Quaternion.identity;
            rig.Mid.localRotation = Quaternion.identity;
            rig.Tip.localRotation = Quaternion.Euler(3f, 7f, 11f);
            Quaternion animatedTipLocalRotation = rig.Tip.localRotation;
            rig.Solver.PositionWeight = 1f;
            rig.Solver.RotationWeight = 0f;
            rig.Solver.SoftReach = 0f;

            rig.Solver.Solve();

            Assert.That(Vector3.Distance(rig.Tip.position, rig.Target.position), Is.LessThan(0.001f));
            AssertQuaternionEqual(animatedTipLocalRotation, rig.Tip.localRotation);
        }

        [Test]
        public void RuntimePositionWeight_BlendsFromFreshAnimatedPose()
        {
            Rig rig = CreateRig("Arm", Vector3.zero);
            rig.Target.position = new Vector3(1f, 1f, 0f);
            rig.Solver.RotationWeight = 0f;
            rig.Solver.SoftReach = 0f;
            Quaternion animatedRootRotation = rig.Root.localRotation;
            Quaternion animatedMidRotation = rig.Mid.localRotation;

            rig.Solver.PositionWeight = 1f;
            rig.Solver.Solve();
            Quaternion solvedRootRotation = rig.Root.localRotation;
            float fullRootAngle = Quaternion.Angle(animatedRootRotation, solvedRootRotation);

            rig.Root.localRotation = animatedRootRotation;
            rig.Mid.localRotation = animatedMidRotation;
            rig.Solver.PositionWeight = 0.5f;
            rig.Solver.Solve();

            float blendedRootAngle = Quaternion.Angle(animatedRootRotation, rig.Root.localRotation);
            Assert.That(blendedRootAngle, Is.EqualTo(fullRootAngle * 0.5f).Within(0.001f));

            rig.Root.localRotation = animatedRootRotation;
            rig.Mid.localRotation = animatedMidRotation;
            rig.Solver.PositionWeight = 0f;
            rig.Solver.Solve();
            AssertQuaternionEqual(animatedRootRotation, rig.Root.localRotation);
            AssertQuaternionEqual(animatedMidRotation, rig.Mid.localRotation);
        }

        [Test]
        public void PoleTarget_ControlsElbowSide()
        {
            Rig rig = CreateRig("Arm", Vector3.zero);
            rig.Target.position = new Vector3(1.25f, 0f, 0f);
            rig.Pole.position = new Vector3(0f, 0f, 2f);
            rig.Solver.PositionWeight = 1f;
            rig.Solver.RotationWeight = 0f;
            rig.Solver.PoleWeight = 1f;
            rig.Solver.SoftReach = 0f;

            rig.Solver.Solve();

            Assert.That(rig.Mid.position.z, Is.GreaterThan(0.1f));
            Assert.That(Vector3.Distance(rig.Tip.position, rig.Target.position), Is.LessThan(0.001f));
        }

        [Test]
        public void TargetBeyondMaximumReach_ClampsWithoutInvalidRotations()
        {
            Rig rig = CreateRig("Arm", Vector3.zero);
            rig.Target.position = new Vector3(10f, 0f, 0f);
            rig.Pole.position = new Vector3(0f, 2f, 0f);
            rig.Solver.PositionWeight = 1f;
            rig.Solver.RotationWeight = 0f;
            rig.Solver.SoftReach = 0f;

            rig.Solver.Solve();

            Assert.That(rig.Solver.IsTargetReachable, Is.False);
            Assert.That(Vector3.Distance(rig.Root.position, rig.Tip.position), Is.LessThanOrEqualTo(2.001f));
            Assert.That(float.IsInfinity(rig.Solver.ReachError), Is.False);
            AssertFinite(rig.Root.rotation);
            AssertFinite(rig.Mid.rotation);
            AssertFinite(rig.Tip.rotation);
        }

        [Test]
        public void TargetInsideMinimumReach_ClampsWithoutNaNs()
        {
            Rig rig = CreateRig("Arm", Vector3.zero, 2f, 1f);
            rig.Target.position = rig.Root.position;
            rig.Pole.position = rig.Root.position + Vector3.forward;
            rig.Solver.PositionWeight = 1f;
            rig.Solver.RotationWeight = 0f;
            rig.Solver.SoftReach = 0f;

            rig.Solver.Solve();

            Assert.That(rig.Solver.IsTargetReachable, Is.False);
            Assert.That(Vector3.Distance(rig.Root.position, rig.Tip.position), Is.EqualTo(1f).Within(0.002f));
            AssertFinite(rig.Root.rotation);
            AssertFinite(rig.Mid.rotation);
            AssertFinite(rig.Tip.rotation);
        }

        [Test]
        public void MovingTargetSweep_RemainsFiniteAndKeepsPoleSide()
        {
            Rig rig = CreateRig("Arm", Vector3.zero);
            rig.Pole.position = new Vector3(0f, 0f, 2f);
            rig.Solver.PositionWeight = 1f;
            rig.Solver.RotationWeight = 0f;
            rig.Solver.PoleWeight = 1f;
            rig.Solver.SoftReach = 0.05f;

            for (int step = 0; step <= 60; step++)
            {
                rig.Root.localRotation = Quaternion.identity;
                rig.Mid.localRotation = Quaternion.identity;
                rig.Tip.localRotation = Quaternion.identity;
                float x = Mathf.Lerp(-1.75f, 1.75f, step / 60f);
                rig.Target.position = new Vector3(x, 0.35f, 0f);

                rig.Solver.Solve();

                AssertFinite(rig.Root.rotation);
                AssertFinite(rig.Mid.rotation);
                AssertFinite(rig.Tip.rotation);
                Assert.That(rig.Mid.position.z, Is.GreaterThan(0f));
                Assert.That(float.IsInfinity(rig.Solver.ReachError), Is.False);
            }
        }

        [Test]
        public void RotatedUniformlyScaledParent_StillSolvesWorldSpaceTarget()
        {
            Rig rig = CreateRig("Arm", new Vector3(4f, -2f, 7f));
            rig.Container.rotation = Quaternion.Euler(20f, 65f, -12f);
            rig.Container.localScale = Vector3.one * 1.75f;
            rig.Target.position = rig.Container.TransformPoint(new Vector3(1f, 1f, 0.25f));
            rig.Pole.position = rig.Container.TransformPoint(new Vector3(0f, 0f, 2f));
            rig.Solver.PositionWeight = 1f;
            rig.Solver.RotationWeight = 0f;
            rig.Solver.SoftReach = 0f;

            rig.Solver.Solve();

            Assert.That(rig.Solver.HasValidChain, Is.True);
            Assert.That(Vector3.Distance(rig.Tip.position, rig.Target.position), Is.LessThan(0.002f));
            AssertFinite(rig.Root.rotation);
            AssertFinite(rig.Mid.rotation);
        }

        [Test]
        public void SeparateArmSolvers_DoNotModifyEachOthersBones()
        {
            Rig left = CreateRig("LeftArm", new Vector3(-3f, 0f, 0f));
            Rig right = CreateRig("RightArm", new Vector3(3f, 0f, 0f));
            left.Target.position = new Vector3(-2f, 1f, 0.5f);
            right.Target.position = new Vector3(4f, -0.5f, -0.5f);
            left.Solver.SoftReach = 0f;
            right.Solver.SoftReach = 0f;
            left.Solver.RotationWeight = 0f;
            right.Solver.RotationWeight = 0f;
            Quaternion rightRootRotation = right.Root.rotation;
            Quaternion rightMidRotation = right.Mid.rotation;

            left.Solver.Solve();

            AssertQuaternionEqual(rightRootRotation, right.Root.rotation);
            AssertQuaternionEqual(rightMidRotation, right.Mid.rotation);

            right.Solver.Solve();

            Assert.That(Vector3.Distance(left.Tip.position, left.Target.position), Is.LessThan(0.002f));
            Assert.That(Vector3.Distance(right.Tip.position, right.Target.position), Is.LessThan(0.002f));
        }

        [Test]
        public void LegacyWeightSetter_UpdatesPositionAndRotationWeights()
        {
            Rig rig = CreateRig("Arm", Vector3.zero);

            rig.Solver.Weight = 0.35f;

            Assert.That(rig.Solver.PositionWeight, Is.EqualTo(0.35f));
            Assert.That(rig.Solver.RotationWeight, Is.EqualTo(0.35f));
        }

        [Test]
        public void InvalidZeroLengthChain_FailsSafely()
        {
            Rig rig = CreateRig("Arm", Vector3.zero);
            rig.Mid.localPosition = Vector3.zero;

            Assert.DoesNotThrow(rig.Solver.Solve);
            Assert.That(rig.Solver.HasValidChain, Is.False);
            Assert.That(rig.Solver.IsTargetReachable, Is.False);
            Assert.That(float.IsPositiveInfinity(rig.Solver.ReachError), Is.True);
        }

        private Rig CreateRig(
            string name,
            Vector3 worldPosition,
            float upperLength = 1f,
            float lowerLength = 1f)
        {
            GameObject containerObject = new GameObject(name);
            _createdObjects.Add(containerObject);
            Transform container = containerObject.transform;
            container.position = worldPosition;

            Transform root = CreateChild(container, "UpperArm", Vector3.zero);
            Transform mid = CreateChild(root, "Forearm", Vector3.right * upperLength);
            Transform tip = CreateChild(mid, "Hand", Vector3.right * lowerLength);

            GameObject targetObject = new GameObject(name + " Target");
            _createdObjects.Add(targetObject);
            GameObject poleObject = new GameObject(name + " Pole");
            _createdObjects.Add(poleObject);
            targetObject.transform.position = worldPosition + new Vector3(1f, 1f, 0f);
            poleObject.transform.position = worldPosition + Vector3.forward * 2f;

            TwoBoneIK solver = containerObject.AddComponent<TwoBoneIK>();
            solver.Root = root;
            solver.Mid = mid;
            solver.Tip = tip;
            solver.Target = targetObject.transform;
            solver.Pole = poleObject.transform;
            solver.SolveInLateUpdate = false;

            return new Rig(container, root, mid, tip, targetObject.transform, poleObject.transform, solver);
        }

        private static Transform CreateChild(Transform parent, string name, Vector3 localPosition)
        {
            GameObject childObject = new GameObject(name);
            Transform child = childObject.transform;
            child.SetParent(parent, false);
            child.localPosition = localPosition;
            child.localRotation = Quaternion.identity;
            child.localScale = Vector3.one;
            return child;
        }

        private static void AssertQuaternionEqual(Quaternion expected, Quaternion actual)
        {
            Assert.That(Quaternion.Angle(expected, actual), Is.LessThan(0.0001f));
        }

        private static void AssertFinite(Quaternion value)
        {
            Assert.That(float.IsNaN(value.x) || float.IsInfinity(value.x), Is.False);
            Assert.That(float.IsNaN(value.y) || float.IsInfinity(value.y), Is.False);
            Assert.That(float.IsNaN(value.z) || float.IsInfinity(value.z), Is.False);
            Assert.That(float.IsNaN(value.w) || float.IsInfinity(value.w), Is.False);
        }

        private readonly struct Rig
        {
            public Rig(
                Transform container,
                Transform root,
                Transform mid,
                Transform tip,
                Transform target,
                Transform pole,
                TwoBoneIK solver)
            {
                Container = container;
                Root = root;
                Mid = mid;
                Tip = tip;
                Target = target;
                Pole = pole;
                Solver = solver;
            }

            public Transform Container { get; }
            public Transform Root { get; }
            public Transform Mid { get; }
            public Transform Tip { get; }
            public Transform Target { get; }
            public Transform Pole { get; }
            public TwoBoneIK Solver { get; }
        }
    }
}
