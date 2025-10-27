namespace XORFilter.Net.Hashing
{
    internal static class Crc32
    {
        internal static uint Hash(byte[] input) => BitConverter.ToUInt32(System.IO.Hashing.Crc32.Hash(input), 0);

        internal static uint Hash(ReadOnlySpan<byte> input) => System.IO.Hashing.Crc32.HashToUInt32(input);
    }
}