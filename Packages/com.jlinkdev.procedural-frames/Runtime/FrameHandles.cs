using UnityEngine;

namespace JLink.Frames
{
    public enum FrameEditMode { Frame, Loops, Edges, Corners }
    public enum FrameHandle { ArtWidth, ArtHeight, RailWidth, Border }
    public static class FrameHandles
    {
        public static Vector3 Axis(FrameHandle handle)
        {
            switch (handle)
            {
                case FrameHandle.ArtWidth: return Vector3.left;
                case FrameHandle.ArtHeight: return Vector3.down;
                case FrameHandle.RailWidth: return Vector3.right;
                default: return Vector3.up;
            }
        }

        public static Vector3 Position(FrameRecipe r, FrameHandle handle)
        {
            Vector2 half = r.artworkSize*.5f;
            switch (handle)
            {
                case FrameHandle.ArtWidth: return new Vector3(-half.x,0,r.Seat);
                case FrameHandle.ArtHeight: return new Vector3(0,-half.y,r.Seat);
                case FrameHandle.RailWidth:
                    float d=r.edges!=null && r.edges.Active(r) ? r.edges.Displacement(r,1,r.outline==FrameOutline.Oval ? 0 : .5f) : 0;
                    return new Vector3(half.x+r.Border+r.railWidth+d,0,-r.Height(1));
                default: return new Vector3(0,half.y+r.Border,-r.Height(0));
            }
        }

        public static void Pull(FrameRecipe r,FrameHandle handle,float distance)
        {
            switch (handle)
            {
                case FrameHandle.ArtWidth: r.artworkSize.x += 2*distance; break;
                case FrameHandle.ArtHeight: r.artworkSize.y += 2*distance; break;
                case FrameHandle.RailWidth: r.railWidth += distance; break;
                case FrameHandle.Border:
                    if (r.mount == FrameMount.Floater) r.floatGap += distance;
                    else r.matWidth += distance;
                    break;
            }
            r.Sanitize();
        }
    }
}
