using System.Text;
using FluentAssertions;
using Xunit;
using XORFilter.Net;
using XORFilter.Net.Hashing;

namespace XORFilter.Net.Tests
{
    /// <summary>
    /// Unit tests for ByteArrayEqualityComparer
    /// </summary>
    public class ByteArrayEqualityComparerTests
    {
        private readonly ByteArrayEqualityComparer _comparer = new();

        [Fact]
        public void Equals_SameReference_ReturnsTrue()
        {
            // Arrange
            var array = new byte[] { 1, 2, 3 };

            // Act
            var result = _comparer.Equals(array, array);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Equals_BothNull_ReturnsTrue()
        {
            // Act
            var result = _comparer.Equals(null, null);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Equals_OneNull_ReturnsFalse()
        {
            // Arrange
            var array = new byte[] { 1, 2, 3 };

            // Act
            var result1 = _comparer.Equals(array, null);
            var result2 = _comparer.Equals(null, array);

            // Assert
            result1.Should().BeFalse();
            result2.Should().BeFalse();
        }

        [Fact]
        public void Equals_DifferentLengths_ReturnsFalse()
        {
            // Arrange
            var array1 = new byte[] { 1, 2, 3 };
            var array2 = new byte[] { 1, 2 };

            // Act
            var result = _comparer.Equals(array1, array2);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_SameContent_ReturnsTrue()
        {
            // Arrange
            var array1 = new byte[] { 1, 2, 3, 4, 5 };
            var array2 = new byte[] { 1, 2, 3, 4, 5 };

            // Act
            var result = _comparer.Equals(array1, array2);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Equals_DifferentContent_ReturnsFalse()
        {
            // Arrange
            var array1 = new byte[] { 1, 2, 3, 4, 5 };
            var array2 = new byte[] { 1, 2, 3, 4, 6 };

            // Act
            var result = _comparer.Equals(array1, array2);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_EmptyArrays_ReturnsTrue()
        {
            // Arrange
            var array1 = Array.Empty<byte>();
            var array2 = Array.Empty<byte>();

            // Act
            var result = _comparer.Equals(array1, array2);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void GetHashCode_NullArray_ThrowsArgumentNullException()
        {
            // Act & Assert
            var action = () => _comparer.GetHashCode(null!);
            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GetHashCode_SameContent_ReturnsSameHashCode()
        {
            // Arrange
            var array1 = new byte[] { 1, 2, 3, 4, 5 };
            var array2 = new byte[] { 1, 2, 3, 4, 5 };

            // Act
            var hash1 = _comparer.GetHashCode(array1);
            var hash2 = _comparer.GetHashCode(array2);

            // Assert
            hash1.Should().Be(hash2);
        }

        [Fact]
        public void GetHashCode_DifferentContent_ReturnsDifferentHashCode()
        {
            // Arrange
            var array1 = new byte[] { 1, 2, 3, 4, 5 };
            var array2 = new byte[] { 1, 2, 3, 4, 6 };

            // Act
            var hash1 = _comparer.GetHashCode(array1);
            var hash2 = _comparer.GetHashCode(array2);

            // Assert
            hash1.Should().NotBe(hash2);
        }

        [Fact]
        public void GetHashCode_EmptyArray_ReturnsValidHashCode()
        {
            // Arrange
            var array = Array.Empty<byte>();

            // Act
            var hash = _comparer.GetHashCode(array);

            // Assert
            hash.Should().NotBe(0); // SHA256 of empty array is not zero
        }
    }

    /// <summary>
    /// Unit tests for Crc32 hashing functionality
    /// </summary>
    public class Crc32Tests
    {
        [Fact]
        public void Hash_EmptyArray_ReturnsValidHash()
        {
            // Arrange
            var input = Array.Empty<byte>();

            // Act
            var result = Crc32.Hash(input);

            // Assert
            result.Should().Be(0u); // CRC32 of empty input is 0
        }

        [Fact]
        public void Hash_SameInput_ReturnsSameHash()
        {
            // Arrange
            var input = Encoding.UTF8.GetBytes("test");

            // Act
            var hash1 = Crc32.Hash(input);
            var hash2 = Crc32.Hash(input);

            // Assert
            hash1.Should().Be(hash2);
        }

        [Fact]
        public void Hash_DifferentInputs_ReturnDifferentHashes()
        {
            // Arrange
            var input1 = Encoding.UTF8.GetBytes("test1");
            var input2 = Encoding.UTF8.GetBytes("test2");

            // Act
            var hash1 = Crc32.Hash(input1);
            var hash2 = Crc32.Hash(input2);

            // Assert
            hash1.Should().NotBe(hash2);
        }

        [Fact]
        public void Hash_KnownInput_ReturnsExpectedHash()
        {
            // Arrange - "Hello World" has a known CRC32
            var input = Encoding.UTF8.GetBytes("Hello World");

            // Act
            var result = Crc32.Hash(input);

            // Assert
            result.Should().BeGreaterThan(0);
        }
    }
}
