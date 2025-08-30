using XORFilter.Net.Hashing;

namespace XORFilter.Net
{
    /// <summary>
    /// Uses L = 16 bits to assign the table slots. Probability of error ε ≈ 0.0015258789%
    /// </summary>
    public class XorFilter16 : BaseXorFilter<ushort>
    {
        private XorFilter16(Span<byte[]> values) : base(values)
        {
        }

        /// <summary>
        /// Generates an XOR filter using L = 16 bits.
        /// </summary>
        /// <param name="values">A collection of byte arrays that will be used to generate the XOR filter.</param>
        public static XorFilter16 BuildFrom(Span<byte[]> values)
        {
            return new XorFilter16(values);
        }

        protected override ushort FingerPrint(byte[] data)
        {
            return (ushort)(Crc32.Hash(data) % (ushort.MaxValue + 1));
        }
    }
}
