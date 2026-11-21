using System;
using System.IO;
using System.Linq;
using JLink.Frames.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JLink.Frames.Tests
{
    public sealed class FrameMeshTests
    {
        Mesh mesh;
        [SetUp] public void Setup() { mesh = new Mesh(); }
        [TearDown] public void Cleanup() { Object.DestroyImmediate(mesh); }

        [Test, Combinatorial]
        public void MountsProduceFiniteOrientedSurfaces([Values] FrameOutline outline,[Values] FrameMount mount,[Values] FrameUV layout)
        {
            var r = new FrameRecipe { outline = outline, mount = mount, uvLayout = layout, curveSegments = 7 };
            FrameMesh.Build(r,mesh);
            Assert.AreEqual(3,mesh.subMeshCount);
            Assert.AreEqual(mount != FrameMount.Floater,mesh.GetIndexCount(1)>0);
            Assert.Greater(mesh.GetIndexCount(2),0);
            Assert.That(mesh.bounds.size.x, Is.EqualTo(r.artworkSize.x+2*(r.Border+r.railWidth)).Within(.00001f));
            var uv = mesh.uv; var pts = mesh.vertices; var norms = mesh.normals; var tan = mesh.tangents;
            Assert.AreEqual(pts.Length,uv.Length); Assert.AreEqual(pts.Length,tan.Length);
            for (int i = 0; i < pts.Length; i++)
            {
                Assert.That(norms[i].sqrMagnitude,Is.EqualTo(1).Within(.001));
                Assert.That(((Vector3)tan[i]).sqrMagnitude,Is.EqualTo(1).Within(.001));
                if (layout == FrameUV.Paintable)
                    Assert.IsTrue(uv[i].x > 0 && uv[i].x < 1 && uv[i].y > 0 && uv[i].y < 1);
            }
            var faces = mesh.triangles;
            for (int j = 0; j < faces.Length; j += 3)
            {
                int a = faces[j], b = faces[j+1], c = faces[j+2];
                var cross = Vector3.Cross(pts[b]-pts[a],pts[c]-pts[a]);
                Assert.Greater(Vector3.Dot(cross,norms[a]+norms[b]+norms[c]),0);
                Assert.Greater(Mathf.Abs(Cross(uv[b]-uv[a],uv[c]-uv[a])),0);
            }
        }

        static float Cross(Vector2 a,Vector2 b) => a.x*b.y-a.y*b.x;

        [TestCase(BandShape.Rounded, false)]
        [TestCase(BandShape.Sharp, true)]
        [TestCase(BandShape.Bevelled, true)]
        [TestCase(BandShape.Stepped, true)]
        public void CrossSectionPreservesItsFacetBreaks(BandShape shape,bool hard)
        {
            var r = new FrameRecipe { railWidth = .08f, relief = 0 };
            var b = new FrameBand { shape = shape, width = .04f, height = .012f };
            r.bands.Add(b);
            FrameMesh.Build(r,mesh);
            float x = b.Center(r.railWidth)+(shape == BandShape.Sharp ? 0 : b.width*.25f);
            var points = mesh.vertices; var normals = mesh.normals;
            // On the bottom rail, both adjacent faces have a vertex at the right mitre.
            var expected = new Vector3(r.artworkSize.x*.5f+r.Border+x,-r.artworkSize.y*.5f-r.Border-x,-r.Height(x/r.railWidth));
            int bottomRailVertices = (mesh.GetTriangles(0).Max()+1)/4;
            var adjacent = Enumerable.Range(0,bottomRailVertices).Where(i => (points[i]-expected).sqrMagnitude < 1e-12f)
                .Select(i => normals[i]).ToArray();
            Assert.AreEqual(2,adjacent.Length);
            Assert.AreEqual(hard,Vector3.Dot(adjacent[0],adjacent[1]) < .995f);
        }

        [TestCase(BandAnchor.Relative,.05f)]
        [TestCase(BandAnchor.Inner,.025f)]
        [TestCase(BandAnchor.Outer,.125f)]
        public void AnchoredLoopsRetainWidthWhenTheRailChanges(BandAnchor anchor,float expected)
        {
            var r = new FrameRecipe { railWidth = .10f };
            var b = new FrameBand { position = .25f, width = .009f, height = -.003f };
            r.bands.Add(b);
            b.SetAnchor(r.railWidth,anchor);
            r.railWidth = .20f;
            Assert.That(b.Center(r.railWidth),Is.EqualTo(expected).Within(.00001f));
            var clone = r.Copy();
            Assert.AreEqual(.009f,clone.bands[0].Width(r.railWidth));
            Assert.AreEqual(b.height,clone.bands[0].height);
            Assert.AreNotSame(b,clone.bands[0]);
        }

        [Test, Combinatorial]
        public void NarrowBandsReachEverySideInBothUvLayouts([Values] FrameOutline outline,[Values] FrameUV layout)
        {
            var r = new FrameRecipe { relief = 0, outline = outline, uvLayout = layout, curveSegments = 4 };
            var ridge = new FrameBand { position = .37f, width = .001f, height = .009f };
            var groove = new FrameBand { position = .72f, width = .003f, height = -.002f };
            r.bands.Add(ridge); r.bands.Add(groove);
            FrameMesh.Build(r,mesh);
            foreach (var band in r.bands)
            {
                float distance = r.Border+band.Center(r.railWidth);
                var crest = mesh.vertices.Where(p => Mathf.Abs(p.z+band.height)<.000001f).ToArray();
                Assert.Greater(crest.Length,3);
                Assert.That(crest.Max(p => p.x),Is.EqualTo(r.artworkSize.x*.5f+distance).Within(.00001f));
                Assert.That(crest.Min(p => p.y),Is.EqualTo(-r.artworkSize.y*.5f-distance).Within(.00001f));
            }
            Assert.AreEqual(mesh.vertexCount,mesh.tangents.Length);
            if (layout == FrameUV.Paintable) Assert.IsTrue(mesh.uv.All(v => v.x>0 && v.x<1 && v.y>0 && v.y<1));
        }

        [Test]
        public void DraggingAProfileKeyAcrossASubdivisionKeepsTrianglesDistinct()
        {
            var r = new FrameRecipe { artworkSize = new Vector2(4,4), railWidth = .008f, relief = 0 };
            r.profile.AddKey(.50000006f,.5f);
            FrameMesh.Build(r,mesh);
            var points = mesh.vertices;
            var indices = mesh.GetTriangles(0);
            for (int i = 0; i < indices.Length; i += 3)
            {
                var a = points[indices[i]];
                Assert.Greater(Vector3.Cross(points[indices[i+1]]-a,points[indices[i+2]]-a).sqrMagnitude,0);
            }
        }

        [TestCase(FrameOutline.Rectangle,FrameMount.Matted)]
        [TestCase(FrameOutline.Oval,FrameMount.Floater)]
        [TestCase(FrameOutline.Rounded,FrameMount.ShadowBox)]
        public void AtlasTrianglesHaveDisjointInteriors(FrameOutline outline,FrameMount mount)
        {
            var r = new FrameRecipe { outline = outline, mount = mount, curveSegments = 4, uvLayout = FrameUV.Paintable };
            FrameMesh.Build(r,mesh);
            var idx = mesh.triangles; var uv = mesh.uv;
            for (int i = 0; i < idx.Length; i += 3)
                for (int j = i+3; j < idx.Length; j += 3)
                    if (!Separated(uv,idx,i,j) && !Separated(uv,idx,j,i))
                        Assert.Fail($"Atlas triangle {i/3} overlaps {j/3}");
        }

        static bool Separated(Vector2[] uv,int[] ids,int a,int b)
        {
            for (int k = 0; k < 3; k++)
            {
                Vector2 p = uv[ids[a+k]], edge = (uv[ids[a+(k+1)%3]]-p).normalized;
                float sign = Mathf.Sign(Cross(edge,uv[ids[a+(k+2)%3]]-p));
                bool outside = true;
                for (int n = 0; n < 3; n++)
                    if (sign*Cross(edge,uv[ids[b+n]]-p) > .0000002f) outside = false;
                if (outside) return true;
            }
            return false;
        }

        [Test]
        public void TiledRailsKeepPhysicalScaleAndArtGetsOneImage()
        {
            var r = new FrameRecipe { profile = FrameRecipe.Profile(Moulding.Flat) };
            FrameMesh.Build(r,mesh);
            var uv = mesh.uv;
            Assert.AreEqual((r.artworkSize.x+2*r.Border)/r.tileSize,uv[2].x-uv[0].x,.0001);
            var art = mesh.GetTriangles(2).Select(i => uv[i]).ToArray();
            CollectionAssert.Contains(art,Vector2.zero);
            CollectionAssert.Contains(art,Vector2.one);
            r.matWidth = 0; r.includeArtwork = false;
            FrameMesh.Build(r,mesh);
            Assert.AreEqual(0,mesh.GetIndexCount(1)); Assert.AreEqual(0,mesh.GetIndexCount(2));
        }

        [Test, Combinatorial]
        public void RepeatsCoverTheLoopAndSurviveResizing([Values] FrameOutline outline,
            [Values(BandOrnament.Beads,BandOrnament.LeafScroll,BandOrnament.RopeTwist,BandOrnament.Fluting,BandOrnament.EggAndDart)] BandOrnament motif,[Values] FrameUV layout)
        {
            var r = new FrameRecipe { outline = outline, relief = 0, railWidth = .07f, uvLayout = layout };
            var band = new FrameBand { height = 0, detailSize = .009f, detailGap = .007f, mirrorOppositeRails = true };
            r.bands.Add(band);
            FrameMesh.Build(r,mesh);
            int baseFaces = (int)mesh.GetIndexCount(0);
            band.ornament = motif;
            FrameMesh.Build(r,mesh);
            var idx = mesh.GetTriangles(0).Skip(baseFaces).ToArray();
            var v = mesh.vertices; var uv = mesh.uv; var n = mesh.normals; var tan = mesh.tangents;
            var corners = new bool[4];
            Assert.AreEqual(3,mesh.subMeshCount);
            for (int i=0;i<idx.Length;i+=3)
            {
                int a=idx[i], b=idx[i+1], c=idx[i+2];
                var cross=Vector3.Cross(v[b]-v[a],v[c]-v[a]);
                if (!(Vector3.Dot(cross,n[a]+n[b]+n[c]) > 0 && Mathf.Abs(Cross(uv[b]-uv[a],uv[c]-uv[a])) > 0))
                    Assert.Fail($"Invalid relief triangle {i/3} on {outline} {motif}");
                if (!float.IsFinite(tan[a].x) || !float.IsFinite(tan[a].y) || !float.IsFinite(tan[a].z))
                    Assert.Fail("Invalid tangent");
                corners[(v[a].x>0 ? 1 : 0)+(v[a].y>0 ? 2 : 0)] = true;
            }
            Assert.IsTrue(corners.All(x=>x));
            if (layout == FrameUV.Paintable) Assert.IsTrue(uv.All(p => p.x>0 && p.x<1 && p.y>0 && p.y<1));
            float crest = idx.Min(i=>v[i].z);
            band.detailGap = .10f;
            FrameMesh.Build(r,mesh);
            Assert.Less((int)mesh.GetIndexCount(0)-baseFaces,idx.Length);
            band.detailSize *= 2;
            FrameMesh.Build(r,mesh);
            v = mesh.vertices;
            Assert.That(mesh.GetTriangles(0).Skip(baseFaces).Min(i=>v[i].z),Is.EqualTo(crest*2).Within(.00001f));
            band.ornament = BandOrnament.None;
            FrameMesh.Build(r,mesh);
            Assert.AreEqual(baseFaces,mesh.GetIndexCount(0));
        }

        [TestCase(BandOrnament.RopeTwist,0f)]
        [TestCase(BandOrnament.Fluting,.035f)]
        public void ContinuousTreatmentsMeetAlongTheMitre(BandOrnament ornament,float gap)
        {
            var r = new FrameRecipe { railWidth = .07f, relief = 0 };
            var band = new FrameBand { height = 0, detailSize = .021f, detailGap = gap };
            r.bands.Add(band);
            FrameMesh.Build(r,mesh);
            int railIndices = (int)mesh.GetIndexCount(0);
            band.ornament = ornament;
            FrameMesh.Build(r,mesh);
            var p = mesh.vertices;
            var ids = mesh.GetTriangles(0).Skip(railIndices).ToArray();
            bool OnJoin(Vector3 v) => v.x>0 && v.y<0 &&
                Mathf.Abs(v.x-r.artworkSize.x*.5f+v.y+r.artworkSize.y*.5f)<.000001f;
            var join = ids.Distinct().Select(i=>p[i]).Where(OnJoin).ToArray();
            Assert.Greater(join.Length,8);
            foreach (var v in join)
                Assert.AreEqual(2,join.Count(other=>(v-other).sqrMagnitude<1e-12f),"Unmatched mitre vertex");
            for (int i=0;i<ids.Length;i+=3)
                if (OnJoin(p[ids[i]]) && OnJoin(p[ids[i+1]]) && OnJoin(p[ids[i+2]]))
                    Assert.Fail("Internal cap on a continuous loop");
        }

        [TestCase(BandOrnament.Beads)]
        [TestCase(BandOrnament.LeafScroll)]
        [TestCase(BandOrnament.RopeTwist)]
        [TestCase(BandOrnament.Fluting)]
        [TestCase(BandOrnament.EggAndDart)]
        public void OrnamentIslandsHaveDisjointInteriors(BandOrnament motif)
        {
            var r = new FrameRecipe { artworkSize = new Vector2(.07f,.06f), curveSegments = 4, uvLayout = FrameUV.Paintable };
            r.bands.Add(new FrameBand { ornament = motif, detailGap = .2f });
            FrameMesh.Build(r,mesh);
            AssertAtlasDisjoint();
        }

        void AssertAtlasDisjoint()
        {
            var indices = mesh.triangles; var uv = mesh.uv;
            var triangles = Enumerable.Range(0,indices.Length/3).Select(k => {
                int i = k*3;
                Vector2 a=uv[indices[i]], b=uv[indices[i+1]], c=uv[indices[i+2]];
                return (i,min: Vector2.Min(a,Vector2.Min(b,c)),max: Vector2.Max(a,Vector2.Max(b,c)));
            }).OrderBy(t => t.min.x).ToArray();
            for (int i=0;i<triangles.Length;i++)
            {
                var a=triangles[i];
                for (int j=i+1;j<triangles.Length && triangles[j].min.x<a.max.x;j++)
                {
                    var b=triangles[j];
                    if (a.min.y>=b.max.y || b.min.y>=a.max.y) continue;
                    if (!Separated(uv,indices,a.i,b.i) && !Separated(uv,indices,b.i,a.i))
                        Assert.Fail($"Overlapping ornament atlas triangles {a.i/3} and {b.i/3}");
                }
            }
        }

        [Test, Combinatorial]
        public void FourCornersShareOneRecipeAndMouldingSlot(
            [Values(CornerStyle.LeafFan,CornerStyle.TwinScroll,CornerStyle.ScallopShell)] CornerStyle style,
            [Values(FrameOutline.Rectangle,FrameOutline.Rounded)] FrameOutline shape,[Values] FrameUV layout)
        {
            var r = new FrameRecipe { outline = shape, uvLayout = layout, relief = 0, curveSegments = 5 };
            FrameMesh.Build(r,mesh);
            int rail=(int)mesh.GetIndexCount(0);
            r.corners.style=style;
            FrameMesh.Build(r,mesh);
            var ids=mesh.GetTriangles(0).Skip(rail).ToArray();
            Assert.Greater(ids.Length,0); Assert.AreEqual(0,ids.Length%12);
            Assert.AreEqual(3,mesh.subMeshCount);
            var points=mesh.vertices; var normals=mesh.normals; var uv=mesh.uv;
            var quadrants=new int[4];
            for (int i=0;i<ids.Length;i+=3)
            {
                int a=ids[i],b=ids[i+1],c=ids[i+2];
                Vector3 p=points[a];
                quadrants[(p.x<0 ? 1 : 0)+(p.y<0 ? 2 : 0)]++;
                var normal=Vector3.Cross(points[b]-p,points[c]-p);
                if (!(normal.z<0 && Vector3.Dot(normal,normals[a]+normals[b]+normals[c])>0 &&
                      Mathf.Abs(Cross(uv[b]-uv[a],uv[c]-uv[a]))>0))
                    Assert.Fail($"Invalid corner face {i/3} on {style}, {shape}");
            }
            Assert.IsTrue(quadrants.All(n=>n==ids.Length/12));
            Assert.That(ids.Min(i=>points[i].z),Is.EqualTo(-r.corners.relief).Within(.00001f));
            if (layout==FrameUV.Paintable) AssertAtlasDisjoint();
            var copy=r.Copy();
            copy.corners.relief*=2;
            FrameMesh.Build(copy,mesh);
            points=mesh.vertices;
            Assert.That(ids.Min(i=>points[i].z),Is.EqualTo(-2*r.corners.relief).Within(.00001f));
            copy.outline=FrameOutline.Oval;
            FrameMesh.Build(copy,mesh);
            var count=mesh.GetIndexCount(0);
            copy.corners.style=CornerStyle.None;
            FrameMesh.Build(copy,mesh);
            Assert.AreEqual(count,mesh.GetIndexCount(0));
        }

        [TestCase(FrameOutline.Rectangle)]
        [TestCase(FrameOutline.Rounded)]
        public void LargeCornersFitTheShortRailsAndKeepRequestedSize(FrameOutline outline)
        {
            var r = new FrameRecipe { artworkSize = new Vector2(.05f,.5f), railWidth = .3f, matWidth = 0,
                outline = outline, cornerFraction = .45f, curveSegments = 4 };
            r.corners.size = .3f;
            foreach (CornerStyle style in new[] { CornerStyle.LeafFan, CornerStyle.TwinScroll, CornerStyle.ScallopShell })
            {
                r.corners.style = CornerStyle.None;
                FrameMesh.Build(r,mesh);
                int rail=(int)mesh.GetIndexCount(0);
                r.corners.style = style;
                r.corners.position = 0;
                FrameMesh.Build(r,mesh);
                var ids=mesh.GetTriangles(0).Skip(rail).ToArray();
                var p=mesh.vertices;
                Assert.IsTrue(ids.Take(ids.Length/4).All(i=>p[i].x>0 && p[i].y>0));
                Assert.Less(r.corners.FittedSize(r),.1f);
                Assert.AreEqual(.3f,r.corners.size);
            }
        }

        [Test, Combinatorial]
        public void SculptedRectanglePreservesOpeningAndLoopFollow([Values] FrameMount mount,[Values] FrameUV layout)
        {
            var r=new FrameRecipe {railWidth=.06f,relief=0,curveSegments=8,mount=mount,uvLayout=layout};
            var band=new FrameBand {position=.45f,width=.005f,height=.008f,edgeFollow=.5f};
            r.bands.Add(band);
            FrameMesh.Build(r,mesh);
            Vector3[] SlotVertices(int slot)
            {
                var v=mesh.vertices; return mesh.GetTriangles(slot).Select(i=>v[i]).ToArray();
            }
            var art=SlotVertices(2); var mat=SlotVertices(1);
            r.edges.corner=.020f; r.edges.waist=-.009f; r.edges.centre=.016f;
            r.edges.SeparateTop(true); r.edges.topCentre=.023f;
            FrameMesh.Build(r,mesh);
            CollectionAssert.AreEqual(art,SlotVertices(2)); CollectionAssert.AreEqual(mat,SlotVertices(1));
            var points=mesh.vertices; var uv=mesh.uv; var normals=mesh.normals;
            float baseTop=r.artworkSize.y*.5f+r.Border;
            Assert.That(points.Any(p=>Mathf.Abs(p.x)<.000002f && Mathf.Abs(p.y-baseTop-r.railWidth-.023f)<.000002f));
            var idx=mesh.GetTriangles(0);
            for (int i=0;i<idx.Length;i+=3)
            {
                int a=idx[i],b=idx[i+1],c=idx[i+2];
                var cross=Vector3.Cross(points[b]-points[a],points[c]-points[a]);
                if (!(Vector3.Dot(cross,normals[a]+normals[b]+normals[c])>0 &&
                      Mathf.Abs(Cross(uv[b]-uv[a],uv[c]-uv[a]))>0))
                    Assert.Fail($"Invalid sculpted triangle {i/3} in {mount}");
            }
            if(layout==FrameUV.Paintable) AssertAtlasDisjoint();
            foreach (float follow in new[]{0f,.5f,1f})
            {
                band.edgeFollow=follow; FrameMesh.Build(r,mesh);
                float top=baseTop+band.Center(r.railWidth)+.023f*follow;
                Assert.IsTrue(mesh.vertices.Any(p=>Mathf.Abs(p.x)<.000002f && Mathf.Abs(p.y-top)<.000002f &&
                    Mathf.Abs(p.z+band.height)<.000002f),"Missing ridge crest at requested follow");
            }
            var clone=r.Copy();
            Assert.AreEqual(.023f,clone.edges.topCentre); Assert.AreEqual(1,clone.bands[0].edgeFollow);
            clone.outline=FrameOutline.Rounded; FrameMesh.Build(clone,mesh);
            var unshaped=mesh.vertices; clone.edges=new FrameEdges(); FrameMesh.Build(clone,mesh);
            CollectionAssert.AreEqual(unshaped,mesh.vertices);
        }

        [Test, Combinatorial]
        public void OvalScallopsPreserveTheOpeningAndFollow([Values] FrameMount mount,[Values] FrameUV layout)
        {
            var r=new FrameRecipe {outline=FrameOutline.Oval,artworkSize=new Vector2(.21f,.36f),
                curveSegments=8,railWidth=.05f,relief=0,mount=mount,uvLayout=layout};
            var loop=new FrameBand {width=.006f,height=.004f,position=.5f};
            r.bands.Add(loop);
            r.edges.ovalTop=.028f; r.edges.ovalBottom=-.012f; r.edges.ovalSides=.018f;
            foreach (float follow in new[]{0f,.5f,1f})
            {
                loop.edgeFollow=follow;
                FrameMesh.Build(r,mesh);
                var points=mesh.vertices;
                for (int pole=0;pole<4;pole++)
                {
                    Vector2 axis=pole==0 ? Vector2.down : pole==1 ? Vector2.right : pole==2 ? Vector2.up : Vector2.left;
                    float projection=pole==0 ? -.012f : pole==2 ? .028f : .018f;
                    float sight=(pole%2==0 ? r.artworkSize.y : r.artworkSize.x)*.5f+r.Border;
                    Vector2 expected=axis*(sight+loop.Center(r.railWidth)+projection*follow);
                    Assert.IsTrue(points.Any(p=>Vector2.Distance(p,expected)<.000002f && Mathf.Abs(p.z+loop.height)<.000002f));
                    Assert.That(Vector2.Distance(FrameEdgeTool.Handle(r,pole),axis*(sight+r.railWidth+projection)),Is.LessThan(.000002f));
                    Assert.IsTrue(points.Any(p=>Vector2.Distance(p,axis*sight)<.000002f && Mathf.Abs(p.z)<.000002f));
                }
            }
            var v=mesh.vertices; var uv=mesh.uv; var normals=mesh.normals;
            for (int slot=0;slot<3;slot++)
            {
                var tris=mesh.GetTriangles(slot);
                for (int i=0;i<tris.Length;i+=3)
                {
                    int a=tris[i], b=tris[i+1], c=tris[i+2];
                    if (!(Vector3.Dot(Vector3.Cross(v[b]-v[a],v[c]-v[a]),normals[a]+normals[b]+normals[c])>0 &&
                        Mathf.Abs(Cross(uv[b]-uv[a],uv[c]-uv[a]))>0)) Assert.Fail($"Invalid oval triangle {slot}/{i/3}");
                }
            }
            if (layout==FrameUV.Paintable) AssertAtlasDisjoint();
            Assert.AreEqual(r.edges.ovalTop,r.Copy().edges.ovalTop);
        }

        [Test]
        public void OvalLoopCanBePickedOnTheSlopingScallop()
        {
            var obj=new GameObject("Oval loop picking");
            try
            {
                var f=obj.AddComponent<ProceduralFrame>(); var r=f.recipe;
                r.outline=FrameOutline.Oval; r.edges.ovalTop=.032f;
                var band=new FrameBand {position=.6f,width=.005f,edgeFollow=1};
                r.bands.Add(band); f.Rebuild();
                var tool=new FrameBandTool();
                var line=tool.Loop(f,band.position);
                Vector3 p=line[(line.Length-1)*17/32];
                Vector2 Project(Vector3 v)=>new Vector2(v.x,-v.y)*3000;
                Assert.IsTrue(tool.Press(f,new Ray(p+Vector3.back,Vector3.forward),Project(p),Project));
                Assert.AreEqual(0,tool.Selected);
                Assert.AreEqual(FrameBandTool.Grip.Move,tool.Dragging);
            }
            finally { Object.DestroyImmediate(obj); }
        }

        [Test]
        public void PickingTheWaistUsesTheDisplacedLoop()
        {
            var obj=new GameObject("Edge picking");
            try
            {
                var f=obj.AddComponent<ProceduralFrame>(); var r=f.recipe;
                r.railWidth=.08f; r.edges.waist=.024f;
                var inner=new FrameBand {position=.25f,width=.004f,height=.003f,edgeFollow=0};
                var outer=new FrameBand {position=.65f,width=.006f,height=.008f,edgeFollow=1};
                r.bands.Add(inner); r.bands.Add(outer); f.Rebuild();
                float offset=r.Border+outer.Center(r.railWidth)+r.edges.waist;
                var p=new Vector3(.5f*(r.artworkSize.x*.5f+offset),r.artworkSize.y*.5f+offset,0);
                Vector2 Project(Vector3 local) => new Vector2(local.x,-local.y)*2000;
                var tool=new FrameBandTool();
                Assert.IsTrue(tool.Press(f,new Ray(p+Vector3.back,Vector3.forward),Project(p),Project));
                Assert.AreEqual(1,tool.Selected); Assert.AreEqual(FrameBandTool.Grip.Move,tool.Dragging);
                tool.Release();
                var line=tool.Loop(f,outer.position);
                Assert.IsTrue(line.Any(v=>Vector2.Distance(v,p)<.000002f));
            }
            finally { Object.DestroyImmediate(obj); }
        }

        [TestCase(BandOrnament.Beads)]
        [TestCase(BandOrnament.LeafScroll)]
        [TestCase(BandOrnament.RopeTwist)]
        [TestCase(BandOrnament.Fluting)]
        [TestCase(BandOrnament.EggAndDart)]
        public void ShapedRailsCarryOrnamentsAcrossTheMitres(BandOrnament pattern)
        {
            var r=new FrameRecipe {railWidth=.06f,relief=0,curveSegments=8,uvLayout=FrameUV.Paintable};
            r.edges.waist=-.009f;r.edges.corner=.018f;r.edges.centre=.014f;
            var band=new FrameBand {position=.55f,width=.012f,height=.002f,edgeFollow=.75f,detailSize=.017f,detailGap=0};
            r.bands.Add(band); FrameMesh.Build(r,mesh); int start=(int)mesh.GetIndexCount(0);
            band.ornament=pattern;r.corners.style=CornerStyle.LeafFan; FrameMesh.Build(r,mesh);
            var pts=mesh.vertices;var ns=mesh.normals;var uv=mesh.uv;var idx=mesh.GetTriangles(0);
            for(int i=start;i<idx.Length;i+=3)
            {
                int a=idx[i],b=idx[i+1],c=idx[i+2];
                var n=Vector3.Cross(pts[b]-pts[a],pts[c]-pts[a]);
                if (!(Vector3.Dot(n,ns[a]+ns[b]+ns[c])>0 && Mathf.Abs(Cross(uv[b]-uv[a],uv[c]-uv[a]))>0))
                    Assert.Fail($"Invalid {pattern} or corner triangle {(i-start)/3}");
            }
        }

        [Test]
        public void SwitchingDesignsInvalidatesPickingAndPreservesAssignedMaterials()
        {
            var obj=new GameObject("Replace edited frame");
            try
            {
                var frame=obj.AddComponent<ProceduralFrame>();
                frame.recipe.railWidth=.06f; frame.Rebuild();
                var renderer=frame.GetComponent<MeshRenderer>();
                var materials=renderer.sharedMaterials;
                materials[0]=materials[2]; renderer.sharedMaterials=materials;
                var tool=new FrameBandTool(); tool.Arm(1);
                var side=new Vector3(frame.recipe.artworkSize.x*.5f+frame.recipe.Border+.03f,0,-1);
                tool.Hover(frame,new Ray(side,Vector3.forward));
                Assert.That(tool.Preview,Is.EqualTo(.5f).Within(.001f));
                var recipe=frame.recipe.Copy(); recipe.artworkSize=Vector2.one;
                recipe.outline=FrameOutline.Oval; recipe.edges.ovalSides=.03f;
                frame.Apply(recipe);
                tool.Hover(frame,new Ray(side,Vector3.forward));
                Assert.AreEqual(-1,tool.Preview);
                side.x=.5f+recipe.Border+(recipe.railWidth+.03f)*.5f;
                tool.Hover(frame,new Ray(side,Vector3.forward));
                Assert.That(tool.Preview,Is.EqualTo(.5f).Within(.001f));
                CollectionAssert.AreEqual(materials,renderer.sharedMaterials);
            }
            finally { Object.DestroyImmediate(obj); }
        }

        [Test, Combinatorial]
        public void TabletopFrameAndFootMeetTheGround([Values] FrameOutline outline,[Values] FrameMount mount,[Values] FrameUV layout)
        {
            var r=new FrameRecipe { outline=outline, mount=mount, uvLayout=layout, curveSegments=5,
                artworkSize=new Vector2(.29f,.14f), easelBack=true, leanAngle=25, includeArtwork=false };
            r.edges.corner=.018f; r.edges.ovalBottom=.021f;
            FrameMesh.Build(r,mesh);
            Assert.AreEqual(4,mesh.subMeshCount);
            var v=mesh.vertices; var uv=mesh.uv; var n=mesh.normals;
            Assert.That(v.Min(p=>p.y),Is.EqualTo(0).Within(.000002f));
            var frontContact=mesh.GetTriangles(0).Where(i=>Mathf.Abs(v[i].y)<.000002f).ToArray();
            var backContact=mesh.GetTriangles(3).Where(i=>Mathf.Abs(v[i].y)<.000002f).ToArray();
            Assert.IsNotEmpty(frontContact); Assert.IsNotEmpty(backContact);
            Assert.Greater(backContact.Min(i=>v[i].z),frontContact.Max(i=>v[i].z)+.01f);
            Assert.Greater(backContact.Max(i=>v[i].x)-backContact.Min(i=>v[i].x),.01f);
            var indices=mesh.GetTriangles(3);
            for (int j=0;j<indices.Length;j+=3)
            {
                int a=indices[j],b=indices[j+1],c=indices[j+2];
                Assert.Greater(Vector3.Dot(Vector3.Cross(v[b]-v[a],v[c]-v[a]),n[a]),0);
                Assert.Greater(Mathf.Abs(Cross(uv[b]-uv[a],uv[c]-uv[a])),0);
            }
            if (layout==FrameUV.Paintable) AssertAtlasDisjoint();
            Assert.IsTrue(r.Copy().easelBack); Assert.AreEqual(25,r.Copy().leanAngle);
            r.easelBack=false; FrameMesh.Build(r,mesh);
            Assert.AreEqual(3,mesh.subMeshCount);
        }

        [TestCase(5f)]
        [TestCase(25f)]
        public void PinchedThinTabletopRailStillTouchesAndCanBePicked(float angle)
        {
            var obj=new GameObject("Tabletop picking");
            try
            {
                obj.transform.SetPositionAndRotation(new Vector3(1,2,3),Quaternion.Euler(0,35,0));
                var frame=obj.AddComponent<ProceduralFrame>(); var r=frame.recipe;
                r.artworkSize=new Vector2(.64f,.48f); r.outline=FrameOutline.Oval;
                r.railWidth=.008f; r.edges.ovalBottom=-.1f; r.easelBack=true; r.leanAngle=angle;
                frame.Rebuild();
                var v=frame.GeneratedMesh.vertices;
                Assert.That(frame.GeneratedMesh.GetTriangles(0).Min(k=>v[k].y),Is.EqualTo(0).Within(.000002f));
                var pose=frame.DesignToWorld;
                var ray=new Ray(pose.MultiplyPoint3x4(new Vector3(r.artworkSize.x*.5f+r.Border+r.railWidth*.5f,0,-1)),pose.MultiplyVector(Vector3.forward));
                var tool=new FrameBandTool(); tool.Arm(1); tool.Hover(frame,ray);
                Assert.That(tool.Preview,Is.EqualTo(.5f).Within(.01f));
            }
            finally { Object.DestroyImmediate(obj); }
        }

        [Test]
        public void TabletopBackSurvivesBakeAndObjExport()
        {
            var r=new FrameRecipe {easelBack=true,uvLayout=FrameUV.Paintable};
            FrameMesh.Build(r,mesh);
            var asset=AssetDatabase.GenerateUniqueAssetPath("Assets/TabletopBake.asset");
            var folder=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));
            try
            {
                var baked=FrameInspector.Bake(mesh,asset);
                CollectionAssert.AreEqual(mesh.GetTriangles(3),baked.GetTriangles(3));
                CollectionAssert.AreEqual(mesh.uv,baked.uv);
                string path=Path.Combine(folder,"Tabletop.obj");
                FrameObj.Write(baked,path);
                CollectionAssert.Contains(File.ReadAllLines(path),"usemtl EaselBack");
                StringAssert.Contains("newmtl EaselBack",File.ReadAllText(Path.Combine(folder,"FrameSlots.mtl")));
            }
            finally
            {
                AssetDatabase.DeleteAsset(asset);
                if (Directory.Exists(folder)) Directory.Delete(folder,true);
            }
        }

        [Test]
        public void MeshAssetAndObjPreserveTheChosenLayout()
        {
            var recipe = new FrameRecipe { uvLayout = FrameUV.Paintable };
            recipe.edges.centre=.012f; recipe.edges.waist=-.005f;
            recipe.corners.style=CornerStyle.LeafFan;
            recipe.bands.Add(new FrameBand { ornament = BandOrnament.LeafScroll, detailGap = .12f });
            FrameMesh.Build(recipe,mesh);
            string asset = AssetDatabase.GenerateUniqueAssetPath("Assets/FrameBakeCheck.asset");
            string folder = Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));
            try
            {
                var baked = FrameInspector.Bake(mesh,asset);
                mesh.Clear();
                AssetDatabase.ImportAsset(asset,ImportAssetOptions.ForceUpdate);
                baked = AssetDatabase.LoadAssetAtPath<Mesh>(asset);
                Assert.Greater(baked.vertexCount,0);
                string path = Path.Combine(folder,"Frame.obj");
                FrameObj.Write(baked,path);
                FrameObj.Write(baked,path);
                var lines = File.ReadAllLines(path);
                Assert.AreEqual(baked.vertexCount,lines.Count(l => l.StartsWith("vt ")));
                Assert.AreEqual(baked.vertexCount,lines.Count(l => l.StartsWith("vn ")));
                Assert.AreEqual(baked.triangles.Length/3,lines.Count(l => l.StartsWith("f ")));
                CollectionAssert.Contains(lines,"usemtl Moulding");
                Assert.IsTrue(File.Exists(Path.Combine(folder,"FrameSlots.mtl")));
                var originalUV = baked.uv;
                FrameMesh.Build(recipe,mesh);
                CollectionAssert.AreEqual(originalUV,mesh.uv);
            }
            finally
            {
                AssetDatabase.DeleteAsset(asset);
                if (Directory.Exists(folder)) Directory.Delete(folder,true);
            }
        }
    }
}
