using UnityEngine.UIElements;

namespace JLinkDev.SampleFramework
{
#if UNITY_6000_0_OR_NEWER
    [UxmlElement]
#endif
    public partial class WorkshopShelf : VisualElement
    {
#if !UNITY_6000_0_OR_NEWER
        public new class UxmlFactory : UxmlFactory<WorkshopShelf, UxmlTraits> { }
#endif
        readonly VisualElement cards = new VisualElement { name = "preset-cards" };

        public WorkshopShelf()
        {
            AddToClassList("studio-shelf");
            Add(Arrow("‹", "Previous preset", -1));
            Add(cards);
            Add(Arrow("›", "Next preset", 1));
        }

        Button Arrow(string label, string hint, int direction)
        {
            var arrow = new Button(() => Step(direction)) { text = label, tooltip = hint };
            arrow.AddToClassList("shelf-arrow");
            return arrow;
        }

        void Step(int direction)
        {
            int count = cards.childCount;
            if (count == 0) return;
            int current = -1;
            for (int i = 0; i < count; i++)
                if (cards[i].ClassListContains("active-preset")) current = i;
            int start = current < 0 && direction < 0 ? 0 : current;
            for (int offset = 1; offset <= count; offset++)
            {
                int next = (start + direction * offset + count) % count;
                if (!(cards[next] is Button button) || !button.enabledInHierarchy) continue;
                button.Focus();
                using (var submit = NavigationSubmitEvent.GetPooled())
                {
                    submit.target = button;
                    button.SendEvent(submit);
                }
                break;
            }
        }
    }
}
