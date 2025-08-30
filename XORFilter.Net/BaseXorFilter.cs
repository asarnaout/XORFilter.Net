using HashDepot;
using System.Numerics;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("XORFilter.Net.Tests")]

namespace XORFilter.Net;

public abstract class BaseXorFilter<T> where T : INumber<T>, IBitwiseOperators<T, T, T>
{
    private T[] _tableSlots = default!;

    private int[,] _hashesPerValue = default!;

    private Func<byte[], int>[] _hashingFunctions = default!;

    // Internal accessors for testing
    internal T[] TableSlots => _tableSlots;
    internal Func<byte[], int>[] HashingFunctions => _hashingFunctions;
    internal int[,] HashesPerValue => _hashesPerValue;

    protected internal BaseXorFilter(Span<byte[]> values)
    {
        if (values is [])
        {
            throw new ArgumentException("Values array should be provided to generate the XOR Filter.");
        }

        values = ToUniqueByteArray(values);

        // Ensure at least 3 slots so each of the three partitions has non-zero width
        var computedSize = (int)Math.Ceiling(values.Length * 1.23d);
        var currentTableSize = Math.Max(3, computedSize);

        Stack<(int indexToPeel, int loneSlotIndex)> peelingOrder = new();
        const int maxRetries = 1000; // Prevent infinite loops
        const int retriesBeforeResize = 100; // Try resizing after this many failures
        var retryCount = 0;

        while (true)
        {
            _tableSlots = new T[currentTableSize];

            var retriesAtCurrentSize = 0;
            bool peelingSuccessful = false;

            // Try multiple hash functions at current table size
            while (retriesAtCurrentSize < retriesBeforeResize && retryCount < maxRetries)
            {
                InitializeHashFunctions(_tableSlots.Length);
                GenerateHashes(values);

                if (TryPeel(values, out peelingOrder))
                {
                    peelingSuccessful = true;
                    break;
                }

                retriesAtCurrentSize++;
                retryCount++;
            }

            if (peelingSuccessful)
            {
                break;
            }

            if (retryCount >= maxRetries)
            {
                throw new InvalidOperationException(
                    $"Failed to construct XOR filter after {maxRetries} attempts. " +
                    $"Input size: {values.Length}, Final table size: {currentTableSize}. " +
                    "Consider using a different filter type or reducing input size.");
            }

            // Increase table size and try again
            currentTableSize = (int)Math.Ceiling(currentTableSize * 1.15d);
        }

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

    private static Span<byte[]> ToUniqueByteArray(Span<byte[]> values)
    {
        HashSet<byte[]> hashset = new(new ByteArrayEqualityComparer());

        foreach (var val in values)
        {
            hashset.Add(val);
        }

        return hashset.ToArray();
    }

    internal void InitializeHashFunctions(int tableSize)
    {
        var random = new Random();

        uint seed0 = GenerateSeed(random),
             seed1 = GenerateSeed(random),
             seed2 = GenerateSeed(random);

        InitializeHashFunctionsWithSeeds(tableSize, seed0, seed1, seed2);
    }

    internal void InitializeHashFunctionsWithSeeds(int tableSize, uint seed0, uint seed1, uint seed2)
    {
        var partitionSize = tableSize / 3;
        var remainder = tableSize % 3;

        var partition1End = partitionSize + (remainder > 0 ? 1 : 0); //Remainder can be [0, 1, 2]. If the remainder is greater than 0, then always add 1 to this partition.
        var partition2End = partition1End + partitionSize + (remainder > 1 ? 1 : 0); //If remainder is 1, then there is no need to add to this partition, we only need to add another 1 here if the remainder is 2.

        _hashingFunctions = [

            x => GetPartitionedHash(MurmurHash3.Hash32(x, seed0), 0, partition1End),
                x => GetPartitionedHash(MurmurHash3.Hash32(x, seed1), partition1End, partition2End),
                x => GetPartitionedHash(MurmurHash3.Hash32(x, seed2), partition2End, tableSize),
            ];

        int GetPartitionedHash(uint hash, int start, int end) => start + (int)(hash % (end - start));
    }

    private static uint GenerateSeed(Random random) => (((uint)random.Next(1 << 30)) << 2) | ((uint)random.Next(1 << 2));

    internal void GenerateHashes(Span<byte[]> values)
    {
        _hashesPerValue = new int[values.Length, 3];

        for (var i = 0; i < values.Length; i++)
        {
            for (var j = 0; j < _hashingFunctions.Length; j++)
            {
                _hashesPerValue[i, j] = _hashingFunctions[j](values[i]);
            }
        }
    }

    internal bool TryPeel(Span<byte[]> values, out Stack<(int indexToPeel, int loneSlotIndex)> peelingOrder)
    {
        var mapping = GetHashMapping(values.Length); //An array of arrays tracking which values reference each slot in _tableSlots

        var loneSlots = GetLoneSlots(mapping);

        peelingOrder = new Stack<(int indexToPeel, int loneSlotIndex)>();

        while (loneSlots.TryDequeue(out var loneIndex))
        {
            if (mapping[loneIndex].Count != 1) continue;

            var referencingSlot = mapping[loneIndex].First();
            peelingOrder.Push((referencingSlot, loneIndex));

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
    }

    internal HashSet<int>[] GetHashMapping(int size)
    {
        var mapping = new HashSet<int>[_tableSlots.Length];

        for (var i = 0; i < size; i++)
        {
            for (var j = 0; j < _hashingFunctions.Length; j++)
            {
                var hashPosition = _hashesPerValue[i, j];
                mapping[hashPosition] ??= [];
                mapping[hashPosition].Add(i);
            }
        }

        return mapping;
    }

    private static Queue<int> GetLoneSlots(HashSet<int>[] mappings)
    {
        var result = new Queue<int>();

        for (var i = 0; i < mappings.Length; i++)
        {
            if (mappings[i] is not null && mappings[i].Count == 1)
            {
                result.Enqueue(i);
            }
        }

        return result;
    }

    internal void FillTableSlots(Span<byte[]> values, Stack<(int indexToPeel, int loneSlotIndex)> peelingOrder)
    {
        while (peelingOrder.TryPop(out var peeled))
        {
            var (indexToPeel, loneSlotIndex) = peeled;

            var value = values[indexToPeel];

            int h0 = _hashesPerValue[indexToPeel, 0],
                h1 = _hashesPerValue[indexToPeel, 1],
                h2 = _hashesPerValue[indexToPeel, 2];

            int altHashA, altHashB;

            if (loneSlotIndex == h0)
            {
                altHashA = h1;
                altHashB = h2;
            }
            else if (loneSlotIndex == h1)
            {
                altHashA = h0;
                altHashB = h2;
            }
            else
            {
                altHashA = h0;
                altHashB = h1;
            }

            _tableSlots[loneSlotIndex] = _tableSlots[altHashA] ^ _tableSlots[altHashB] ^ FingerPrint(value);
        }

        _hashesPerValue = default!;
    }
}