namespace Jazz2.Networking
{
    public static class SpecialPacketTypes
    {
        /// <summary>
        /// Sent from client as unconnected message, then sent back from server
        /// </summary>
        public const byte Ping = 1;

        /// <summary>
        /// Sent from server to update state and position of players and objects
        /// </summary>
        public const byte UpdateAllActors = 90;
    }
}
