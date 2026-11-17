#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using jlinkdev.UnityUtilities.InteractiveFog.Editor;

namespace jlinkdev.UnityUtilities.InteractiveFog.Tests
{
    public sealed class FogSetupTests
    {
        string folder;
        GameObject root;
        [SetUp] public void Setup()
        {
            folder="Assets/FogSetupTest_"+System.Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets",System.IO.Path.GetFileName(folder));
        }
        [TearDown] public void Cleanup()
        { if(root) Object.DestroyImmediate(root);AssetDatabase.DeleteAsset(folder); }
        [TestCase(FogDensityShape.GroundMist)] [TestCase(FogDensityShape.Cumulus)] [TestCase(FogDensityShape.DryIce)]
        public void CreateStyleVolumeAssignsMatchingPresetAndCompute(FogDensityShape shape)
        {
            var volume=FogSetup.CreateVolume(shape);root=volume.gameObject;
            Assert.That(volume.style,Is.SameAs(FogSetup.Preset(shape)));
            Assert.That(volume.ResolvedStyle.densityShape,Is.EqualTo(shape));
            Assert.That(volume.simulation,Is.Not.Null);
            Assert.That(Selection.activeGameObject,Is.SameAs(root));
        }
        [Test] public void IndependentCopyPreservesTuningDoesNotOverwriteAndAssignmentCanUndo()
        {
            root=new GameObject("Copy test");var volume=root.AddComponent<FogVolume>();volume.style=FogSetup.Preset(FogDensityShape.DryIce);
            var source=volume.style;string before=JsonUtility.ToJson(source.settings);
            var copy=FogSetup.SaveStyleCopy(volume,folder+"/Look.asset");
            Assert.That(copy,Is.Not.SameAs(source));Assert.That(JsonUtility.ToJson(copy.settings),Is.EqualTo(before));
            Undo.FlushUndoRecordObjects();Undo.PerformUndo();Assert.That(volume.style,Is.SameAs(source));
            var second=FogSetup.SaveStyleCopy(volume,folder+"/Look.asset");
            Assert.That(AssetDatabase.GetAssetPath(second),Is.Not.EqualTo(AssetDatabase.GetAssetPath(copy)));
            second.settings.extinction=1;Assert.That(JsonUtility.ToJson(source.settings),Is.EqualTo(before));
            Assert.That(JsonUtility.ToJson(copy.settings),Is.EqualTo(before));
        }
        [Test] public void ConfigureRendererIsIdempotentAndStoresShaderAndFeatureMap()
        {
            var renderer=ScriptableObject.CreateInstance<UniversalRendererData>();AssetDatabase.CreateAsset(renderer,folder+"/Renderer.asset");
            var first=FogSetup.ConfigureRenderer(renderer);first.SetActive(false);
            var second=FogSetup.ConfigureRenderer(renderer);
            Assert.That(second,Is.SameAs(first));Assert.That(renderer.rendererFeatures.Count,Is.EqualTo(1));Assert.That(second.isActive,Is.True);
            using(var data=new SerializedObject(second)) Assert.That(data.FindProperty("shader").objectReferenceValue,Is.Not.Null);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(first,out string guid,out long id);
            using(var data=new SerializedObject(renderer))
            { var map=data.FindProperty("m_RendererFeatureMap");Assert.That(map.arraySize,Is.EqualTo(1));Assert.That(map.GetArrayElementAtIndex(0).longValue,Is.EqualTo(id)); }
        }
        [TestCase(false)] [TestCase(true)]
        public void CameraOverridePreservesDefaultAndGlobalPipelinesAndReusesRegistration(bool alreadyRegistered)
        {
            var originalGraphics=UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;
            var originalQuality=QualitySettings.renderPipeline;
            var original=ScriptableObject.CreateInstance<UniversalRendererData>();AssetDatabase.CreateAsset(original,folder+"/Original.asset");
            var fog=ScriptableObject.CreateInstance<UniversalRendererData>();AssetDatabase.CreateAsset(fog,folder+"/Fog.asset");
            var pipeline=UniversalRenderPipelineAsset.Create(original);AssetDatabase.CreateAsset(pipeline,folder+"/Pipeline.asset");
            if(alreadyRegistered)
                using(var data=new SerializedObject(pipeline))
                { var list=data.FindProperty("m_RendererDataList");list.arraySize=2;list.GetArrayElementAtIndex(1).objectReferenceValue=fog;data.FindProperty("m_DefaultRendererIndex").intValue=1;data.ApplyModifiedPropertiesWithoutUndo(); }
            root=new GameObject("Camera test");var camera=root.AddComponent<Camera>();
            Assert.That(FogSetup.AssignRendererToCamera(pipeline,fog,camera),Is.EqualTo(1));
            Assert.That(FogSetup.AssignRendererToCamera(pipeline,fog,camera),Is.EqualTo(1));
            using(var data=new SerializedObject(pipeline))
            {
                var list=data.FindProperty("m_RendererDataList");Assert.That(list.arraySize,Is.EqualTo(2));
                Assert.That(list.GetArrayElementAtIndex(0).objectReferenceValue,Is.SameAs(original));
                Assert.That(list.GetArrayElementAtIndex(1).objectReferenceValue,Is.SameAs(fog));
                Assert.That(data.FindProperty("m_DefaultRendererIndex").intValue,Is.EqualTo(alreadyRegistered?1:0));
            }
            using(var data=new SerializedObject(camera.GetComponent<UniversalAdditionalCameraData>())) Assert.That(data.FindProperty("m_RendererIndex").intValue,Is.EqualTo(1));
            Assert.That(UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline,Is.SameAs(originalGraphics));
            Assert.That(QualitySettings.renderPipeline,Is.SameAs(originalQuality));
        }
        [Test] public void CameraRendererAssignmentAndRegistrationUndoTogether()
        {
            var original=ScriptableObject.CreateInstance<UniversalRendererData>();AssetDatabase.CreateAsset(original,folder+"/Original.asset");
            var fog=ScriptableObject.CreateInstance<UniversalRendererData>();AssetDatabase.CreateAsset(fog,folder+"/Fog.asset");
            var pipeline=UniversalRenderPipelineAsset.Create(original);AssetDatabase.CreateAsset(pipeline,folder+"/Pipeline.asset");
            root=new GameObject("Undo camera");var camera=root.AddComponent<Camera>();var cameraData=root.AddComponent<UniversalAdditionalCameraData>();cameraData.SetRenderer(-1);
            FogSetup.AssignRendererToCamera(pipeline,fog,camera);Undo.FlushUndoRecordObjects();Undo.PerformUndo();
            using(var data=new SerializedObject(pipeline)) Assert.That(data.FindProperty("m_RendererDataList").arraySize,Is.EqualTo(1));
            using(var data=new SerializedObject(cameraData)) Assert.That(data.FindProperty("m_RendererIndex").intValue,Is.EqualTo(-1));
        }
    }
}
#endif
