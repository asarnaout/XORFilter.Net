namespace XORFilter.Net.Hashing
{
    public static class Adler32
    {
        private const uint Mod = 65521;

        public static uint Hash(byte[] input)
        {
            uint a = 1, b = 0;
            foreach (var block in input)
            {
                a += block;
                b += a;
            }

            return (b % Mod << 16) + a % Mod;
        }
    }
}