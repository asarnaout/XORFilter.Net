using XORFilter.Net.Hashing;

namespace XORFilter.Net
{
    public sealed class XorFilter64 : BaseXorFilter<ulong>
    {
        protected override ulong FingerPrint(byte[] data)
        {
            return Crc32.Hash(data) % ulong.MaxValue; //TODO: Figure out a workaround: ulong.MaxValue + 1 will overflow, so the +1 was omitted
        }

        protected override ulong Xor(ulong val1, ulong val2)
        {
            return val1 ^ val2;
        }
    }
}