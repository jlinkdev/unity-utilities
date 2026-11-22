using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace JLink.Plants.Studio
{
    [Serializable]
    internal sealed class PlantWorkshopPanel
    {
        [SerializeField] StyleSheet style;
        [SerializeField] VisualTreeAsset shelfTemplate;
        [SerializeField] Texture2D[] previews;
        [SerializeField] Texture2D buttonSurface, selectedSurface;
        VisualElement root, rail, tools, shelf, items, body, tabs, footer;
        Button[] cards;
        GUISkin skin;
        public GUIStyle SectionStyle { get; private set; }
        public bool Mounted => root?.panel != null;
        public bool Ready => Mounted && root.worldBound.width > 0f && body.worldBound.height > 0f;
        public Rect Body => body.worldBound;
        public float PanelWidth => root.worldBound.width;
        public float PanelHeight => root.worldBound.height;
        public float Scale => Screen.width / root.worldBound.width;
        public float RailWidth => rail.worldBound.width;
        public float ShelfHeight => shelf.worldBound.height;
        public bool OverViewport(Vector2 point) => point.x > rail.worldBound.xMax && point.y < shelf.worldBound.yMin && !tools.worldBound.Contains(point);

        public bool Open(UIDocument identity, Action<PlantSpecies> select)
        {
            if (Mounted || !identity || !identity.panelSettings || !style || !shelfTemplate) return false;
            var parent = identity.rootVisualElement;
            if (parent?.panel == null) return false;
            root = new VisualElement { pickingMode = PickingMode.Ignore };
            root.AddToClassList("sample-studio");
            root.AddToClassList("plant-workshop");
            root.styleSheets.Add(style);
            // The identity document also owns the subject-specific header above the rail.
            var oldBackdrop = parent.Q("inspector-backdrop");
            if (oldBackdrop != null) oldBackdrop.style.display = DisplayStyle.None;
            parent.Insert(0,root);
            rail = Box(root,"plant-rail");
            tabs = Box(rail,"tabs");
            body = Box(rail,"plant-body");
            footer = Box(rail,"footer");
            tools = Box(root,"view-tools");
            shelf = Box(root,"plant-shelf");
            shelfTemplate.CloneTree(shelf);
            items = shelf.Q("preset-cards");
            var species = (PlantSpecies[])Enum.GetValues(typeof(PlantSpecies));
            cards = new Button[species.Length];
            foreach (var kind in species)
            {
                int i = (int)kind;
                var name = PlantSpeciesProfile.For(kind).DisplayName;
                var card = new Button(() => select(kind)) { name = "Preset" + kind, tooltip = name };
                cards[i] = card;
                card.AddToClassList("preset");
                items.Add(card);
                if (previews != null && i < previews.Length)
                    card.Add(new Image { image = previews[i], scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore });
                var label = new Label(name) { pickingMode = PickingMode.Ignore };
                label.AddToClassList("preset-title");
                card.Add(label);
            }
            var heading = parent.Q(className:"sample-identity");
            rail.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                shelf.style.left = rail.worldBound.xMax;
                tools.style.left = Mathf.Max(rail.worldBound.xMax,heading?.worldBound.xMax ?? 0) + 12f;
            });
            return true;
        }

        static VisualElement Box(VisualElement parent, string className)
        {
            var box = new VisualElement { pickingMode = PickingMode.Ignore };
            box.AddToClassList(className);
            parent.Add(box);
            return box;
        }

        static Button ActionButton(VisualElement parent,string label,Action action,string style)
        {
            var button = new Button(action) { text = label };
            if (!string.IsNullOrEmpty(style)) button.AddToClassList(style);
            parent.Add(button);
            return button;
        }

        public void Tab(string label,Action action) => ActionButton(tabs,label,action,null);
        public Button Tool(string label,Action action,string icon = null) => ActionButton(tools,label,action,icon);
        public Label Message()
        {
            var label = new Label { pickingMode = PickingMode.Ignore };
            label.AddToClassList("notice");
            footer.Add(label);
            return label;
        }

        public Button Action(string label,Action action,string style = null) => ActionButton(footer,label,action,style);
        public VisualElement ActionRow() => Box(footer,"row");
        public Button RowAction(VisualElement row,string label,Action action,string style = null) => ActionButton(row,label,action,style);

        public void Select(int tab,PlantSpecies species,bool allowPresets = true)
        {
            for (int i=0; i<tabs.childCount; i++) tabs[i].EnableInClassList("chosen",i==tab);
            for (int i=0; i<cards.Length; i++) cards[i].EnableInClassList("active-preset",i==(int)species);
            shelf.SetEnabled(allowPresets);
        }

        public GUISkin InspectorSkin(GUISkin original)
        {
            if (skin) return skin;
            skin = UnityEngine.Object.Instantiate(original);
            skin.hideFlags = HideFlags.DontSave;
            var button = skin.button;
            button.fontSize = 12;
            button.fontStyle = FontStyle.Normal;
            button.fixedHeight = 25;
            button.wordWrap = true;
            button.margin = new RectOffset(2,2,2,2);
            button.padding = new RectOffset(3,3,2,2);
            button.border = new RectOffset(6,6,6,6);
            button.normal.background = button.hover.background = button.focused.background = buttonSurface;
            button.onNormal.background = button.onHover.background = button.onFocused.background = button.active.background = button.onActive.background = selectedSurface;
            button.normal.textColor = button.onNormal.textColor = button.hover.textColor = Color.white;
            skin.toggle = new GUIStyle(button) { alignment = TextAnchor.MiddleLeft, padding = new RectOffset(8,4,2,2) };
            skin.label.fontSize = 12;
            skin.label.wordWrap = true;
            skin.label.normal.textColor = new Color(.88f,.91f,.93f);
            SectionStyle = new GUIStyle(skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
            skin.verticalScrollbar.fixedWidth = 6;
            skin.verticalScrollbarThumb.fixedWidth = 5;
            skin.verticalScrollbarThumb.border = new RectOffset(2,2,2,2);
            skin.verticalScrollbarThumb.normal.background = buttonSurface;
            skin.verticalScrollbarThumb.hover.background = skin.verticalScrollbarThumb.active.background = buttonSurface;
            skin.verticalScrollbar.margin = new RectOffset(3,0,0,0);
            skin.verticalScrollbarThumb.margin = new RectOffset(0,0,0,0);
            skin.verticalScrollbarUpButton = new GUIStyle(GUIStyle.none);
            skin.verticalScrollbarDownButton = new GUIStyle(GUIStyle.none);
            return skin;
        }

        public void Close()
        {
            var backdrop = root?.parent?.Q("inspector-backdrop");
            if (backdrop != null) backdrop.style.display = StyleKeyword.Null;
            root?.RemoveFromHierarchy();
            root = null;
            if (skin) UnityEngine.Object.Destroy(skin);
            skin = null;
        }
    }
}
