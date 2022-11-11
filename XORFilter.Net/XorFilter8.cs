using XORFilter.Net.Hashing;

namespace XORFilter.Net
{
    /// <summary>
    /// Uses L = 8 bits to assign the table slots. Probability of error ε = 0.00390625%
    /// </summary>
    public sealed class XorFilter8 : BaseXorFilter<byte>
    {
        protected override byte FingerPrint(byte[] data)
        {
            return (byte)(Crc32.Hash(data) % (byte.MaxValue + 1));
        }
    }
}