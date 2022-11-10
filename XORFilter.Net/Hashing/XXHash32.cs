using HashDepot;

namespace XORFilter.Net.Hashing
{
    public static class XXHash32
    {
        public static uint Hash(byte[] input, uint seed) => XXHash.Hash32(input, seed);
    }
}
