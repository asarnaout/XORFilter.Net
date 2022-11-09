using System.Numerics;
using XORFilter.Net.Hashing;

namespace XORFilter.Net
{
    public abstract class BaseXorFilter<T> where T : INumber<T>
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

        protected abstract T Xor(T val1, T val2);

        protected abstract T FingerPrint(byte[] data);

        public void Generate(Span<byte[]> values)
        {
            var tableSize = (int)Math.Ceiling(values.Length * _slotsMultiplier);
            _tableSlots = new T[tableSize];

            //TODO: Handle this more elegantly and provide means to swap hash functions
            _hashingFunctions = new Func<byte[], int>[]
            {
                x => (int)(Adler32.Hash(x) % tableSize),
                x => (int)(Fnv1A32.Hash(x) % tableSize),
                x => (int)(Murmur32.Hash(x) % tableSize)
            };

            var peelingOrder = new int[values.Length];
            Array.Fill(peelingOrder, -1);

            Peel(tableSize, values, peelingOrder);
            FillTableSlots(tableSize, values, peelingOrder);
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
                xorResult = Xor(xorResult, _tableSlots[_hashingFunctions[i](value)]);
            }

            return FingerPrint(value) == xorResult;
        }

        private void Peel(int tableSize, Span<byte[]> values, int[] peelingOrder)
        {
            var counters = new byte[tableSize];

            for (var i = 0; i < values.Length; i++)
            {
                for (var j = 0; j <= _hashingFunctions.Length - 1; j++)
                {
                    counters[_hashingFunctions[j](values[i])]++;
                }
            }

            for (var peelingCounter = 0; peelingCounter < values.Length; peelingCounter++)
            {
                bool peelable = false;

                for (var i = 0; i < tableSize; i++)
                {
                    if (counters[i] != 1) //Peel slots with only one hash referencing them
                    {
                        continue;
                    }

                    for (var j = 0; j < values.Length; j++)
                    {
                        int h0 = _hashingFunctions[0](values[j]), h1 = _hashingFunctions[1](values[j]), h2 = _hashingFunctions[2](values[j]);

                        if (i == h0 || i == h1 || i == h2)
                        {
                            var previouslyPeeled = false;
                            for (var k = 0; k < peelingCounter; k++)
                            {
                                if (peelingOrder[k] == j)
                                {
                                    previouslyPeeled = true;
                                    break;
                                }
                            }

                            if (previouslyPeeled) continue;

                            peelingOrder[peelingCounter] = j;
                            counters[h0]--;
                            counters[h1]--;
                            counters[h2]--;
                            break;
                        }
                    }
                    peelable = true;
                    break;
                }

                if (!peelable)
                {
                    throw new NotImplementedException("Need to swap hashes here"); //TODO: NEED TO SWAP HASHES HERE
                }
            }
        }

        private void FillTableSlots(int tableSize, Span<byte[]> values, int[] peelingOrder)
        {
            var assigned = new bool[tableSize];

            for (var i = values.Length - 1; i >= 0; i--)
            {
                var value = values[peelingOrder[i]];

                int h0 = _hashingFunctions[0](value),
                    h1 = _hashingFunctions[1](value),
                    h2 = _hashingFunctions[2](value);

                if (TryApplySlotValue(h0, h1, h2, assigned, value))
                {
                    continue;
                }

                if (TryApplySlotValue(h1, h0, h2, assigned, value))
                {
                    continue;
                }

                TryApplySlotValue(h2, h0, h1, assigned, value);
            }
        }

        private bool TryApplySlotValue(int currentHash, int altHashA, int altHashB, bool[] assignedValues, byte[] value)
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

            if (_tableSlots[currentHash] == default && !assignedValues[currentHash] && ((currentHash == altHashA && currentHash == altHashB) || (currentHash != altHashA && currentHash != altHashB)))
            {
                _tableSlots[currentHash] = Xor(Xor(_tableSlots[altHashA], _tableSlots[altHashB]), FingerPrint(value));
                assignedValues[currentHash] = true;
                assignedValues[altHashA] = true;
                assignedValues[altHashB] = true;

                return true;
            }

            return false;
        }
    }
}