using XORFilter.Net.Hashing;

namespace XORFilter.Net
{
    /// <summary>
    /// Uses L = 8 bits to assign the table slots. Probability of error ε = 0.00390625%
    /// </summary>
    public sealed class XorFilter8 : BaseXorFilter<byte>
    {
        private XorFilter8(Span<byte[]> values) : base(values)
        {
        }

        /// <summary>
        /// Generates an XOR filter using L = 8 bits.
        /// </summary>
        /// <param name="values">A collection of byte arrays that will be used to generate the XOR filter.</param>
        public static XorFilter8 BuildFrom(Span<byte[]> values)
        {
            return new XorFilter8(values);
        }

        protected override byte FingerPrint(byte[] data)
        {
            return (byte)(Crc32.Hash(data) % (byte.MaxValue + 1));
        }
    }
}