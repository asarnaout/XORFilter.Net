using HashDepot;

namespace XORFilter.Net.Hashing
{
    public static class Fnv1A32
    {
        public static uint Hash(byte[] input) => Fnv1a.Hash32(input);
    }
}
