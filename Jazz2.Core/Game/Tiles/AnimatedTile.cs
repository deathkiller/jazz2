using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Game.Tiles
{
    public class AnimatedTiles
    {
        private RawList<LayerTile> animationTiles;
        private int fps;
        private int delay;
        //private int delayJitter;
        private bool pingPong;
        private int pingPongDelay;
        private int currentTileIdx;
        private bool forwards;

        private float frameDuration;
        private float framesLeft;

        public int this[int index] => animationTiles[index].TileID;

        public LayerTile CurrentTile => animationTiles[currentTileIdx];

        public int Length => animationTiles.Count;

        public AnimatedTiles(TileSet tileset, ushort[] tileIDs, byte[] tileFlags, int fps, int delay, int delayJitter, bool pingPong, int pingPongDelay)
        {
            this.fps = fps;
            this.delay = delay;
            // ToDo: DelayJitter is not used...
            //this.delayJitter = delayJitter;
            this.pingPong = pingPong;
            this.pingPongDelay = pingPongDelay;

            animationTiles = new RawList<LayerTile>();

            for (int i = 0; i < tileIDs.Length; i++) {
                ushort tidx = tileIDs[i];

                LayerTile pseudotile = new LayerTile();
                pseudotile.Material = tileset.Material;
                pseudotile.MaterialOffset = new Point2(
                    (tidx % tileset.TilesPerRow) * tileset.TileSize,
                    (tidx / tileset.TilesPerRow) * tileset.TileSize
                );

                if ((tileFlags[i] & 0x80) > 0) {
                    pseudotile.MaterialAlpha = 127;
                } else {
                    pseudotile.MaterialAlpha = 255;
                }

                pseudotile.SuspendType = SuspendType.None;
                pseudotile.TileID = tidx;

                animationTiles.Add(pseudotile);
            }

            if (fps > 0) {
                frameDuration = 70f / fps;
                framesLeft = frameDuration;
            }
        }

        public void UpdateTile(float timeMult)
        {
            if (fps == 0 || animationTiles.Count < 2) {
                return;
            }

            framesLeft -= timeMult;
            if (framesLeft >= 0f) {
                return;
            }

            if (forwards) {
                if (currentTileIdx == animationTiles.Count - 1) {
                    if (pingPong) {
                        forwards = false;
                        framesLeft += (frameDuration * (1 + pingPongDelay));
                    } else {
                        currentTileIdx = 0;
                        framesLeft += (frameDuration * (1 + delay));
                    }
                } else {
                    currentTileIdx += 1;
                    framesLeft += frameDuration;
                }
            } else {
                if (currentTileIdx == 0) {
                    // Reverse only occurs on ping pong mode so no need to check for that here
                    forwards = true;
                    framesLeft += (frameDuration * (1 + delay));
                } else {
                    currentTileIdx -= 1;
                    framesLeft += frameDuration;
                }
            }
        }
    }
}