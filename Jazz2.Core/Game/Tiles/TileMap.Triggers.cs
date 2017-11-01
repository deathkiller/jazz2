using Jazz2.Game.Structs;

namespace Jazz2.Game.Tiles
{
    partial class TileMap
    {
        public bool GetTrigger(ushort triggerID)
        {
            return triggerState[triggerID];
        }

        public void SetTrigger(ushort triggerID, bool newState)
        {
            if (triggerState[triggerID] == newState) {
                return;
            }

            triggerState[triggerID] = newState;

            // Go through all tiles and update any that are influenced by this trigger
            int n = levelWidth * levelHeight;
            for (int i = 0; i < n; i++) {
                ref LayerTile tile = ref layers[sprLayerIndex].Layout[i];
                if (tile.DestructType == TileDestructType.Trigger && tile.ExtraData == triggerID) {
                    if (animatedTiles[tile.DestructAnimation].Length > 1) {
                        tile.DestructFrameIndex = (newState ? 1 : 0);
                        tile.TileID = animatedTiles[tile.DestructAnimation][tile.DestructFrameIndex];
                        tile.MaterialOffset = tileset.GetTileTextureRect(tile.TileID);
                    }
                }
            }
        }
    }
}