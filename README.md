# XORFilter.Net

## Overview

This is a .NET implementation of [XOR Filters](https://arxiv.org/pdf/1912.08258.pdf). An XOR Filter is a data structure similar to a Bloom Filter but with distinct advantages in certain scenarios.

### Bloom Filter

A Bloom Filter is a probabilistic data structure that enables fast set membership checks while consuming minimal memory. It operates by applying *n* hash functions to each value in the input, yielding a set of positions in a bit array. These positions are then marked to create a footprint for each value.

Example: Given three hash functions  f<sub>0</sub>, f<sub>1</sub>, f<sub>2</sub> , which map a value  v<sub>1</sub>  to positions 3, 4, and 7, the bit array would be updated as follows:

| 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 |
| - | - | - | - | - | - | - | - | - | - |
| 0 | 0 | 0 | 1 | 1 | 0 | 0 | 1 | 0 | 0 |

To check if  v<sub>1</sub>  is in the set, the Bloom Filter re-applies  f<sub>0</sub>, f<sub>1</sub>, f<sub>2</sub>  and verifies that all corresponding positions in the bit array are set to 1.

The Bloom Filter, however, is susceptible to false positives because the same bits could be set by other values (v<sub>2</sub>, v<sub>3</sub>, etc). For instance, if  f<sub>x</sub>(v<sub>2</sub>) = 3, f<sub>x</sub>(v<sub>3</sub>) = 4 and f<sub>x</sub>(v<sub>4</sub>) = 7, the Bloom Filter would incorrectly indicate that  v<sub>1</sub>  is a member. While false positives are possible, Bloom Filters guarantee no false negatives.


### XOR Filter

Like the Bloom Filter, the XOR Filter allows set membership checks without storing the entire set. Instead of single bits, it uses L-bit values. The XOR Filter employs *n* hash functions (h<sub>0</sub>, h<sub>1</sub>,...h<sub>n</sub>) and a fingerprinting function that generates an L-bit fingerprint for each value. Set membership is verified by ensuring:

Fingerprint(v<sub>1</sub>) = Slot[h<sub>0</sub>(v<sub>1</sub>)] ⊕ Slot[h<sub>1</sub>(v<sub>1</sub>)] ⊕  ....... ⊕  Slot[h<sub>n</sub>(v<sub>1</sub>)]


Example:

Given: <br/>
    &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; L = 4 <br/>
    &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; Fingerprint(v<sub>1</sub>) = 0110 <br/>
    &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; h<sub>0</sub>(v<sub>1</sub>) = 3 <br/> 
    &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; h<sub>1</sub>(v<sub>1</sub>) = 4 <br/> 
    &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; h<sub>2</sub>(v<sub>1</sub>) = 7

| 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 |
| - | - | - | - | - | - | - | - | - | - |
| 0000 | 0000 | 0000 | 0111 | 1010 | 0000 | 0000 | 1011 | 0000 | 0000 |

To verify v<sub>0</sub>'s membership in the set, compute:<br/>
    &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Slot[h<sub>0</sub>(v<sub>1</sub>)] ⊕ Slot[h<sub>1</sub>(v<sub>1</sub>)] ⊕ Slot[h<sub>2</sub>(v<sub>1</sub>)]
 = 0111 ⊕ 1010 ⊕ 1011 = 0110 = Fingerprint(v<sub>1</sub>)
<br/>

#### Peeling

Filling the table involves a “peeling” algorithm. Detailed steps can be found <a href="https://web.stanford.edu/class/archive/cs/cs166/cs166.1216/lectures/13/Slides13.pdf#page=57">here</a>. The library implements the following steps:

1 - Initialize an array of size m = (1.23 × n), where n = number of values in the set.

2 - Choose d = 3 hash functions with a random seed.

3 - Find a peelable value v<sub>1</sub>, that is a value which hashes to a slot that no other value v<sub>n</sub> hashes to.

4 - If no peelable value exists and the set is not fully peeled, return to step 2.

5 - Otherwise, keep track of the peeling order.

6 - Repeat steps 3 - 5 until all values are processed.

7 - Reapply the fingerprints of the peeled values in reverse order as follows:

&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; - If Slot[h<sub>d<sub>x</sub></sub>(v)] hasn't been assigned before in step 7, then set Slot[h<sub>d<sub>x</sub></sub>(v)] to Fingerprint(v). Repeat until all values are processed. <br/>

The choice of m = 1.23 x n  balances memory usage and algorithm success probability. Larger m values improve the probability of a successful peeling but increase memory consumption. Likewise, choosing a higher value for L reduces collision probability at the expense of additional memory.

#### Usage

To generate the filter:

```
var myStrings = new string []{ "this", "is", "a collection", "of strings" };

var values = myStrings.Select(Encoding.ASCII.GetBytes).ToArray();

var filter = new XorFilter32(values);
```

Choose the appropriate implementation based on the size and requirements of your dataset:

| Implementation | Underlying Type | Probability of Error (ε) | Size in Bits |
| - | - | - | - |
| XorFilter8 | byte | 0.00390625% | 1.23 × n × 8 |
| XorFilter16 | ushort | 0.00001525878% | 1.23 × n × 16 |
| XorFilter32 | uint | 2.3283064e-10% | 1.23 × n × 32 |

To check set membership simply use the IsMember function:

```
filter.IsMember(Encoding.ASCII.GetBytes("is"));
```
