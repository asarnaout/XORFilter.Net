using XORFilter.Net.Hashing;

namespace XORFilter.Net
{
    /// <summary>
    /// Uses L = 32 bits to assign the table slots. Probability of error ε ≈ 2.3283064e-8%
    /// </summary>
    public sealed class XorFilter32 : BaseXorFilter<uint>
    {
        private XorFilter32(Span<byte[]> values) : base(values)
        {
        }

        /// <summary>
        /// Generates an XOR filter using L = 32 bits.
        /// </summary>
        /// <param name="values">A collection of byte arrays that will be used to generate the XOR filter.</param>
        public static XorFilter32 BuildFrom(Span<byte[]> values)
        {
            return new XorFilter32(values);
        }

        protected override uint FingerPrint(byte[] data) => Crc32.Hash(data);
    }
}