using XORFilter.Net.Hashing;

namespace XORFilter.Net
{
    public sealed class XorFilter8 : BaseXorFilter<byte>
    {
        protected override byte FingerPrint(byte[] data)
        {
            return (byte)(Crc32.Hash(data) % (byte.MaxValue + 1));
        }
    }
}