using HashDepot;
using System.Numerics;
using XORFilter.Net.Hashing;

namespace XORFilter.Net
{
    public abstract class BaseXorFilter<T> where T : INumber<T>, IBitwiseOperators<T, T, T>
    {
        private T[] _tableSlots = default!;

        private Func<byte[], int>[] _hashingFunctions = default!;

        private static readonly Random _random = new ();

        protected abstract T FingerPrint(byte[] data);

        /// <summary>
        /// Generates the xor filter values.
        /// </summary>
        public void Generate(Span<byte[]> values)
        {
            var tableSize = (int)Math.Ceiling(values.Length * 1.23d);
            _tableSlots = new T[tableSize];

            var peelingOrder = new int[values.Length];

            do
            {
                InitializeHashFunctions(tableSize);

            } while (!Peel(tableSize, values, peelingOrder));

            FillTableSlots(values, peelingOrder);
        }

        /// <summary>
        /// Checks whether the byte array value has been previously hashed into the xor filter. Note that there is a possible degree of error that could happen
        /// based on which filter was chosen (8 vs 16 vs 32).
        /// </summary>
        /// <param name="value">The array of bytes that will be checked for membership</param>
        /// <returns>True if the value was previously added or if there is a collision.</returns>
        public bool IsMember(byte[] value)
        {
            if (_tableSlots is null || _tableSlots is [] || _hashingFunctions is null || _hashingFunctions is [])
            {
                return false;
            }

            var xorResult = T.Zero;

            for (var i = 0; i < _hashingFunctions.Length; i++)
            {
                xorResult ^= _tableSlots[_hashingFunctions[i](value)];
            }

            return FingerPrint(value) == xorResult;
        }

        private void InitializeHashFunctions(int tableSize)
        {
            var seed0 = GenerateSeed();
            var seed1 = GenerateSeed();

            _hashingFunctions = new Func<byte[], int>[]
            {
                x => (int)(XXHash.Hash32(x, seed0) % tableSize),
                x => (int)(MurmurHash3.Hash32(x, seed1) % tableSize),
                x => (int)(Fnv1a.Hash32(x) % tableSize)
            };

            static uint GenerateSeed() => (((uint)_random.Next(1 << 30)) << 2) | ((uint)_random.Next(1 << 2));
        }

        private bool Peel(int tableSize, Span<byte[]> values, int[] peelingOrder)
        {
            var mapping = GetHashMapping(values);

            for (var peelingCounter = 0; peelingCounter < values.Length; peelingCounter++)
            {
                var peelable = false;

                for (var i = 0; i < tableSize && !peelable; i++)
                {
                    peelable = mapping[i] is not null && mapping[i].Count == 1;

                    if (!peelable)
                    {
                        continue;
                    }

                    var referencingIndex = mapping[i][0];
                    peelingOrder[peelingCounter] = referencingIndex;

                    for(int j = 0; j < _hashingFunctions.Length; j++)
                    {
                        var hashPosition = _hashingFunctions[j](values[referencingIndex]);
                        mapping[hashPosition].Remove(referencingIndex);
                    }
                }

                if (!peelable)
                {
                    return false;
                }
            }

            return true;

            IList<int>[] GetHashMapping(Span<byte[]> values)
            {
                var mapping = new List<int>[tableSize];

                for (var i = 0; i < values.Length; i++)
                {
                    for (var j = 0; j <= _hashingFunctions.Length - 1; j++)
                    {
                        var hashPosition = _hashingFunctions[j](values[i]);
                        mapping[hashPosition] ??= new List<int>();
                        mapping[hashPosition].Add(i);
                    }
                }

                return mapping;
            }
        }

        private void FillTableSlots(Span<byte[]> values, int[] peelingOrder)
        {
            var assignedValues = new HashSet<int>();

            for (var i = values.Length - 1; i >= 0; i--)
            {
                var value = values[peelingOrder[i]];

                int h0 = _hashingFunctions[0](value),
                    h1 = _hashingFunctions[1](value),
                    h2 = _hashingFunctions[2](value);

                if (TryApplySlotValue(h0, h1, h2, assignedValues, value))
                {
                    continue;
                }

                if (TryApplySlotValue(h1, h0, h2, assignedValues, value))
                {
                    continue;
                }

                TryApplySlotValue(h2, h0, h1, assignedValues, value);
            }
        }

        private bool TryApplySlotValue(int currentHash, int altHashA, int altHashB, HashSet<int> assignedValues, byte[] value)
        {
            if (_tableSlots[currentHash] == default 
                && !assignedValues.Contains(currentHash) 
                && ((currentHash == altHashA && currentHash == altHashB) || (currentHash != altHashA && currentHash != altHashB)))
            {
                _tableSlots[currentHash] = _tableSlots[altHashA] ^ _tableSlots[altHashB] ^ FingerPrint(value);
                assignedValues.Add(currentHash);
                assignedValues.Add(altHashA);
                assignedValues.Add(altHashB);

                return true;
            }

            return false;
        }
    }
}