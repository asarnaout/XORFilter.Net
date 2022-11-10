using XORFilter.Net.Hashing;

namespace XORFilter.Net
{
    public sealed class XorFilter16 : BaseXorFilter<ushort>
    {
        protected override ushort FingerPrint(byte[] data)
        {
            return (ushort)(Crc32.Hash(data) % (ushort.MaxValue + 1));
        }
    }
}
