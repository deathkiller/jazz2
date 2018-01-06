using Duality;
using Jazz2.Game.Structs;

namespace Jazz2.Game.Tiles
{
    public class AnimatedTile
    {
        private RawList<LayerTile> tiles;
        private int frameRate;
        private int delay;
        //private int delayJitter;
        private bool pingPong;
        private int pingPongDelay;
        private int currentTileIdx;
        private bool forwards;

        private float frameDuration;
        private float framesLeft;

        public LayerTile CurrentTile => tiles[currentTileIdx];

        public LayerTile[] Tiles => tiles.Data;

        public int Length => tiles.Count;

        public AnimatedTile(TileSet tileset, ushort[] tileIDs, byte[] tileFlags, int fps, int delay, int delayJitter, bool pingPong, int pingPongDelay)
        {
            this.frameRate = fps;
            this.delay = delay;
            // ToDo: DelayJitter is not used...
            //this.delayJitter = delayJitter;
            this.pingPong = pingPong;
            this.pingPongDelay = pingPongDelay;

            tiles = new RawList<LayerTile>();

            for (int i = 0; i < tileIDs.Length; i++) {
                LayerTile tile = tileset.GetDefaultTile(tileIDs[i]);

                byte tileModifier = (byte)(tileFlags[i] >> 4);
                if (tileModifier == 1 /*Translucent*/) {
                    tile.MaterialAlpha = /*127*/140;
                } else if (tileModifier == 2 /*Invisible*/) {
                    tile.MaterialAlpha = 0;
                } else {
                    tile.MaterialAlpha = 255;
                }

                tile.SuspendType = SuspendType.None;

                tiles.Add(tile);
            }

            if (fps > 0) {
                frameDuration = 70f / fps;
                framesLeft = frameDuration;
            }
        }

        public void UpdateTile(float timeMult)
        {
            if (frameRate == 0 || tiles.Count < 2) {
                return;
            }

            framesLeft -= timeMult;
            if (framesLeft >= 0f) {
                return;
            }

            if (forwards) {
                if (currentTileIdx == tiles.Count - 1) {
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