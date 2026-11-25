using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JLink.ProceduralTextures.Editor
{
    internal sealed class TextureInspectorPreview : IDisposable
    {
        internal enum View { Result, Scalar, RGB, Red, Green, Blue, Alpha }
        static readonly string[] Resolutions = { "32 px", "128 px", "256 px", "512 px", "1024 px" };
        static readonly int[] Limits = { 32, 128, 256, 512, 1024 };
        Texture2D source, channel, checker;
        uint sourceHash;
        int sourceLimit;
        bool sourceScalar;
        View view, displayedView;
        int resolution = 2;
        float zoom = 1;
        Vector2 pan, offset, velocity = new Vector2(0.2f, 0);
        bool tiled, playing;
        double lastFrame, nextBake;

        internal bool Animating => tiled && playing;

        internal void Draw(TextureRecipe recipe, uint hash, bool valid, Action repaint)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                view = (View)EditorGUILayout.EnumPopup(view, EditorStyles.toolbarPopup, GUILayout.MinWidth(46));
                resolution = EditorGUILayout.Popup(resolution, Resolutions, EditorStyles.toolbarPopup, GUILayout.Width(74));
                bool repeat = GUILayout.Toggle(tiled, new GUIContent("Tile", "Repeat the preview three times per axis"), EditorStyles.toolbarButton);
                if (repeat != tiled) lastFrame = EditorApplication.timeSinceStartup;
                tiled = repeat;
                if (GUILayout.Button(new GUIContent("Fit", "Center the preview and reset zoom"), EditorStyles.toolbarButton))
                    Fit();
            }
            if (tiled)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool play = GUILayout.Toggle(playing, new GUIContent("Scroll", "Animate the tile offset"), GUILayout.Width(60));
                    if (play != playing) lastFrame = EditorApplication.timeSinceStartup;
                    playing = play;
                    if (playing) velocity = EditorGUILayout.Vector2Field("UV per second", velocity);
                    else offset = EditorGUILayout.Vector2Field("UV offset", offset);
                }
            }
            if (valid && Event.current.type == EventType.Layout)
                Refresh(recipe, hash, repaint);

            Rect area = GUILayoutUtility.GetRect(10, 190, GUILayout.ExpandWidth(true));
            Navigate(area);
            if (source && Event.current.type == EventType.Repaint)
            {
                double now = EditorApplication.timeSinceStartup;
                if (Animating)
                {
                    offset += velocity * (float)(now - lastFrame);
                    offset = new Vector2(Mathf.Repeat(offset.x, 1), Mathf.Repeat(offset.y, 1));
                }
                lastFrame = now;
                Paint(area, recipe.width == 1, recipe.height == 1);
            }
            if (source)
                EditorGUILayout.LabelField($"Preview {source.width} × {source.height}    Output {recipe.width} × {recipe.height}", EditorStyles.miniLabel);
        }

        void Refresh(TextureRecipe recipe, uint hash, Action repaint)
        {
            bool scalar = view == View.Scalar && recipe.colorMode != TextureColorMode.Grayscale;
            int limit = Limits[resolution];
            bool bake = !source || hash != sourceHash || sourceScalar != scalar || sourceLimit != limit;
            if (bake)
            {
                if (source && EditorApplication.timeSinceStartup < nextBake) { repaint(); return; }
                var generated = TextureGenerator.GeneratePreview(recipe, limit, scalar);
                generated.hideFlags = HideFlags.HideAndDontSave;
                generated.wrapMode = TextureWrapMode.Repeat;
                Object.DestroyImmediate(source);
                source = generated;
                sourceHash = hash;
                sourceScalar = scalar;
                sourceLimit = limit;
                nextBake = EditorApplication.timeSinceStartup + 0.09;
            }
            if (!bake && displayedView == view) return;
            Object.DestroyImmediate(channel);
            channel = null;
            if (view >= View.RGB)
            {
                var pixels = source.GetPixels32();
                for (int i = 0; i < pixels.Length; i++) pixels[i] = Inspect(pixels[i], view);
                channel = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, true)
                    { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Repeat };
                channel.SetPixels32(pixels);
                channel.Apply(false);
            }
            displayedView = view;
        }

        internal static Color32 Inspect(Color32 pixel, View mode)
        {
            if (mode == View.RGB) return new Color32(pixel.r, pixel.g, pixel.b, 255);
            byte value = mode switch { View.Red => pixel.r, View.Green => pixel.g, View.Blue => pixel.b, View.Alpha => pixel.a,
                _ => throw new ArgumentOutOfRangeException(nameof(mode)) };
            return new Color32(value, value, value, 255);
        }

        void Paint(Rect area, bool column, bool row)
        {
            EditorGUI.DrawRect(area, new Color(0.14f, 0.14f, 0.14f));
            float scale = Mathf.Min(area.width / source.width, area.height / source.height);
            Vector2 size = new Vector2(source.width * scale, source.height * scale);
            if (row && !column) size.y = Mathf.Min(area.height, 42);
            if (column && !row) size.x = Mathf.Min(area.width, 42);
            size *= zoom;
            GUI.BeginGroup(area);
            try
            {
                var rect = new Rect(area.size * 0.5f + pan - size * 0.5f, size);
                if (displayedView == View.Result)
                {
                    if (!checker)
                    {
                        checker = new Texture2D(2, 2, TextureFormat.RGB24, false)
                            { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Point };
                        Color a = new Color(0.31f, 0.31f, 0.31f), b = new Color(0.43f, 0.43f, 0.43f);
                        checker.SetPixels(new[] { a, b, b, a });
                        checker.Apply();
                    }
                    GUI.DrawTextureWithTexCoords(rect, checker, new Rect(0, 0, size.x / 16, size.y / 16), false);
                }
                var uv = tiled ? new Rect(offset.x, offset.y, column ? 1 : 3, row ? 1 : 3) : new Rect(0, 0, 1, 1);
                GUI.DrawTextureWithTexCoords(rect, channel ? channel : source, uv, displayedView == View.Result);
            }
            finally { GUI.EndGroup(); }
        }

        void Fit() { pan = Vector2.zero; zoom = 1; }

        void Navigate(Rect area)
        {
            int id = GUIUtility.GetControlID(FocusType.Passive);
            var e = Event.current;
            switch (e.GetTypeForControl(id))
            {
                case EventType.ScrollWheel when area.Contains(e.mousePosition):
                    float next = Mathf.Clamp(zoom * Mathf.Pow(1.12f, -e.delta.y), 0.25f, 32);
                    Vector2 anchor = e.mousePosition - area.center;
                    pan = anchor + (pan - anchor) * (next / zoom);
                    zoom = next;
                    e.Use();
                    break;
                case EventType.MouseDown when area.Contains(e.mousePosition) && (e.button == 0 || e.button == 2):
                    if (e.clickCount == 2) Fit();
                    else GUIUtility.hotControl = id;
                    e.Use();
                    break;
                case EventType.MouseDrag when GUIUtility.hotControl == id:
                    pan += e.delta;
                    e.Use();
                    break;
                case EventType.MouseUp when GUIUtility.hotControl == id:
                    GUIUtility.hotControl = 0;
                    e.Use();
                    break;
            }
        }

        public void Dispose()
        {
            Object.DestroyImmediate(channel);
            Object.DestroyImmediate(source);
            Object.DestroyImmediate(checker);
        }
    }
}
