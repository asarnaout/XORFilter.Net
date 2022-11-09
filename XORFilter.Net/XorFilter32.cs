using XORFilter.Net.Hashing;

namespace XORFilter.Net
{
    public sealed class XorFilter32 : BaseXorFilter<uint>
    {
        protected override uint FingerPrint(byte[] data)
        {
            return Crc32.Hash(data) % uint.MaxValue; //TODO: Figure out a workaround: uint.MaxValue + 1 will overflow, so the +1 was omitted
        }

        protected override uint Xor(uint val1, uint val2)
        {
            return val1 ^ val2;
        }
    }
}
