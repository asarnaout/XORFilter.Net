using HashDepot;

namespace XORFilter.Net.Hashing
{
    public static class Murmur32
    {
        private static readonly uint _seed = (uint)new Random().Next(int.MaxValue); //TODO: Is this the best way to initialize the seed? Maybe pass the seed from the filter.

        public static uint Hash(byte[] input) => MurmurHash3.Hash32(input, _seed);
    }
}