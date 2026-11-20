using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace JLinkDev.ProceduralNebulae.Tests
{
    public class NebulaStyleTests
    {
        const string Presets = "Packages/com.jlinkdev.procedural-nebulae/Runtime/Presets/";

        [TestCase(0,NebulaStructureWarp.Switchback)]
        [TestCase(1,NebulaStructureWarp.OverturningBillows)]
        [TestCase(7,NebulaStructureWarp.CarapacePlates)]
        [TestCase(8,NebulaStructureWarp.BranchingGills)]
        [TestCase(9,NebulaStructureWarp.PoreCups)]
        [TestCase(2,NebulaStructureWarp.Switchback)]
        [TestCase(6,NebulaStructureWarp.Switchback)]
        [TestCase(11,NebulaStructureWarp.Switchback)]
        public void SavedFormationIdsResolveOnLoad(int id,NebulaStructureWarp expected)
        {
            var profile = ScriptableObject.CreateInstance<NebulaStyleProfile>();
            var path = AssetDatabase.GenerateUniqueAssetPath("Assets/NebulaFormationLoadTest.asset");
            try
            {
                profile.settings.structureWarp = NebulaStructureWarp.Switchback;
                profile.settings.structureWarpStrength = -.75f;
                profile.settings.bankFormation = (NebulaBankFormation)id;
                AssetDatabase.CreateAsset(profile,path);
                AssetDatabase.SaveAssetIfDirty(profile);
                Resources.UnloadAsset(profile);
                var restored = AssetDatabase.LoadAssetAtPath<NebulaStyleProfile>(path);
                Assert.AreEqual(expected,restored.settings.structureWarp);
                Assert.AreEqual(NebulaBankFormation.Original,restored.settings.bankFormation);
                if (expected == NebulaStructureWarp.Switchback)
                    Assert.AreEqual(-.75f,restored.settings.structureWarpStrength);
                else
                {
                    Assert.AreEqual(NebulaBaseWarp.Switchback,restored.settings.baseWarp);
                    Assert.AreEqual(-.75f,restored.settings.baseWarpStrength);
                    Assert.AreEqual(1,restored.settings.structureWarpStrength);
                }
            }
            finally { AssetDatabase.DeleteAsset(path); }
        }

        [Test]
        public void ShippedPresetsHaveIndependentStylesAndAuthoredLights()
        {
            var painter = AssetDatabase.LoadAssetAtPath<GameObject>(Presets+"Starlit Banks.prefab").GetComponent<NebulaVolume>();
            var plume = AssetDatabase.LoadAssetAtPath<GameObject>(Presets+"Single Star Plumes.prefab").GetComponent<NebulaVolume>();
            Assert.AreEqual(NebulaShape.PainterlyCavity,painter.ResolvedStyle.shape);
            Assert.AreEqual(91,painter.FormSeed);
            Assert.AreEqual(1049,painter.DetailSeed);
            Assert.IsFalse(painter.varyStarPositions);
            Assert.AreEqual(NebulaShape.Plumes,plume.ResolvedStyle.shape);
            Assert.IsTrue(plume.star.gameObject.activeSelf);
            Assert.IsFalse(plume.companion.gameObject.activeSelf);
            Assert.AreEqual(new Vector3(13,42,4),plume.star.transform.localPosition);

            var bank = AssetDatabase.LoadAssetAtPath<GameObject>(Presets+"Backlit Bank.prefab").GetComponent<NebulaVolume>();
            Assert.AreEqual(NebulaShape.BacklitBank,bank.ResolvedStyle.shape);
            Assert.AreEqual(Color.white,bank.star.color);
            Assert.IsFalse(bank.companion.gameObject.activeSelf);
            Assert.IsFalse(bank.varyStarPositions);
        }

        [Test]
        public void LocalSeedsResetToProfileAndStarOffsetsNeverMoveAnchors()
        {
            var go = new GameObject("Variation test");
            go.SetActive(false);
            var volume = go.AddComponent<NebulaVolume>();
            var primary = go.AddComponent<NebulaStar>();
            var profile = ScriptableObject.CreateInstance<NebulaStyleProfile>();
            try
            {
                volume.seed = -7;
                Assert.AreEqual(-7,volume.FormSeed);
                volume.style = profile;
                profile.defaultFormSeed = 22;
                volume.star = primary;
                var anchor = primary.transform.position;
                Assert.AreEqual(22,volume.FormSeed);
                volume.overrideFormSeed = true;
                Assert.AreEqual(-7,volume.FormSeed);
                Assert.AreEqual(22,profile.defaultFormSeed);
                volume.overrideFormSeed = false;
                Assert.AreEqual(22,volume.FormSeed);
                Assert.AreEqual(anchor,volume.GetStarPosition(primary));

                volume.varyStarPositions = true;
                var offset = volume.GetStarPosition(primary);
                Assert.That(Vector3.Distance(anchor,offset),Is.InRange(.001f,volume.starOffsetRadius+.0001f));
                volume.overrideDetailSeed = true;
                volume.detailSeed = -302;
                volume.seed = 102;
                volume.overrideFormSeed = true;
                Assert.AreEqual(offset,volume.GetStarPosition(primary));
                volume.starSeed++;
                Assert.AreNotEqual(offset,volume.GetStarPosition(primary));
                Assert.AreEqual(anchor,primary.transform.position);
                volume.varyStarPositions = false;
                Assert.AreEqual(anchor,volume.GetStarPosition(primary));
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(profile);
            }
        }
    }
}
