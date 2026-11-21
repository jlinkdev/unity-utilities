using UnityEngine.UIElements;

namespace JLink.Frames
{
    public sealed partial class FrameWorkshop
    {
        readonly FrameCornerTool cornerTool = new FrameCornerTool();

        void AddCornerControls(VisualElement side)
        {
            var panel=new VisualElement();
            side.Add(panel);
            Section(panel,"Four linked corners");
            var ovalNote=AddText(panel,"Choose Rectangle or Rounded under Frame to use corner ornaments.","help");
            var options=new VisualElement(); panel.Add(options);
            Choices(options,new[]{"No ornament","Leaf fan","Twin scroll","Scallop shell"},
                i=>R.corners.style=(CornerStyle)i,()=>(int)R.corners.style);
            var settingsPanel=new VisualElement(); options.Add(settingsPanel);
            Section(settingsPanel,"Drag any corner");
            var actions=Row(settingsPanel);
            for (int i=0;i<3;i++)
            {
                var adjustment=(CornerAdjustment)i;
                var b=Button(actions,adjustment.ToString(),()=>{cornerTool.adjustment=adjustment; Refresh();});
                sync.Add(()=>b.EnableInClassList("selected",cornerTool.adjustment==adjustment));
            }
            Slider(settingsPanel,"Position (%)",0,100,()=>R.corners.position*100,v=>R.corners.position=v*.01f);
            Slider(settingsPanel,"Size (mm)",1,400,()=>R.corners.size*1000,v=>R.corners.size=v*.001f);
            Slider(settingsPanel,"Relief (mm)",.1f,100,()=>R.corners.relief*1000,v=>R.corners.relief=v*.001f);
            var fit=AddText(settingsPanel,"","help");
            sync.Add(()=>{
                panel.style.display=mode==FrameEditMode.Corners ? DisplayStyle.Flex : DisplayStyle.None;
                bool oval=R.outline==FrameOutline.Oval;
                ovalNote.style.display=oval ? DisplayStyle.Flex : DisplayStyle.None;
                options.style.display=oval ? DisplayStyle.None : DisplayStyle.Flex;
                settingsPanel.style.display=R.corners.Visible(R) ? DisplayStyle.Flex : DisplayStyle.None;
                float fitted=R.corners.FittedSize(R);
                fit.style.display=fitted<R.corners.size-.00001f ? DisplayStyle.Flex : DisplayStyle.None;
                if (fitted<R.corners.size-.00001f) fit.text=$"Fits {fitted*1000:0.#} mm on this frame.";
            });
        }
    }
}
