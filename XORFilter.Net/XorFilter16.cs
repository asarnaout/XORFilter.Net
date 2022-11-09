using XORFilter.Net.Hashing;

namespace XORFilter.Net
{
    public sealed class XorFilter16 : BaseXorFilter<ushort>
    {
        protected override ushort FingerPrint(byte[] data)
        {
            return (ushort)(Crc32.Hash(data) % (ushort.MaxValue + 1));
        }

        protected override ushort Xor(ushort val1, ushort val2)
        {
            return (ushort)(val1 ^ val2);
        }
    }
}
