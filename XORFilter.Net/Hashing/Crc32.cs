namespace XORFilter.Net.Hashing
{
    internal static class Crc32
    {
        internal static uint Hash(byte[] input) => Hash(input.AsSpan());

        internal static uint Hash(ReadOnlySpan<byte> input) => System.IO.Hashing.Crc32.HashToUInt32(input);
    }
}