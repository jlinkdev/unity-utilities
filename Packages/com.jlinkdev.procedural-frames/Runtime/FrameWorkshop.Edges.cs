using UnityEngine;
using UnityEngine.UIElements;

namespace JLink.Frames
{
    public sealed partial class FrameWorkshop
    {
        readonly FrameEdgeTool edgeTool=new FrameEdgeTool();

        void AddEdgeControls(VisualElement side)
        {
            var panel=new VisualElement(); side.Add(panel);
            var heading=AddText(panel,"","section");
            var note=AddText(panel,"Choose Rectangle or Oval under Frame to sculpt the edges.","help");
            var controls=new VisualElement(); panel.Add(controls);
            var selection=AddText(controls,"","measure");
            var row=Row(controls);
            EdgeFeature Selected() => edgeTool.Selected(R);
            for (int i=0;i<3;i++)
            {
                int index=i;
                EdgeFeature Option() => (EdgeFeature)(index+(R.outline==FrameOutline.Oval ? (int)EdgeFeature.Top : 0));
                var button=Button(row,Option().ToString(),()=>{edgeTool.feature=Option(); Refresh();});
                sync.Add(()=>{
                    button.text=Option().ToString();
                    button.EnableInClassList("selected",Selected()==Option());
                });
            }
            var split=new Toggle("Shape top rail separately");
            controls.Add(split);
            split.RegisterValueChangedCallback(e=>Edit(()=>R.edges.SeparateTop(e.newValue)));
            var projection=Slider(controls,"Projection (mm)",-720*R.railWidth,400,()=>R.edges.Amount(Selected(),edgeTool.side)*1000,
                v=>R.edges.SetAmount(Selected(),edgeTool.side,Mathf.Max(v*.001f,-.72f*R.railWidth)));
            var reach=Slider(controls,"Reach (% of side)",5,25,()=>R.edges.Span(Selected())*100,
                v=>R.edges.SetSpan(Selected(),v*.01f));
            var ovalReach=Slider(controls,"Reach (degrees)",12,45,()=>R.edges.Span(Selected())*360,
                v=>R.edges.SetSpan(Selected(),v/360));
            var help=AddText(controls,"","help");
            sync.Add(()=>{
                float min=-720*R.railWidth;
                if (!Mathf.Approximately(projection.lowValue,min)) projection.lowValue=min;
                panel.style.display=mode==FrameEditMode.Edges ? DisplayStyle.Flex : DisplayStyle.None;
                bool rect=R.outline==FrameOutline.Rectangle;
                bool oval=R.outline==FrameOutline.Oval;
                heading.text=oval ? "Oval scallops" : "Rectangular silhouette";
                controls.style.display=rect || oval ? DisplayStyle.Flex : DisplayStyle.None;
                note.style.display=rect || oval ? DisplayStyle.None : DisplayStyle.Flex;
                split.SetValueWithoutNotify(R.edges.independentTop);
                split.style.display=reach.style.display=rect ? DisplayStyle.Flex : DisplayStyle.None;
                ovalReach.style.display=oval ? DisplayStyle.Flex : DisplayStyle.None;
                selection.text=oval ? (Selected()==EdgeFeature.Sides ? "Linked left and right scallops" : Selected()+" scallop") : edgeTool.Selection(R.edges);
                help.text=oval ? "Drag the gold top, green bottom or either blue side. Reach sets the angular spread of the crest and its shoulders."
                    : "Drag gold corners, green waists or blue centres. Reach controls how far the selected shape spreads along the rail.";
            });
        }
    }
}
