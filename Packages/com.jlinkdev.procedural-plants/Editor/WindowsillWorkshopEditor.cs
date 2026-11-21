using JLink.Plants.Studio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JLink.Plants.Editor
{
    [CustomEditor(typeof(WindowsillWorkshop))]
    sealed class WindowsillWorkshopEditor : UnityEditor.Editor
    {
        void OnEnable() => Undo.undoRedoPerformed+=Resync;
        void OnDisable() => Undo.undoRedoPerformed-=Resync;
        void Resync()
        {
            if(target is WindowsillWorkshop bench && bench.Ready)
            {
                bench.Mother.Refresh(); bench.Nursery.Refresh(); bench.Sync();
            }
            Repaint();
        }
        public override void OnInspectorGUI()
        {
            var bench=(WindowsillWorkshop)target;
            if(!bench.Ready) { DrawDefaultInspector(); return; }
            bool editing=!Application.IsPlaying(bench.gameObject);
            var type=Event.current.type;
            if(editing && (type==EventType.MouseDown || type==EventType.MouseUp || type==EventType.MouseDrag || type==EventType.KeyDown))
                Undo.RecordObjects(new Object[]{bench,bench.Mother,bench.Nursery,bench.Mother.transform.parent,
                    bench.Nursery.gameObject,bench.CameraTransform},"Edit windowsill");
            EditorGUI.BeginChangeCheck();
            if(bench.CurrentView!=WindowsillWorkshop.View.Tray && GUILayout.Button(bench.PotCloseUp?"Both pots":"Selected pot"))
                bench.SetPotCloseUp(!bench.PotCloseUp);
            bench.DrawControls();
            if((EditorGUI.EndChangeCheck() || type==EventType.MouseUp || type==EventType.KeyUp) && editing)
            {
                EditorUtility.SetDirty(bench); EditorUtility.SetDirty(bench.Mother);EditorUtility.SetDirty(bench.Nursery);
                EditorSceneManager.MarkSceneDirty(bench.gameObject.scene);
                SceneView.RepaintAll();
            }
        }
    }
}
