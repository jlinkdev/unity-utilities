using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace JLink.Frames.Tests
{
    public sealed class FrameWorkshopTests
    {
        GameObject root;
        FrameWorkshop workshop;
        VisualElement ui;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            root = new GameObject("Frame workshop test");
            workshop = root.AddComponent<FrameWorkshop>();
            yield return null;
            yield return null;
            ui = root.GetComponentInChildren<UIDocument>().rootVisualElement;
        }
        [UnityTearDown]
        public IEnumerator Cleanup() { Object.Destroy(root); yield return null; }

        IEnumerator Pointer(EventType type, Vector2 pos)
        {
            var target = ui.panel.GetCapturingElement(PointerId.mousePointerId) ?? ui.panel.Pick(pos);
            Assert.IsNotNull(target);
            var input = new Event { type = type, mousePosition = pos, button = 0, clickCount = 1 };
            EventBase evt;
            if (type == EventType.MouseDown) evt = PointerDownEvent.GetPooled(input);
            else if (type == EventType.MouseUp) evt = PointerUpEvent.GetPooled(input);
            else evt = PointerMoveEvent.GetPooled(input);
            using (evt) { evt.target = target; target.SendEvent(evt); }
            yield return null; yield return null;
        }
        IEnumerator Click(string label)
        {
            var button = ui.Query<Button>().Where(b => b.text == label).First();
            var scroll = button.GetFirstAncestorOfType<ScrollView>();
            if (scroll != null) scroll.ScrollTo(button);
            yield return null;
            var point = button.worldBound.center;
            yield return Pointer(EventType.MouseDown,point);
            yield return Pointer(EventType.MouseUp,point);
        }
        Vector2 ScreenPoint(Vector3 local)
        {
            var p = root.GetComponentInChildren<Camera>().WorldToScreenPoint(workshop.Frame.transform.TransformPoint(local));
            return new Vector2(p.x,Screen.height-p.y);
        }
        IEnumerator Drag(Vector2 pos, Vector2 delta)
        {
            yield return Pointer(EventType.MouseDown,pos);
            yield return Pointer(EventType.MouseDrag,pos+delta);
            yield return Pointer(EventType.MouseUp,pos+delta);
        }

        [UnityTest]
        public IEnumerator PullingARailAndSculptingItsSurfaceUndoIndependently()
        {
            yield return Click("Front");
            var r = workshop.Frame.recipe;
            float width = r.railWidth;
            yield return Drag(ScreenPoint(FrameHandles.Position(r,FrameHandle.RailWidth)),Vector2.right*15);
            Assert.Greater(r.railWidth,width);
            string resized = JsonUtility.ToJson(r);
            var surface = new Vector3(r.artworkSize.x*.5f+r.Border+r.railWidth*.54f,r.artworkSize.y*.24f,-r.Height(.54f));
            float original = r.Height(.54f);
            yield return Drag(ScreenPoint(surface),Vector2.down*18);
            Assert.Greater(r.Height(.54f),original);
            yield return Click("Undo");
            Assert.AreEqual(resized,JsonUtility.ToJson(workshop.Frame.recipe));
            yield return Click("Undo");
            Assert.AreEqual(width,workshop.Frame.recipe.railWidth);
            yield return Click("Redo");
            Assert.Greater(workshop.Frame.recipe.railWidth,width);
        }

        [UnityTest]
        public IEnumerator LoopGesturesPlaceMoveWidenAndCutWithUndo()
        {
            var f = workshop.Frame;
            var r = f.recipe;
            r.railWidth = .09f;
            r.profile = FrameRecipe.Profile(Moulding.Flat);
            f.Rebuild();
            yield return Click("Front");
            yield return Click("Loops");
            yield return Click("+ Ridge");
            Vector2 Rail(float fraction) => ScreenPoint(new Vector3(r.artworkSize.x*.5f+r.Border+fraction*r.railWidth,
                r.artworkSize.y*.22f,-r.Height(fraction)));
            var place = Rail(.32f);
            yield return Pointer(EventType.MouseDown,place);
            yield return Pointer(EventType.MouseUp,place);
            Assert.AreEqual(1,r.bands.Count);
            var b = r.bands[0];
            float start = b.Center(r.railWidth);
            yield return Drag(Rail(start/r.railWidth),Rail(.52f)-Rail(.32f));
            Assert.That(b.Center(r.railWidth),Is.EqualTo(start+.2f*r.railWidth).Within(.002f));
            float width = b.width;
            yield return Drag(ScreenPoint(FrameBandTool.Handle(r,b,FrameBandTool.Grip.OuterWidth)),Rail(.1f)-Rail(0));
            Assert.That(b.width,Is.EqualTo(width+.2f*r.railWidth).Within(.002f));
            string widened = JsonUtility.ToJson(r);
            yield return Drag(ScreenPoint(FrameBandTool.Handle(r,b,FrameBandTool.Grip.Height)),new Vector2(0,25));
            Assert.Less(b.height,0);
            yield return Click("Undo");
            Assert.AreEqual(widened,JsonUtility.ToJson(f.recipe));
            yield return Click("Redo");
            Assert.Less(f.recipe.bands[0].height,0);
            yield return Click("Remove");
            Assert.IsEmpty(f.recipe.bands);
            yield return Click("+ Groove");
            yield return Pointer(EventType.MouseDown,place);
            yield return Pointer(EventType.MouseUp,place);
            Assert.Less(f.recipe.bands[0].height,0);
        }

        [UnityTest]
        public IEnumerator EditingRepeatsChangesOnlyTheSelectedLoop()
        {
            var f = workshop.Frame;
            f.recipe.railWidth = .09f;
            f.recipe.bands.Add(new FrameBand { position = .25f });
            var outer = new FrameBand { position = .65f };
            f.recipe.bands.Add(outer);
            f.Rebuild();
            yield return Click("Front");
            yield return Click("Loops");
            yield return Click("Beads");
            yield return Click("Next");
            yield return Click("Leaf scroll");
            float size = outer.detailSize;
            yield return Drag(ScreenPoint(FrameBandTool.Handle(f.recipe,outer,FrameBandTool.Grip.DetailSize)),Vector2.right*9);
            Assert.Greater(outer.detailSize,size);
            string enlarged = JsonUtility.ToJson(f.recipe);
            float gap = outer.detailGap;
            yield return Drag(ScreenPoint(FrameBandTool.Handle(f.recipe,outer,FrameBandTool.Grip.DetailGap)),Vector2.right*12);
            Assert.Greater(outer.detailGap,gap);
            var copy = f.recipe.Copy();
            Assert.AreEqual(outer.detailGap,copy.bands[1].detailGap);
            Assert.AreEqual(BandOrnament.LeafScroll,copy.bands[1].ornament);
            yield return Click("Undo");
            Assert.AreEqual(enlarged,JsonUtility.ToJson(f.recipe));
            foreach (var pattern in new[]{("Rope twist",BandOrnament.RopeTwist),("Fluting",BandOrnament.Fluting),("Egg-and-dart",BandOrnament.EggAndDart)})
            {
                yield return Click(pattern.Item1);
                var loop = f.recipe.bands[1];
                Assert.AreEqual(pattern.Item2,loop.ornament);
                float previousSize = loop.detailSize, previousGap = loop.detailGap;
                yield return Drag(ScreenPoint(FrameBandTool.Handle(f.recipe,loop,FrameBandTool.Grip.DetailSize)),Vector2.right*3);
                yield return Drag(ScreenPoint(FrameBandTool.Handle(f.recipe,loop,FrameBandTool.Grip.DetailGap)),Vector2.right*5);
                Assert.Greater(loop.detailSize,previousSize);
                Assert.Greater(loop.detailGap,previousGap);
            }
            yield return Click("Plain");
            Assert.AreEqual(BandOrnament.None,f.recipe.bands[1].ornament);
            Assert.AreEqual(BandOrnament.Beads,f.recipe.bands[0].ornament);
            Assert.AreEqual(size,f.recipe.bands[0].detailSize);
        }

        [UnityTest]
        public IEnumerator DraggingDifferentCornersUpdatesTheSharedSettings()
        {
            var r=workshop.Frame.recipe;
            r.railWidth=.085f;
            workshop.Frame.Rebuild();
            yield return Click("Front");
            yield return Click("Corners");
            yield return Click("Twin scroll");
            Assert.AreEqual(CornerStyle.TwinScroll,r.corners.style);
            float pos=r.corners.position;
            yield return Drag(ScreenPoint(r.corners.Handle(r,0,CornerAdjustment.Position)),new Vector2(4,-4));
            Assert.Greater(r.corners.position,pos);
            yield return Click("Size");
            float size=r.corners.size;
            yield return Drag(ScreenPoint(r.corners.Handle(r,1,CornerAdjustment.Size)),Vector2.up*6);
            Assert.Greater(r.corners.size,size);
            yield return Click("Relief");
            string before=JsonUtility.ToJson(r);
            yield return Drag(ScreenPoint(r.corners.Handle(r,2,CornerAdjustment.Relief)),Vector2.down*6);
            Assert.Greater(r.corners.relief,.005f);
            yield return Click("Undo");
            Assert.AreEqual(before,JsonUtility.ToJson(workshop.Frame.recipe));
            yield return Click("Scallop shell");
            Assert.AreEqual(CornerStyle.ScallopShell,workshop.Frame.recipe.corners.style);
            yield return Click("No ornament");
            Assert.AreEqual(CornerStyle.None,workshop.Frame.recipe.corners.style);
        }

        [UnityTest]
        public IEnumerator EdgeHandlesAndFollowUseTheWorkshopRecipe()
        {
            var f=workshop.Frame; var r=f.recipe;
            var b=new FrameBand {position=.55f,width=.007f,height=.004f};
            r.bands.Add(b); f.Rebuild();
            yield return Click("Front");
            yield return Click("Edges");
            yield return Drag(ScreenPoint(FrameEdgeTool.Handle(r,8)),new Vector2(5,-5));
            Assert.Greater(r.edges.corner,0);
            yield return Drag(ScreenPoint(FrameEdgeTool.Handle(r,5)),Vector2.left*6);
            Assert.Less(r.edges.waist,0);
            string before=JsonUtility.ToJson(r);
            yield return Drag(ScreenPoint(FrameEdgeTool.Handle(r,6)),Vector2.right*8);
            Assert.Greater(r.edges.centre,0);
            yield return Click("Undo");
            Assert.AreEqual(before,JsonUtility.ToJson(f.recipe));
            yield return Click("Redo");
            yield return Click("Loops");
            var follow=ui.Query<Slider>().Where(x=>x.label=="Follow edge (%)").First();
            follow.value=65;
            Assert.That(f.recipe.bands[0].edgeFollow,Is.EqualTo(.65f).Within(.00001f));
        }

        [UnityTest]
        public IEnumerator OvalHandlesEditSeparatePolesAndLinkedSidesWithUndo()
        {
            var f=workshop.Frame; var r=f.recipe;
            r.bands.Add(new FrameBand()); f.Rebuild();
            yield return Click("Front"); yield return Click("Oval"); yield return Click("Edges");
            yield return Drag(ScreenPoint(FrameEdgeTool.Handle(r,2)),Vector2.down*7);
            Assert.Greater(r.edges.ovalTop,0); Assert.AreEqual(0,r.edges.ovalBottom);
            yield return Drag(ScreenPoint(FrameEdgeTool.Handle(r,0)),Vector2.up*6);
            Assert.Greater(r.edges.ovalBottom,0);
            float top=r.edges.ovalTop, bottom=r.edges.ovalBottom;
            yield return Drag(ScreenPoint(FrameEdgeTool.Handle(r,3)),Vector2.left*7);
            Assert.Greater(r.edges.ovalSides,0);
            Assert.AreEqual(-FrameEdgeTool.Handle(r,3).x,FrameEdgeTool.Handle(r,1).x);
            Assert.AreEqual(top,r.edges.ovalTop); Assert.AreEqual(bottom,r.edges.ovalBottom);
            var reach=ui.Query<Slider>().Where(x=>x.label=="Reach (degrees)").First();
            reach.value=28;
            Assert.That(r.edges.ovalSideReach,Is.EqualTo(28).Within(.0001f));
            yield return Click("Undo"); Assert.AreEqual(42,f.recipe.edges.ovalSideReach);
            yield return Click("Loops");
            var follow=ui.Query<Slider>().Where(x=>x.label=="Follow edge (%)").First();
            Assert.AreEqual(DisplayStyle.Flex,follow.resolvedStyle.display);
            follow.value=75; Assert.AreEqual(.75f,f.recipe.bands[0].edgeFollow);
        }

        [UnityTest]
        public IEnumerator ProfilePadAndMountChangeTheVisibleMesh()
        {
            yield return Click("Floater");
            Assert.AreEqual(FrameMount.Floater,workshop.Frame.recipe.mount);
            Assert.AreEqual(0,workshop.Frame.GeneratedMesh.GetIndexCount(1));
            yield return Click("Oval");
            Assert.AreEqual(FrameOutline.Oval,workshop.Frame.recipe.outline);
            yield return Click("Flat");
            var pad = ui.Q("ProfilePad");
            pad.GetFirstAncestorOfType<ScrollView>().ScrollTo(pad);
            yield return null;
            var bounds = pad.worldBound;
            Vector2 point = new Vector2(bounds.x+10,bounds.y+10+(1-.55f)*(bounds.height-20));
            yield return Drag(point,Vector2.down*12);
            Assert.Greater(workshop.Frame.recipe.Height(0),.55f*workshop.Frame.recipe.relief);
        }

        [UnityTest]
        public IEnumerator EaselToggleAndLeanKeepTheWorkshopEditable()
        {
            var frame=workshop.Frame;
            string before=JsonUtility.ToJson(frame.recipe);
            var toggle=ui.Query<Toggle>().Where(t=>t.label=="Easel back").First();
            toggle.value=true; yield return null;
            Assert.AreEqual(4,frame.GeneratedMesh.subMeshCount);
            Assert.IsTrue(frame.GetComponent<MeshRenderer>().sharedMaterials[3]);
            var lean=ui.Query<Slider>().Where(s=>s.label=="Lean angle (degrees)").First();
            lean.value=19; yield return null;
            Assert.AreEqual(19,frame.recipe.leanAngle);
            yield return Click("Back"); yield return Click("Front");
            yield return Click("Edges");
            var cam=root.GetComponentInChildren<Camera>();
            var screen=cam.WorldToScreenPoint(frame.DesignToWorld.MultiplyPoint3x4(FrameEdgeTool.Handle(frame.recipe,6)));
            yield return Drag(new Vector2(screen.x,Screen.height-screen.y),Vector2.right*6);
            Assert.Greater(frame.recipe.edges.centre,0);
            yield return Click("Undo"); yield return Click("Undo"); yield return Click("Undo");
            Assert.AreEqual(before,JsonUtility.ToJson(frame.recipe));
            Assert.AreEqual(3,frame.GetComponent<MeshRenderer>().sharedMaterials.Length);
        }

        [UnityTest]
        public IEnumerator FitIncludesTheFullAsymmetricSilhouette()
        {
            var frame=workshop.Frame;
            var r=frame.recipe;
            r.outline=FrameOutline.Oval; r.artworkSize=Vector2.one*.05f;
            r.railWidth=.014f; r.matWidth=0; r.edges.ovalTop=.36f;
            frame.Rebuild();
            yield return Click("Front"); yield return Click("Fit");
            var camera=root.GetComponentInChildren<Camera>();
            foreach (var point in frame.GeneratedMesh.vertices)
            {
                var uv=camera.WorldToViewportPoint(frame.transform.TransformPoint(point));
                if (uv.x<=0 || uv.x>=1 || uv.y<=0 || uv.y>=1 || uv.z<=0)
                    Assert.Fail($"Fitted vertex outside the viewport at {uv}");
            }
        }

        [UnityTest]
        public IEnumerator UnreadableSavedDesignLeavesCurrentRecipeAndHistoryIntact()
        {
            const string key="com.jlinkdev.procedural-frames.workshop.design.v1";
            bool existed=PlayerPrefs.HasKey(key);
            string original=PlayerPrefs.GetString(key);
            try
            {
                PlayerPrefs.SetString(key,"{invalid json");
                yield return Click("Oval");
                var recipe=workshop.Frame.recipe;
                yield return Click("Recall");
                Assert.AreSame(recipe,workshop.Frame.recipe);
                yield return Click("Undo");
                Assert.AreEqual(FrameOutline.Rectangle,workshop.Frame.recipe.outline);
            }
            finally
            {
                if (existed) PlayerPrefs.SetString(key,original); else PlayerPrefs.DeleteKey(key);
            }
        }

        [UnityTest]
        public IEnumerator ReopeningDisposesThePreviewAndRestoresAnIndependentDesign()
        {
            var design = ScriptableObject.CreateInstance<FrameDesign>();
            try
            {
                var old = workshop.Frame.GeneratedMesh;
                workshop.enabled = false;
                yield return null;
                Assert.IsFalse(old);
                workshop.startingDesign = design;
                workshop.enabled = true;
                yield return null;
                var r = workshop.Frame.recipe;
                r.railWidth *= 2;
                workshop.Frame.Rebuild();
                Assert.AreNotEqual(r.railWidth,design.recipe.railWidth);
                Assert.AreEqual(1,root.GetComponentsInChildren<Camera>().Length);
                foreach (var mat in workshop.Frame.GetComponent<Renderer>().sharedMaterials)
                    Assert.IsTrue(mat && mat.shader && mat.shader.isSupported);
            }
            finally { Object.Destroy(design); }
        }
    }
}
