using XORFilter.Net.Hashing;

namespace XORFilter.Net
{
    /// <summary>
    /// Uses L = 16 bits to assign the table slots. Probability of error ε = 0.00001525878%
    /// </summary>
    public sealed class XorFilter16 : BaseXorFilter<ushort>
    {
        /// <summary>
        /// Generates an XOR filter using L = 16 bits. Provides adequate safety against collisions.
        /// </summary>
        /// <param name="values">A collection of byte arrays that will be fingerprinted to generate the XOR filter.</param>
        public XorFilter16(Span<byte[]> values) : base(values)
        {
        }

        protected override ushort FingerPrint(byte[] data)
        {
            return (ushort)(Crc32.Hash(data) % (ushort.MaxValue + 1));
        }
    }
}
