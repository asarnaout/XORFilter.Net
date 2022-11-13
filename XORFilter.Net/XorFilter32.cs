using XORFilter.Net.Hashing;

namespace XORFilter.Net
{
    /// <summary>
    /// Uses L = 32 bits to assign the table slots. Probability of error ε = 2.3283064e-10%
    /// </summary>
    public sealed class XorFilter32 : BaseXorFilter<uint>
    {
        /// <summary>
        /// Generates an XOR filter using L = 32 bits. Provides maximum safety against collisions.
        /// </summary>
        /// <param name="values">A collection of byte arrays that will be fingerprinted to generate the XOR filter.</param>
        public XorFilter32(Span<byte[]> values) : base(values)
        {
        }

        protected override uint FingerPrint(byte[] data) => Crc32.Hash(data);
    }
}
