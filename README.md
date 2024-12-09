# XORFilter.Net [![NuGet Package](https://img.shields.io/nuget/v/XorFilterDotnet.svg)](https://www.nuget.org/packages/XORFilterDotNet)

## Overview

This is a .NET implementation of [XOR Filters](https://arxiv.org/pdf/1912.08258.pdf). An XOR filter is a compact, probabilistic data structure used for approximate membership testing, designed to efficiently determine whether an element is in a set with minimal false-positive rates and better space efficiency than Bloom filters in many cases. Unlike Bloom filters, which use multiple hash functions and bit arrays, XOR filters leverage a combination of XOR operations and hash functions to achieve faster lookups and reduced memory overhead.

## Installation

You can install from the package manager console:

`PM> NuGet\Install-Package XORFilterDotNet -Version 1.0.5`

Or from the .NET CLI as:

`dotnet add package XORFilterDotNet --version 1.0.5`

## Bloom Filters

A Bloom Filter is a probabilistic data structure that enables fast set membership checks while consuming minimal memory. A well-known use case for Bloom filters involves efficiently checking whether a URL is part of a large list of prohibited or restricted addresses, such as those flagged for malware, phishing, or policy violations. When a user attempts to access a URL, the system queries the Bloom filter to determine if the URL might be on the restricted list. If the Bloom filter indicates a negative result, the URL is guaranteed to be safe (not restricted), avoiding the need for further checks. If it gives a positive result, it signals that the URL might be restricted, prompting a more precise and computationally intensive check against the full list. This approach significantly reduces the overhead of storing and querying massive URL databases while ensuring rapid responses.

Bloom Filters operate by applying *n* hash functions to each value in the input, yielding a set of positions in a bit array. These positions are then marked to create a footprint for each value.

Example: Given three hash functions  f&#8320;, f&#8321;, f&#8322; , which map a value  v&#8321;  to positions 3, 4, and 7, the bit array would be updated as follows:

| 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 |
| - | - | - | - | - | - | - | - | - | - |
| 0 | 0 | 0 | 1 | 1 | 0 | 0 | 1 | 0 | 0 |

To check if  v&#8321;  is in the set, the Bloom Filter re-applies f&#8320;, f&#8321;, f&#8322; and verifies that all corresponding positions in the bit array are set to 1.

The Bloom Filter, however, is susceptible to false positives because the same bits could be set by other values (v&#8322;, v&#8323;, etc). For instance, if  f&#8339;(v&#8322;) = 3, f&#8339;(v&#8323;) = 4 and f&#8339;(v&#8324;) = 7, the Bloom Filter would incorrectly indicate that  v&#8321;  is a member. While false positives are possible, Bloom Filters guarantee no false negatives.


## XOR Filters

Like the Bloom Filter, the XOR Filter allows set membership checks without storing the entire set. One major difference between the two is that instead of single bits (used in Bloom), XOR Filters use L-bit values. The XOR Filter employs *n* hash functions (h&#8320;, h&#8321;,...h&#8345;) and a fingerprinting function that generates an L-bit fingerprint for each value. Set membership is verified by checking:

Fingerprint(v&#8321;) = Slot[h&#8320;(v&#8321;)] ⊕ Slot[h&#8321;(v&#8321;)] ⊕  ....... ⊕  Slot[h&#8345;(v&#8321;)]


### Example:

Given:  

&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; L = 4  

&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; Fingerprint(v&#8321;) = 0110  

&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; h&#8320;(v&#8321;) = 3  

&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; h&#8321;(v&#8321;) = 4  

&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; h&#8322;(v&#8321;) = 7

| 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 |
| - | - | - | - | - | - | - | - | - | - |
| 0000 | 0000 | 0000 | 0111 | 1010 | 0000 | 0000 | 1011 | 0000 | 0000 |

To verify v&#8321;'s membership in the set, compute:  

&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Slot[h&#8320;(v&#8321;)] ⊕ Slot[h&#8321;(v&#8321;)] ⊕ Slot[h&#8322;(v&#8321;)]
 = 0111 ⊕ 1010 ⊕ 1011 = 0110 = Fingerprint(v&#8321;)  


### Peeling

Filling the table involves a “peeling” algorithm. Detailed steps can be found [here](https://web.stanford.edu/class/archive/cs/cs166/cs166.1216/lectures/13/Slides13.pdf#page=57). The library implements the following steps:

1 - Initialize an array of size m = (1.23 × n), where n = number of values in the set.

2 - Choose d = 3 hash functions with a random seed.

3 - Find a peelable value v&#8321;, that is a value which hashes to a slot that no other value v&#8345; hashes to.

4 - If no peelable value exists and the set is not fully peeled, return to step 2.

5 - Otherwise, keep track of the peeling order.

6 - Repeat steps 3 - 5 until all values are processed.

7 - Reapply the fingerprints of the peeled values in reverse order as follows:

&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; - If Slot[h&#8339;(v)] hasn't been assigned before in step 7, then set Slot[h&#8339;(v)] to Fingerprint(v). Repeat until all values are processed.  

The choice of m = 1.23 x n  balances memory usage and algorithm success probability. Larger m values improve the probability of a successful peeling but increase memory consumption. Likewise, choosing a higher value for L reduces collision probability at the expense of additional memory.

### Usage

To generate the filter:

```csharp
var maliciousUrls = new List<string>
{
    "getscammednow.com",
    "malicious-software-is-cool.net",
    "getrichquickfrfr.org",
    "legitssncheck.com",
    "totallylegitcreditcardnumberlookup.com",
    "getmalwarenow.com"
};

var encodedValues = maliciousUrls.Select(Encoding.ASCII.GetBytes).ToArray();
            
var filter = XorFilter32.BuildFrom(encodedValues); //The builder method expects a collection of byte arrays
```

Choose the appropriate implementation based on the size and requirements of your dataset:

| Implementation | Underlying Type | Probability of Error (ε) | Size in Bits |
| - | - | - | - |
| XorFilter8 | byte | 0.00390625% | 1.23 × n × 8 |
| XorFilter16 | ushort | 0.00001525878% | 1.23 × n × 16 |
| XorFilter32 | uint | 2.3283064e-10% | 1.23 × n × 32 |

To check set membership simply use the IsMember function:

```csharp
bool isMaliciousUrl = filter.IsMember(Encoding.ASCII.GetBytes("getrichquickfrfr.org")); //Returns true
```