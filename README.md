<p align="center">
    <img src="media/logo.svg" width="720" alt="XORFilter.Net logo" />
</p>

# XORFilter.Net

[![NuGet](https://img.shields.io/nuget/v/XORFilterDotNet.svg)](https://www.nuget.org/packages/XORFilterDotNet)
[![NuGet Downloads](https://img.shields.io/nuget/dt/XORFilterDotNet.svg)](https://www.nuget.org/packages/XORFilterDotNet)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)

Fast, compact, and production-ready XOR filters for .NET. Perform O(1) approximate membership tests with tiny memory footprints and extremely low false-positive rates.

• Targets: .NET 8.0

## Overview

XORFilter.Net is a .NET implementation of XOR filters, a family of probabilistic data structures for set membership. Compared to Bloom filters, XOR filters provide faster lookups and often better space efficiency while keeping false positives extremely low. See the original paper: "Xor Filters: Faster and Smaller Than Bloom and Cuckoo Filters" ([arXiv:1912.08258](https://arxiv.org/pdf/1912.08258.pdf)).

### Why use XORFilter.Net?

- O(1), branch-light lookups with no false negatives
- Very small: about 1.23 × n slots of L-bit fingerprints
- Choice of fingerprint width: 8, 16, or 32 bits to tune false-positive rate
- Deterministic builds via optional seed
- Thread-safe lookups after filter construction

## Installation

Package Manager:

```
PM> NuGet\Install-Package XORFilterDotNet -Version 1.0.8
```

.NET CLI:

```
dotnet add package XORFilterDotNet --version 1.0.8
```

## Quickstart

Build a filter from a list of strings (converted to bytes) and query it:

```csharp
using System.Text;
using XORFilter.Net;

var maliciousUrls = new List<string>
{
    "phishing.example",
    "malware.example",
    "fraud.example",
    "credential-theft.example",
    "drive-by-download.example",
    "suspicious-site.example"
};

var encodedValues = maliciousUrls.Select(Encoding.UTF8.GetBytes).ToArray();

// Choose the fingerprint width that fits your needs (8, 16, or 32 bits)
var filter = XorFilter32.BuildFrom(encodedValues);

// Zero-allocation queries using ReadOnlySpan<byte>
bool malicious = filter.IsMember(Encoding.UTF8.GetBytes("phishing.example").AsSpan()); // returns true
bool shouldBeClean = filter.IsMember(Encoding.UTF8.GetBytes("example.com").AsSpan()); // likely returns false

// Or reuse buffers for maximum efficiency in hot paths
var buffer = new byte[256];
int written = Encoding.UTF8.GetBytes("phishing.example", buffer);
bool result = filter.IsMember(buffer.AsSpan(0, written)); // zero allocations
```

## Choosing a fingerprint width

False-positive probabilities (percent) and memory formulas:

| Implementation | Underlying Type | False-positive rate (ε) | Memory |
| - | - | - | - |
| XorFilter8  | byte   | 0.390625%         | ≈ 1.23 × n × 8 bits  |
| XorFilter16 | ushort | 0.0015258789%     | ≈ 1.23 × n × 16 bits |
| XorFilter32 | uint   | 2.3283064e-8%     | ≈ 1.23 × n × 32 bits |

Tip: Start with 16-bit for a good balance. Use 8-bit for tiny memory or 32-bit for near-zero false positives.

## API at a glance

- Build: `XorFilter8.BuildFrom(Span<byte[]> values, int? seed = null)` (also 16 and 32 variants)
- Query:
  - **Recommended**: `bool IsMember(ReadOnlySpan<byte> value)` — zero allocations
  - Legacy: `bool IsMember(byte[] value)` — maintained for backward compatibility

Notes:

- Input is deduplicated internally; duplicates don't increase size.
- Lookups are thread-safe after the filter is built.
- Construction may retry with different hashes; the library handles this automatically and may slightly grow the table on failure.
- Use the `ReadOnlySpan<byte>` overload for maximum performance in hot paths.

## How it works (brief)

Values are mapped to three disjoint partitions using MurmurHash3 and a CRC32-based fingerprint. A peeling process identifies values that are uniquely mapped, records an order, and fills table slots in reverse so that the XOR of the three slots equals the value's fingerprint. Membership testing XORs the three slots and compares with the fingerprint.

## Comparison with Bloom filters

- Faster lookups and typically better space usage at similar error rates
- Stores L-bit fingerprints instead of bits, enabling strong accuracy with modest memory
- Still probabilistic: false positives possible, no false negatives

## Constraints and best practices

- Static sets: filters are immutable; add/remove requires rebuilding.
- Operates on byte sequences. Convert from strings with `Encoding.UTF8.GetBytes` or from structs via serialization.
- **Zero allocations**: Use `IsMember(ReadOnlySpan<byte>)` for hot paths to eliminate heap allocations entirely.
- Reuse buffers when encoding repeatedly: `Encoding.UTF8.GetBytes(string, Span<byte>)` writes directly to a buffer.
- For reproducibility across runs, provide a fixed `seed`.

## References

- Xor Filters: Faster and Smaller Than Bloom and Cuckoo Filters — https://arxiv.org/pdf/1912.08258.pdf
- Stanford CS166 slides (peeling and construction) — https://web.stanford.edu/class/archive/cs/cs166/cs166.1216/lectures/13/Slides13.pdf#page=57

## Support and contributions

Issues and PRs are welcome. If you have a use case or find an edge case, please open an issue.