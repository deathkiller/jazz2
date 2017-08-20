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

        public bool AsyncFinalizingRequired;
    }

    public class GenericGraphicResource
    {
        public bool Referenced;
        public GenericGraphicResourceAsyncFinalize AsyncFinalize;

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
        public GenericGraphicResource Base;
        public GraphicResourceAsyncFinalize AsyncFinalize;

        public HashSet<AnimState> State;
        public ContentRef<Material> Material;
        public float FrameDuration;
        public int FrameCount;
        public int FrameOffset;
        public bool OnlyOnce;

        public static GraphicResource From(GenericGraphicResource resBase, ContentRef<DrawTechnique> drawTechnique, ColorRgba color, bool isIndexed, ContentRef<Texture> paletteTexture)
        {
            GraphicResource res = new GraphicResource();
            res.FrameDuration = resBase.FrameDuration;
            res.FrameCount = resBase.FrameCount;
            res.Base = resBase;

            Dictionary<string, ContentRef<Texture>> textures = new Dictionary<string, ContentRef<Texture>>();
            textures.Add("mainTex", resBase.Texture);
            if (resBase.TextureNormal != null) {
                textures.Add("normalTex", resBase.TextureNormal);
            }

            if (isIndexed) {
                textures.Add("paletteTex", paletteTexture);
            }

            res.Material = new Material(drawTechnique, color, textures);

            return res;
        }

        public static GraphicResource From(GenericGraphicResource resBase, string shader, ColorRgba color, bool isIndexed)
        {
            GraphicResource res = new GraphicResource();
            res.FrameDuration = resBase.FrameDuration;
            res.FrameCount = resBase.FrameCount;
            res.Base = resBase;

            res.AsyncFinalize = new GraphicResourceAsyncFinalize {
                Shader = shader,
                Color = color,
                BindPaletteToMaterial = isIndexed
            };

            return res;
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

    public class GenericGraphicResourceAsyncFinalize
    {
        public Pixmap TextureMap;
        public Pixmap TextureNormalMap;
        public TextureWrapMode TextureWrap;
        public bool LinearSampling;
    }

    public class GraphicResourceAsyncFinalize
    {
        public string Shader;
        public ColorRgba Color;
        public bool BindPaletteToMaterial;
    }
}