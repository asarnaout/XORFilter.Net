using XORFilter.Net.Hashing;

namespace XORFilter.Net
{
    /// <summary>
    /// Uses L = 16 bits to assign the table slots. Probability of error ε = 0.00001525878%
    /// </summary>
    public sealed class XorFilter16 : BaseXorFilter<ushort>
    {
        protected override ushort FingerPrint(byte[] data)
        {
            return (ushort)(Crc32.Hash(data) % (ushort.MaxValue + 1));
        }
    }
}
