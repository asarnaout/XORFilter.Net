using HashDepot;
using System.Numerics;

namespace XORFilter.Net
{
    public abstract class BaseXorFilter<T> where T : INumber<T>, IBitwiseOperators<T, T, T>
    {
        private readonly T[] _tableSlots = default!;
        
        private int[,] _hashesPerValue = default!;

        private Func<byte[], int>[] _hashingFunctions = default!;

        private readonly Random _random = new ();

        protected BaseXorFilter(Span<byte[]> values)
        {
            if (values is [])
            {
                throw new ArgumentException("Values array should be provided to generate the XOR Filter.");
            }

            var tableSize = (int)Math.Ceiling(values.Length * 1.23d);

            _tableSlots = new T[tableSize];

            Stack<int> peelingOrder;

            do
            {
                InitializeHashFunctions(tableSize);
                GenerateHashes(values);

            } while (!TryPeel(values, out peelingOrder));

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
            var xorResult = T.Zero;

            for (var i = 0; i < _hashingFunctions.Length; i++)
            {
                xorResult ^= _tableSlots[_hashingFunctions[i](value)];
            }

            return FingerPrint(value) == xorResult;
        }

        protected abstract T FingerPrint(byte[] data);

        private void InitializeHashFunctions(int tableSize)
        {
            var seed0 = GenerateSeed();
            var seed1 = GenerateSeed();
            var seed2 = GenerateSeed();

            _hashingFunctions = 
            [
                x => (int)(MurmurHash3.Hash32(x, seed0) % tableSize),
                x => (int)(MurmurHash3.Hash32(x, seed1) % tableSize),
                x => (int)(MurmurHash3.Hash32(x, seed2) % tableSize),
            ];

            uint GenerateSeed() => (((uint)_random.Next(1 << 30)) << 2) | ((uint)_random.Next(1 << 2));
        }

        private void GenerateHashes(Span<byte[]> values)
        {
            _hashesPerValue = new int[values.Length, 3];

            for (var i = 0; i < values.Length; i++)
            {
                for(var j = 0; j < _hashingFunctions.Length; j++)
                {
                    _hashesPerValue[i, j] = _hashingFunctions[j](values[i]);
                }
            }
        }

        private bool TryPeel(Span<byte[]> values, out Stack<int> peelingOrder)
        {
            var mapping = GetHashMapping(values.Length); //An array of arrays tracking which values reference each slot in _tableSlots

            var loneSlots = GetLoneSlots(mapping);

            peelingOrder = new Stack<int>();

            while (loneSlots.TryDequeue(out var loneIndex))
            {
                if (mapping[loneIndex].Count != 1) continue;

                var referencingSlot = mapping[loneIndex][0];
                peelingOrder.Push(referencingSlot);

                for (var j = 0; j < _hashingFunctions.Length; j++)
                {
                    var hashPosition = _hashesPerValue[referencingSlot, j];

                    mapping[hashPosition].Remove(referencingSlot);

                    if (mapping[hashPosition].Count == 1)
                    {
                        loneSlots.Enqueue(hashPosition);
                    }
                }
            }

            return peelingOrder.Count == values.Length;

            IList<int>[] GetHashMapping(int size)
            {
                var mapping = new List<int>[_tableSlots.Length];

                for (var i = 0; i < size; i++)
                {
                    for (var j = 0; j < _hashingFunctions.Length ; j++)
                    {
                        var hashPosition = _hashesPerValue[i, j];
                        mapping[hashPosition] ??= [];
                        mapping[hashPosition].Add(i);
                    }
                }

                return mapping;
            }

            Queue<int> GetLoneSlots(IEnumerable<int>[] mappings)
            {
                var result = new Queue<int>();

                for (var i = 0; i < mappings.Length; i++)
                {
                    if (mappings[i] is not null && mappings[i].Count() == 1)
                    {
                        result.Enqueue(i);
                    }
                }

                return result;
            }
        }

        private void FillTableSlots(Span<byte[]> values, Stack<int> peelingOrder)
        {
            var assignedValues = new HashSet<int>();

            while (peelingOrder.TryPop(out var slotIndex))
            {
                var value = values[slotIndex];

                int h0 = _hashesPerValue[slotIndex, 0],
                    h1 = _hashesPerValue[slotIndex, 1],
                    h2 = _hashesPerValue[slotIndex, 2];

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

            _hashesPerValue = default!;

            bool TryApplySlotValue(int currentHash, int altHashA, int altHashB, HashSet<int> assignedValues, byte[] value)
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
}