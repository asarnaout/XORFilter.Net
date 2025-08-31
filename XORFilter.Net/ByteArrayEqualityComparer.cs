using HashDepot;

namespace XORFilter.Net;

public class ByteArrayEqualityComparer : IEqualityComparer<byte[]>
{
    public bool Equals(byte[]? x, byte[]? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x == null || y == null || x.Length != y.Length)
        {
            return false;
        }

        for (int i = 0; i < x.Length; i++)
        {
            if (x[i] != y[i])
            {
                return false;
            }
        }

        return true;
    }

    public int GetHashCode(byte[] obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        var hash = XXHash.Hash64(obj);
        
        return (int)(hash ^ (hash >> 32));
    }
}
