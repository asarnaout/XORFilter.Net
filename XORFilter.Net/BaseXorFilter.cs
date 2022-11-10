using System;
using System.Diagnostics.Tracing;
using System.Numerics;
using XORFilter.Net.Hashing;

namespace XORFilter.Net
{
    public abstract class BaseXorFilter<T> where T : INumber<T>, IBitwiseOperators<T, T, T>
    {
        private T[] _tableSlots = default!;

        /// <summary>
        /// To increase the likelihood of the peeling function's success (its ability to find slots to peel) we can control either d (the number of hash
        /// functions used) or m (the number of slots).
        /// 
        /// A small value of d would cause the table to be un-peelable in case any collisions occur.
        /// A large value of d would cause too many items to hash to the same slots making the table un-peelable as well.
        /// 
        /// Increasing m would increase the likelihood of success however it would also increase memory usage.
        /// 
        /// The safest option is to use d = 3 with m = 1.23 x the number of slots.
        /// </summary>
        private const double _slotsMultiplier = 1.23d;

        private Func<byte[], int>[] _hashingFunctions = default!;

        private static readonly Random _random = new ();

        protected abstract T FingerPrint(byte[] data);

        public void Generate(Span<byte[]> values)
        {
            var tableSize = (int)Math.Ceiling(values.Length * _slotsMultiplier);
            _tableSlots = new T[tableSize];

            var peelingOrder = new int[values.Length];

            InitializeHashFunctions(tableSize);

            while(!Peel(tableSize, values, peelingOrder))
            {
                InitializeHashFunctions(tableSize);
            }

            FillTableSlots(values, peelingOrder);
        }

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
                x => (int)(XXHash32.Hash(x, seed0) % tableSize),
                x => (int)(Murmur32.Hash(x, seed1) % tableSize),
                x => (int)(Fnv1A32.Hash(x) % tableSize)
            };

            static uint GenerateSeed() => (((uint)_random.Next(1 << 30)) << 2) | ((uint)_random.Next(1 << 2));
        }

        private bool Peel(int tableSize, Span<byte[]> values, int[] peelingOrder)
        {
            var counters = GetHashingCounters(values);

            var peeledValues = new HashSet<int>();

            for (var peelingCounter = 0; peelingCounter < values.Length; peelingCounter++)
            {
                bool peelable = false;

                for (var i = 0; i < tableSize && !peelable; i++)
                {
                    peelable = counters[i] == 1;

                    if (!peelable) //Peel slots with only one hash referencing them
                    {
                        continue;
                    }

                    for (var valuesIndex = 0; valuesIndex < values.Length; valuesIndex++)
                    {
                        int h0 = _hashingFunctions[0](values[valuesIndex]), 
                            h1 = _hashingFunctions[1](values[valuesIndex]), 
                            h2 = _hashingFunctions[2](values[valuesIndex]);

                        if ((i == h0 || i == h1 || i == h2) && !peeledValues.Contains(valuesIndex))
                        {
                            peelingOrder[peelingCounter] = valuesIndex;
                            counters[h0]--;
                            counters[h1]--;
                            counters[h2]--;

                            peeledValues.Add(valuesIndex);
                            break;
                        }
                    }
                }

                if (!peelable)
                {
                    return false;
                }
            }

            return true;

            ushort[] GetHashingCounters(Span<byte[]> values) //The choice to use ushort was made as it guard against the possibility of more than 255 values hashing to the same slot.
            {
                var counters = new ushort[tableSize];

                for (var i = 0; i < values.Length; i++)
                {
                    for (var j = 0; j <= _hashingFunctions.Length - 1; j++)
                    {
                        counters[_hashingFunctions[j](values[i])]++;
                    }
                }

                return counters;
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
            /*
             * We need to apply the fingerprint to a slot only if:
             * - The slot hasn't been previously used. Since this is an array of unsigned numbers, we're using another collection "assignedValues" to keep track of this.
             * - Either:
             *          1- All 3 hashes point to the same slot, in this case it would be safe to assign the fingerprint directly to the slot since hn ^ hn ^ hn = hn.
             *          2- No other hash points to the same slot, if another hash points to the same slot then hn ^ hn = 0 will yield an inaccurate result.
             *          Ex: For some "val", h0 = 3, h1 = 3, h2 = 5, fingerprint = 7.
             *          tableSlot#3 = 7
             *          tableSlot#5 = 0
             *          
             *          XorFilter(val) = tableSlot#3 ^ tableSlot#3 ^ tableSlot#5 = 0 (Incorrect result).
             *          
             *          In this case tableSlot#5 should be the one assigned the fingerprint.
             */

            if (_tableSlots[currentHash] == default && !assignedValues.Contains(currentHash) && ((currentHash == altHashA && currentHash == altHashB) || (currentHash != altHashA && currentHash != altHashB)))
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