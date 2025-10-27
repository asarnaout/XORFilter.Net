using XORFilter.Net.Hashing;

namespace XORFilter.Net
{
    /// <summary>
    /// Uses L = 8 bits to assign the table slots. Probability of error ε ≈ 0.390625%
    /// </summary>
    public class XorFilter8 : BaseXorFilter<byte>
    {
        private XorFilter8(Span<byte[]> values, int? seed = null) : base(values, seed)
        {
        }

        /// <summary>
        /// Generates an XOR filter using L = 8 bits.
        /// </summary>
        /// <param name="values">A collection of byte arrays that will be used to generate the XOR filter.</param>
        /// <param name="seed">Optional random seed for deterministic filter generation.</param>
        public static XorFilter8 BuildFrom(Span<byte[]> values, int? seed = null)
        {
            return new XorFilter8(values, seed);
        }

        protected override byte FingerPrint(byte[] data) => FingerPrint(data.AsSpan());

        protected override byte FingerPrint(ReadOnlySpan<byte> data)
        {
            return (byte)(Crc32.Hash(data) % (byte.MaxValue + 1));
        }
    }
}