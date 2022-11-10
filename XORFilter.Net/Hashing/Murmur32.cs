using HashDepot;

namespace XORFilter.Net.Hashing
{
    public static class Murmur32
    {
        public static uint Hash(byte[] input, uint seed) => MurmurHash3.Hash32(input, seed);
    }
}