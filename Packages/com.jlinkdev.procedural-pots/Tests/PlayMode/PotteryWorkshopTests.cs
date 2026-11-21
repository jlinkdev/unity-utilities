using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace JLink.Pots.Tests
{
    public class PotteryWorkshopTests
    {
        GameObject root;

        [UnityTearDown]
        public IEnumerator CleanUp()
        {
            if (root) Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator StartingDesignIsIndependentAndWorkshopCanReopen()
        {
            var design = ScriptableObject.CreateInstance<PotDesign>();
            try
            {
                design.recipe = PotRecipe.Create(PotForm.Urn);
                root = new GameObject("Workshop test");
                root.SetActive(false);
                var workshop = root.AddComponent<PotteryWorkshop>();
                workshop.startingDesign = design;
                root.SetActive(true);
                yield return null;
                var first = workshop.Vessel.GeneratedMesh;
                Assert.IsTrue(first && first.vertexCount > 0);
                Assert.IsNotNull(workshop.Vessel.GetComponent<Renderer>().sharedMaterial);
                workshop.Vessel.recipe.width *= 2;
                workshop.Vessel.Rebuild();
                Assert.AreNotEqual(workshop.Vessel.recipe.width,design.recipe.width);
                workshop.enabled = false;
                yield return null;
                Assert.IsFalse(first);
                workshop.enabled = true;
                yield return null;
                Assert.AreEqual(design.recipe.width,workshop.Vessel.recipe.width);
                Assert.AreEqual(1,root.GetComponentsInChildren<Camera>().Length);
            }
            finally { Object.Destroy(design); }
        }

        IEnumerator Pointer(EventType type, Vector2 position, Vector2 delta = default)
        {
            var panel = root.GetComponentInChildren<UIDocument>().rootVisualElement.panel;
            var target = panel.GetCapturingElement(PointerId.mousePointerId) ?? panel.Pick(position);
            Assert.IsNotNull(target);
            var input = new Event { type = type, button = 0, mousePosition = position, delta = delta, clickCount = 1 };
            EventBase evt;
            if (type == EventType.MouseDown) evt = PointerDownEvent.GetPooled(input);
            else if (type == EventType.MouseUp) evt = PointerUpEvent.GetPooled(input);
            else evt = PointerMoveEvent.GetPooled(input);
            using (evt) { evt.target = target; target.SendEvent(evt); }
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator FormShelfSculptAndUndoThroughGameView()
        {
            root = new GameObject("Workshop gesture test");
            var workshop = root.AddComponent<PotteryWorkshop>();
            yield return null;
            yield return null;
            var ui = root.GetComponentInChildren<UIDocument>().rootVisualElement;
            Vector2 urn = ui.Q<Button>("PresetUrn").worldBound.center;
            yield return Pointer(EventType.MouseDown,urn);
            yield return Pointer(EventType.MouseUp,urn);
            Assert.AreEqual("Urn",workshop.Vessel.recipe.title,"Preset card");

            var pot = workshop.Vessel;
            var camera = root.GetComponentInChildren<Camera>();
            var screen = camera.WorldToScreenPoint(pot.transform.TransformPoint(Vector3.up*pot.recipe.height));
            Vector2 grip = new Vector2(screen.x,Screen.height-screen.y-24);
            float initial = pot.recipe.height;
            yield return Pointer(EventType.MouseDown,grip);
            yield return Pointer(EventType.MouseDrag,grip+Vector2.down*24,Vector2.down*24);
            yield return Pointer(EventType.MouseUp,grip+Vector2.down*24);
            Assert.Greater(pot.recipe.height,initial,"Direct height grip");

            var undo = ui.Query<Button>().Where(b => b.text == "Undo").First().worldBound.center;
            yield return Pointer(EventType.MouseDown,undo);
            yield return Pointer(EventType.MouseUp,undo);
            Assert.AreEqual(initial,pot.recipe.height,"One undo for the drag");
            Assert.AreEqual("Urn",pot.recipe.title);
            var slider = ui.Q<Slider>("Height (mm)");
            slider.GetFirstAncestorOfType<ScrollView>().ScrollTo(slider);
            yield return null;
            Vector2 knob = slider.Q(className: "unity-base-slider__dragger").worldBound.center;
            yield return Pointer(EventType.MouseDown,knob);
            yield return Pointer(EventType.MouseDrag,knob+Vector2.right*15,Vector2.right*15);
            yield return Pointer(EventType.MouseDrag,knob+Vector2.right*30,Vector2.right*15);
            yield return Pointer(EventType.MouseUp,knob+Vector2.right*30);
            Assert.Greater(pot.recipe.height,initial,"Inspector slider");
            yield return Pointer(EventType.MouseDown,undo);
            yield return Pointer(EventType.MouseUp,undo);
            Assert.AreEqual(initial,pot.recipe.height,"One undo for the complete slider drag");
        }

        [UnityTest]
        public IEnumerator WheelDragShapesBothSidesOfThePickedBand()
        {
            root = new GameObject("Wheel sculpt test");
            var workshop = root.AddComponent<PotteryWorkshop>();
            yield return null;
            yield return null;
            var ui = root.GetComponentInChildren<UIDocument>().rootVisualElement;
            IEnumerator Click(string name)
            {
                Vector2 point = ui.Query<Button>().Where(b => b.text == name).First().worldBound.center;
                yield return Pointer(EventType.MouseDown,point);
                yield return Pointer(EventType.MouseUp,point);
            }
            yield return Click("Side");
            var pot = workshop.Vessel;
            var camera = root.GetComponentInChildren<Camera>();
            const float level = .60f;
            var original = pot.recipe.Copy();
            var front = PotMesh.Surface(original,level,-Mathf.PI/2);
            var pixel = camera.WorldToScreenPoint(pot.transform.TransformPoint(front));
            var wall = new Vector2(pixel.x,Screen.height-pixel.y);
            yield return Pointer(EventType.MouseDown,wall);
            yield return Pointer(EventType.MouseDrag,wall+Vector2.right*22,Vector2.right*22);
            for (int i = 0; i < 8; i++)
            {
                float angle = i*Mathf.PI/4;
                Assert.Greater(PotMesh.Surface(pot.recipe,level,angle).sqrMagnitude,
                    PotMesh.Surface(original,level,angle).sqrMagnitude);
            }
            Assert.AreEqual(original.RadiusAt(.2f),pot.recipe.RadiusAt(.2f));
            Assert.Greater(pot.recipe.RadiusAt(level)-original.RadiusAt(level),
                pot.recipe.RadiusAt(.71f)-original.RadiusAt(.71f));
            yield return Pointer(EventType.MouseDrag,wall+Vector2.left*15,Vector2.left*37);
            yield return Pointer(EventType.MouseUp,wall+Vector2.left*15);
            float pinched = pot.recipe.RadiusAt(level);
            Assert.Less(pinched,original.RadiusAt(level));
            Assert.AreEqual(pinched,pot.recipe.Copy().RadiusAt(level));
            yield return Click("Undo");
            Assert.AreEqual(JsonUtility.ToJson(original),JsonUtility.ToJson(pot.recipe));
            yield return Click("Redo");
            Assert.AreEqual(pinched,pot.recipe.RadiusAt(level));
        }

        [UnityTest]
        public IEnumerator CheckerRestoresMaterialAndRefitUndoes()
        {
            root = new GameObject("Workshop material test");
            root.SetActive(false);
            var workshop = root.AddComponent<PotteryWorkshop>();
            var finish = new Material(Resources.Load<Material>("JLinkPots/Clay"));
            try
            {
                workshop.surfaceMaterial = finish;
                root.SetActive(true);
                yield return null;
                yield return null;
                var pot = workshop.Vessel;
                var renderer = pot.GetComponent<Renderer>();
                Assert.AreSame(finish,renderer.sharedMaterial);
                var before = pot.GeneratedMesh.uv;
                pot.recipe.width *= 1.8f;
                pot.Rebuild();
                var ui = root.GetComponentInChildren<UIDocument>().rootVisualElement;
                IEnumerator Click(VisualElement element)
                {
                    Vector2 point = element.worldBound.center;
                    yield return Pointer(EventType.MouseDown,point);
                    yield return Pointer(EventType.MouseUp,point);
                }
                Button ButtonNamed(string text) => ui.Query<Button>().Where(b => b.text == text).First();
                yield return Click(ButtonNamed("UVs"));
                var checker = ui.Q<Toggle>();
                checker.value = true;
                Assert.AreNotSame(finish,renderer.sharedMaterial);
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                Assert.AreEqual(1,block.GetFloat("_ShowChecker"));
                Assert.IsNotNull(block.GetTexture("_CheckerTex"));
                checker.value = false;
                Assert.AreSame(finish,renderer.sharedMaterial);
                var refit = ButtonNamed("Refit to current shape");
                refit.GetFirstAncestorOfType<ScrollView>().ScrollTo(refit);
                yield return null;
                yield return Click(refit);
                Assert.Greater(pot.GeneratedMesh.uv[pot.recipe.segments].x,before[pot.recipe.segments].x);
                yield return Click(ButtonNamed("Undo"));
                CollectionAssert.AreEqual(before,pot.GeneratedMesh.uv);
            }
            finally { Object.Destroy(finish); }
        }

        [UnityTest]
        public IEnumerator ThumbAndLoopAttachAtPickedWall()
        {
            root = new GameObject("Workshop wall test");
            var workshop = root.AddComponent<PotteryWorkshop>();
            yield return null;
            yield return null;
            var ui = root.GetComponentInChildren<UIDocument>().rootVisualElement;
            IEnumerator Click(string caption)
            {
                var point = ui.Query<Button>().Where(b => b.text == caption).First().worldBound.center;
                yield return Pointer(EventType.MouseDown,point);
                yield return Pointer(EventType.MouseUp,point);
            }
            yield return Click("Side");
            yield return Click("Clay");
            var pot = workshop.Vessel;
            var camera = root.GetComponentInChildren<Camera>();
            Vector3 front = PotMesh.Surface(pot.recipe,.55f,-Mathf.PI/2);
            var pixel = camera.WorldToScreenPoint(pot.transform.TransformPoint(front));
            var wall = new Vector2(pixel.x,Screen.height-pixel.y);
            yield return Pointer(EventType.MouseDown,wall);
            yield return Pointer(EventType.MouseDrag,wall+Vector2.left*18,Vector2.left*18);
            yield return Pointer(EventType.MouseUp,wall+Vector2.left*18);
            Assert.AreEqual(1,pot.recipe.marks.Count);
            Assert.Less(pot.recipe.marks[0].depth,0);
            yield return Click("Handles");
            yield return Click("Place on wall");
            yield return Pointer(EventType.MouseDown,wall);
            yield return Pointer(EventType.MouseUp,wall);
            Assert.AreEqual(1,pot.recipe.handles);
            Assert.That(pot.recipe.handleAngle,Is.EqualTo(270).Within(2));
            Assert.That(pot.recipe.handleLevel,Is.EqualTo(.55f).Within(.02f));
        }
    }
}
