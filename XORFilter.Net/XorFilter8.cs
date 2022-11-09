using XORFilter.Net.Hashing;

namespace XORFilter.Net
{
    public sealed class XorFilter8 : BaseXorFilter<byte>
    {
        protected override byte FingerPrint(byte[] data)
        {
            return (byte)(Crc32.Hash(data) % (byte.MaxValue + 1));
        }

        protected override byte Xor(byte val1, byte val2)
        {
            return (byte)(val1 ^ val2);
        }
    }
}
