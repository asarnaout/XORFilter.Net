# XORFilter.Net

## Overview

This is a .NET implementation of XOR Filters. An XOR Filter is a data structure that holds a lot of similarity to the better known Bloom Filter. 

### Bloom Filter

A Bloom Filter is a probabilistic data structure that allows you to quickly verify set membership without consuming much memory. This is done by running *n* hash functions over each value in the given input to retrieve a set of positions in the bit array that will be used, the bloom filter would then set the values at each of those positions which marks a footprint for each of those values in the bit array. 

Example: Given 3 hash functions f<sub>0</sub>, f<sub>1</sub> and f<sub>2</sub> that map to positions: 3, 4 and 7 when run on a value v<sub>1</sub>, we set those values in the bit array as follows:

| 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 |
| - | - | - | - | - | - | - | - | - | - |
| 0 | 0 | 0 | 1 | 1 | 0 | 0 | 1 | 0 | 0 |

And therefore whenever we ask the bloom filter whether v<sub>1</sub> is a member of the set, then we simply re-run f<sub>0</sub>, f<sub>1</sub> and f<sub>2</sub> and verify that the positions at the bit array are all set to 1.

The problem with Bloom filters is that there is a likely degree of error possible since the corresponding bits to v<sub>1</sub> could have been set by other values (v<sub>2</sub>, v<sub>3</sub>, etc). For example, given that f<sub>x</sub>(v<sub>2</sub>) = 3, f<sub>x</sub>(v<sub>3</sub>) = 4 and f<sub>x</sub>(v<sub>4</sub>) = 7 and given that v<sub>1</sub> was never set in the bloom filter before, therefore if we ask the bloom filter if v<sub>1</sub> is a member, then we shall get a false positive result. This is acceptable in context of working with probabilistic data structures: You can have false positives however you can never have false negatives.


### XOR Filter

Same as the bloom filter, the XOR Filter gives us the ability to check set membership without having to keep track of the entire set. With XOR Filter, we keep track of L-bit values rather than single bits. Like bloom filters, we also define n hash functions (h<sub>0</sub>...h<sub>n</sub>) but we also define a *finger printing* function (Fingerprint) that would transform a given value v<sub>1</sub> to an L-bit fingerprint. To check set membership in an XOR Filter, we ensure that the following holds:

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

Therefore to check if v<sub>0</sub> is a member of the set we calculate that:<br/>
    &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Slot[h<sub>0</sub>(v<sub>1</sub>)] ⊕ Slot[h<sub>1</sub>(v<sub>1</sub>)] ⊕ Slot[h<sub>2</sub>(v<sub>1</sub>)]
 = 0111 ⊕ 1010 ⊕ 1011 = 0110 = Fingerprint(v<sub>1</sub>)
<br/>

The challenge is in how to fill this table, this is achieved by running a "peeling" algorithm. Details could be found <a href="https://web.stanford.edu/class/archive/cs/cs166/cs166.1216/lectures/13/Slides13.pdf#page=57">here</a>.

#### Algorithm

To summarize the algorithm implemented in this library:

1 - Initialize the table slots array with a size of 1.23 × (number of values in the set) [To be explained briefly].

2 - Choose d = 3 hash functions with a random seed.

3 - Find a peelable value v<sub>1</sub>, that is a value which hashes to a slot that no other value v<sub>n</sub> hashes to.

4 - If no peelable value exists and the set hasn't been peeled yet then go to step 2.

5 - Otherwise, keep track of the peeling order.

6 - Repeat steps 2 - 6 until no further items are available in the set.

7 - Reapply the fingerprints of the peeled values in reverse order as follows:

&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; A - If Slot[h<sub>d<sub>n</sub></sub>(v)] hasn't been assigned before in step 7. <br/>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; B - If EITHER: <br/>

&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;i. Both h<sub>d<sub>n+1</sub></sub>(v) and h<sub>d<sub>n+2</sub></sub>(v) are equal to h<sub>d<sub>n</sub></sub>(v): If all 3 hashes point to the same slot, then it would be safe to assign the fingerprint directly to the slot since a ⊕ a ⊕ a = a <br/>

&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;ii. h<sub>d<sub>n</sub></sub>(v) != h<sub>d<sub>n+1</sub></sub>(v) AND h<sub>d<sub>n</sub></sub>(v) != h<sub>d<sub>n+2</sub></sub>(v): If any of the two other hashes points to the same slot then it would be unsafe to set the fingerprint directly to that slot since a ⊕ a = 0. In this case it would be safer to assign the hash value to the third slot.

&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; C - Then set Slot[h<sub>d<sub>n</sub></sub>(v)] to Fingerprint(v) <br/>

&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; D - Repeat till all values are handled.

To use the XOR Filter, ensure that you choose the correct value of L, a higher value of L would indeed consume more space, however it would decrease the probability of collisions and false positives.

#### Usage

To generate the filter:

```
var myStrings = new string []{ "this", "is", "a collection", "of strings" };

var values = myStrings.Select(Encoding.ASCII.GetBytes).ToArray();

var filter = new XorFilter32();

filter.Generate(values);
```

To check set membership simply use the IsMember function:

```
filter.IsMember(Encoding.ASCII.GetBytes("is"));
```
