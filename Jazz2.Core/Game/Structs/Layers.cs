using Duality;
using Duality.Drawing;
using Duality.Resources;

namespace Jazz2.Game.Structs
{
    public enum LayerType
    {
        Sprite,
        Sky,
        Other
    }

    public enum BackgroundStyle
    {
        Plain,
        Sky,
        Circle
    }

    public enum TileDestructType
    {
        None,
        Weapon,
        Speed,
        Collapse,
        Special,
        Trigger
    }

    public enum SuspendType
    {
        None,
        Vine,
        Hook
    }

    public struct LayerTile
    {
        public int TileID;

        public ContentRef<Material> Material;
        public Point2 MaterialOffset;
        public byte MaterialAlpha;

        public bool IsFlippedX;
        public bool IsFlippedY;
        public bool IsAnimated;

        // Collision affecting modifiers
        public bool IsOneWay;
        public SuspendType SuspendType;
        public TileDestructType DestructType;
        public int DestructAnimation;   // Animation index for a destructible tile that uses an animation, but doesn't animate normally
        public int DestructFrameIndex;  // Denotes the specific frame from the above animation that is currently active
                                        // Collapsible: delay ("wait" parameter); trigger: trigger id

        // ToDo: I don't know if it's good solution for this
        public uint ExtraData;
        // ToDo: I don't know if it's used at all
        //public bool TilesetDefault;
    }

    public struct TileMapLayer
    {
        public int Index;

        public LayerTile[] Layout;
        public int LayoutWidth;

        public float Depth;
        public float SpeedX;
        public float SpeedY;
        public float AutoSpeedX;
        public float AutoSpeedY;
        public bool RepeatX;
        public bool RepeatY;

        public float OffsetX;
        public float OffsetY;

        // JJ2's "limit visible area" flag
        public bool UseInherentOffset;

        public BackgroundStyle BackgroundStyle;
        public ColorRgba BackgroundColor;
        public bool UseStarsTextured;
    }
}