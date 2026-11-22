using UnityEngine;
using UnityEngine.UIElements;

namespace JLinkDev.SampleFramework
{
#if UNITY_6000_0_OR_NEWER
    [UxmlElement]
#endif
    public partial class StudioSlider : Slider
    {
#if !UNITY_6000_0_OR_NEWER
        public new class UxmlFactory : UxmlFactory<StudioSlider, UxmlTraits> { }
#endif
        readonly VisualElement fill;
        readonly FloatField readout;
        readonly Label caption, unit;

        public StudioSlider()
        {
            AddToClassList("studio-slider");
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.AddToClassList("slider-caption-row");
            hierarchy.Insert(0,row);
            caption = new Label { pickingMode = PickingMode.Ignore };
            row.Add(caption);
            var box = new VisualElement();
            box.AddToClassList("slider-readout");
            row.Add(box);
            readout = new FloatField { isDelayed = true, formatString = "0.##" };
            box.Add(readout);
            unit = new Label { pickingMode = PickingMode.Ignore };
            box.Add(unit);

            var track = this.Q(className: trackerUssClassName);
            fill = new VisualElement { pickingMode = PickingMode.Ignore };
            fill.AddToClassList("slider-fill");
            track.Add(fill);
            readout.RegisterValueChangedCallback(e => {
                e.StopPropagation();
                value = Mathf.Clamp(e.newValue,lowValue,highValue);
                readout.SetValueWithoutNotify(value);
            });
            RegisterCallback<AttachToPanelEvent>(_ => RefreshAppearance());
            RegisterCallback<GeometryChangedEvent>(_ => RefreshAppearance());
        }

        public override void SetValueWithoutNotify(float newValue)
        {
            base.SetValueWithoutNotify(newValue);
            if (fill != null) RefreshAppearance();
        }

        void RefreshAppearance()
        {
            string text = label ?? "";
            int suffix = text.LastIndexOf(" (",System.StringComparison.Ordinal);
            bool hasUnit = suffix >= 0 && text.EndsWith(")",System.StringComparison.Ordinal);
            caption.text = hasUnit ? text.Substring(0,suffix) : text;
            unit.text = hasUnit ? text.Substring(suffix+2,text.Length-suffix-3) : "";
            fill.style.width = Length.Percent(100 * Mathf.InverseLerp(lowValue,highValue,value));
            readout.SetValueWithoutNotify(value);
        }
    }
}
