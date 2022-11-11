using XORFilter.Net.Hashing;

namespace XORFilter.Net
{
    /// <summary>
    /// Uses L = 32 bits to assign the table slots. Probability of error ε = 2.3283064e-10%
    /// </summary>
    public sealed class XorFilter32 : BaseXorFilter<uint>
    {
        protected override uint FingerPrint(byte[] data) => Crc32.Hash(data);
    }
}
