namespace Lidgren.Network
{
    public partial class NetBuffer
    {
        /// <summary>
        /// Number of bytes to overallocate for each message to avoid resizing
        /// </summary>
        protected const int c_overAllocateAmount = 4;

        internal byte[] m_data;
        internal int m_bitLength;
        internal int m_readPosition;

        /// <summary>
        /// Gets or sets the internal data buffer
        /// </summary>
        public byte[] Data
        {
            get { return m_data; }
            set { m_data = value; }
        }

        /// <summary>
        /// Gets or sets the length of the used portion of the buffer in bytes
        /// </summary>
        public int LengthBytes
        {
            get { return ((m_bitLength + 7) >> 3); }
            set
            {
                m_bitLength = value * 8;
                InternalEnsureBufferSize(m_bitLength);
            }
        }

        /// <summary>
        /// Gets or sets the length of the used portion of the buffer in bits
        /// </summary>
        public int LengthBits
        {
            get { return m_bitLength; }
            set
            {
                m_bitLength = value;
                InternalEnsureBufferSize(m_bitLength);
            }
        }

        /// <summary>
        /// Gets or sets the read position in the buffer, in bits (not bytes)
        /// </summary>
        public long Position
        {
            get { return (long)m_readPosition; }
            set { m_readPosition = (int)value; }
        }

        /// <summary>
        /// Gets the position in the buffer in bytes; note that the bits of the first returned byte may already have been read - check the Position property to make sure.
        /// </summary>
        public int PositionInBytes
        {
            get { return (int)(m_readPosition / 8); }
        }
    }
}