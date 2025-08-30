<p align="center">
    <img src="XORFilter.Net/media/logo.svg" width="720" alt="XORFilter.Net logo" />
</p>

# XORFilter.Net

[![NuGet](https://img.shields.io/nuget/v/XORFilterDotNet.svg)](https://www.nuget.org/packages/XORFilterDotNet)
[![NuGet Downloads](https://img.shields.io/nuget/dt/XORFilterDotNet.svg)](https://www.nuget.org/packages/XORFilterDotNet)

Fast, compact, and production-ready XOR filters for .NET. Perform O(1) approximate membership tests with tiny memory footprints and extremely low false-positive rates.

• Targets: .NET 8.0

## Overview

XORFilter.Net is a .NET implementation of XOR filters, a family of probabilistic data structures for set membership. Compared to Bloom filters, XOR filters provide faster lookups and often better space efficiency while keeping false positives extremely low. See the original paper: "Xor Filters: Faster and Smaller Than Bloom and Cuckoo Filters" ([arXiv:1912.08258](https://arxiv.org/pdf/1912.08258.pdf)).

### Why use XORFilter.Net?

- O(1), branch-light lookups with no false negatives
- Very small: about 1.23 × n slots of L-bit fingerprints
- Choice of fingerprint width: 8, 16, or 32 bits to tune false-positive rate
- Deterministic builds via optional seed
- Thread-safe lookups after construction

## Installation

Package Manager:

```
PM> NuGet\Install-Package XORFilterDotNet -Version 1.0.5
```

.NET CLI:

```
dotnet add package XORFilterDotNet --version 1.0.5
```

## Quickstart

Build a filter from a list of strings (converted to bytes) and query it:

```csharp
using System.Text;
using XORFilter.Net;

var maliciousUrls = new List<string>
{
    "getscammednow.com",
    "malicious-software-is-cool.net",
    "getrichquickfrfr.org",
    "legitssncheck.com",
    "totallylegitcreditcardnumberlookup.com",
    "getmalwarenow.com"
};

var encodedValues = maliciousUrls.Select(Encoding.UTF8.GetBytes).ToArray();

// Choose the fingerprint width that fits your needs (8, 16, or 32 bits)
var filter = XorFilter32.BuildFrom(encodedValues);

bool mightBeMalicious = filter.IsMember(Encoding.UTF8.GetBytes("getrichquickfrfr.org")); // true
bool shouldBeClean   = filter.IsMember(Encoding.UTF8.GetBytes("example.com"));            // likely false
```

To make construction deterministic (reproducible builds), pass a seed:

```csharp
var deterministic = XorFilter16.BuildFrom(encodedValues, seed: 12345);
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
- Query: `bool IsMember(byte[] value)`

Notes:

- Input is deduplicated internally; duplicates don't increase size.
- Lookups are thread-safe after the filter is built.
- Construction may retry with different hashes; the library handles this automatically and may slightly grow the table on failure.

## How it works (brief)

Values are mapped to three disjoint partitions using MurmurHash3 and a CRC32-based fingerprint. A peeling process identifies values that are uniquely mapped, records an order, and fills table slots in reverse so that the XOR of the three slots equals the value's fingerprint. Membership testing XORs the three slots and compares with the fingerprint.

## Comparison with Bloom filters

- Faster lookups and typically better space usage at similar error rates
- Stores L-bit fingerprints instead of bits, enabling strong accuracy with modest memory
- Still probabilistic: false positives possible, no false negatives

## Constraints and best practices

- Static sets: filters are immutable; add/remove requires rebuilding.
- Operates on `byte[]`. Convert from strings with `Encoding.UTF8.GetBytes` or from structs via serialization.
- Minimize allocations in hot paths by reusing `byte[]` buffers when possible.
- For reproducibility across runs, provide a fixed `seed`.

## References

- Xor Filters: Faster and Smaller Than Bloom and Cuckoo Filters — https://arxiv.org/pdf/1912.08258.pdf
- Stanford CS166 slides (peeling and construction) — https://web.stanford.edu/class/archive/cs/cs166/cs166.1216/lectures/13/Slides13.pdf#page=57

## Support and contributions

Issues and PRs are welcome. If you have a use case or find an edge case, please open an issue.