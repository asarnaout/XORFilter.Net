using XORFilter.Net.Hashing;

namespace XORFilter.Net
{
    /// <summary>
    /// Uses L = 32 bits to assign the table slots. Probability of error ε ≈ 2.3283064e-8%
    /// </summary>
    public class XorFilter32 : BaseXorFilter<uint>
    {
        private XorFilter32(Span<byte[]> values, int? seed = null) : base(values, seed)
        {
        }

        /// <summary>
        /// Generates an XOR filter using L = 32 bits.
        /// </summary>
        /// <param name="values">A collection of byte arrays that will be used to generate the XOR filter.</param>
        /// <param name="seed">Optional random seed for deterministic filter generation.</param>
        public static XorFilter32 BuildFrom(Span<byte[]> values, int? seed = null)
        {
            return new XorFilter32(values, seed);
        }

        protected override uint FingerPrint(byte[] data) => FingerPrint(data.AsSpan());

        protected override uint FingerPrint(ReadOnlySpan<byte> data) => Crc32.Hash(data);
    }
}