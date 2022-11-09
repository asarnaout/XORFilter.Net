namespace XORFilter.Net.Hashing
{
    public static class Crc32
    {
        public static uint Hash(byte[] input) => BitConverter.ToUInt32(System.IO.Hashing.Crc32.Hash(input), 0);
    }
}