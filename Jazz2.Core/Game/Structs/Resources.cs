using System.Collections.Generic;
using Duality;
using Duality.Drawing;
using Duality.Resources;

namespace Jazz2.Game.Structs
{
    public class Metadata
    {
        public bool Referenced;

        public Dictionary<string, GraphicResource> Graphics;
        public Dictionary<string, SoundResource> Sounds;

        public Point2 BoundingBox;
    }

    public class GenericGraphicResource
    {
        public bool Referenced;

        public ContentRef<Texture> Texture;
        public ContentRef<Texture> TextureNormal;
        public Point2 FrameDimensions;
        public Point2 FrameConfiguration;
        public float FrameDuration;
        public int FrameCount;
        public Point2 Hotspot;
        public Point2 Coldspot;
        public Point2 Gunspot;
        public bool HasColdspot;
        public bool HasGunspot;
    }

    public class GraphicResource
    {
        public bool Referenced;

        public HashSet<AnimState> State;
        public ContentRef<Material> Material;
        public Point2 FrameDimensions;
        public Point2 FrameConfiguration;
        public float FrameDuration;
        public int FrameCount;
        public int FrameOffset;
        public Point2 Hotspot;
        public Point2 Coldspot;
        public Point2 Gunspot;
        public bool HasColdspot;
        public bool HasGunspot;
        public bool OnlyOnce;

        public static GraphicResource From(GenericGraphicResource g, ContentRef<DrawTechnique> drawTechnique, ColorRgba color)
        {
            Dictionary<string, ContentRef<Texture>> textures = new Dictionary<string, ContentRef<Texture>>();
            textures.Add("mainTex", g.Texture);
            if (g.TextureNormal != null) {
                textures.Add("normalTex", g.TextureNormal);
            }

            GraphicResource resource = new GraphicResource();
            resource.Material = new Material(drawTechnique, color, textures);
            resource.FrameDimensions = g.FrameDimensions;
            resource.FrameConfiguration = g.FrameConfiguration;
            resource.FrameDuration = g.FrameDuration;
            resource.FrameCount = g.FrameCount;
            resource.Hotspot = g.Hotspot;
            resource.Coldspot = g.Coldspot;
            resource.Gunspot = g.Gunspot;
            resource.HasColdspot = g.HasColdspot;
            resource.HasGunspot = g.HasGunspot;
            return resource;
        }

        private GraphicResource()
        {
        }
    }

    // ToDo: Refactor sounds
    public class SoundResource
    {
        public ContentRef<Sound> Sound;
    }
}