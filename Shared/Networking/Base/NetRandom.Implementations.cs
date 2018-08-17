namespace Lidgren.Network
{
    /// <summary>
    /// Multiply With Carry random
    /// </summary>
    public class MWCRandom : NetRandom
    {
        /// <summary>
        /// Get global instance of MWCRandom
        /// </summary>
        public static new readonly MWCRandom Instance = new MWCRandom();

        private uint m_w, m_z;

        /// <summary>
        /// Constructor with randomized seed
        /// </summary>
        public MWCRandom()
        {
            Initialize(NetRandomSeed.GetUInt64());
        }

        /// <summary>
        /// (Re)initialize this instance with provided 32 bit seed
        /// </summary>
        public override void Initialize(uint seed)
        {
            m_w = seed;
            m_z = seed * 16777619;
        }

        /// <summary>
        /// (Re)initialize this instance with provided 64 bit seed
        /// </summary>
        public void Initialize(ulong seed)
        {
            m_w = (uint)seed;
            m_z = (uint)(seed >> 32);
        }

        /// <summary>
        /// Generates a random value from UInt32.MinValue to UInt32.MaxValue, inclusively
        /// </summary>
        public override uint NextUInt32()
        {
            m_z = 36969 * (m_z & 65535) + (m_z >> 16);
            m_w = 18000 * (m_w & 65535) + (m_w >> 16);
            return ((m_z << 16) + m_w);
        }
    }
}