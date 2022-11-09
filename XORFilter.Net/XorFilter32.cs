using XORFilter.Net.Hashing;

namespace XORFilter.Net
{
    public sealed class XorFilter32 : BaseXorFilter<uint>
    {
        protected override uint FingerPrint(byte[] data) => Crc32.Hash(data);

        protected override uint Xor(uint val1, uint val2)
        {
            return val1 ^ val2;
        }
    }
}
