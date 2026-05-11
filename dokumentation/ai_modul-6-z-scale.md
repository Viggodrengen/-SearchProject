# AI Extract: Modul 6 - z-scale.pptx

- Kilde: `Modul 6 - z-scale.pptx`
- Type: `pptx`
- Indhold: udtraek af slide-tekst + indlejrede billeder

## Slide 1

- Z-
- scale

## Slide 2

- When to z-scale
- A Z-scaled system typically has:
- A partition key
- Something you can deterministically route on
- Examples:
- userId
- ,
- customerId
- , region,
- accountNumber
- Exclusive ownership
- One partition/node is responsible for a given piece of data
- Locality
- Reads and writes usually hit one node only
- Cross-partition operations are minimized or handled asynchronously

## Slide 3

- What
- is
- meaning
- of z-
- scale
- ?
- Z-
- scaling
- (
- also
- called
- data
- partitioning
- or
- sharding
- )
- means
- :
- You
- scale
- the system by
- dividing
- the data set
- into
- independent partitions and routing
- each
- request
- to the node
- that
- owns
- the relevant data.
- Each
- node:
- Stores
- only
- part of the total data
- Handles
- only
- requests
- for
- that
- part
- Can
- often
- operate
- without
- coordinating
- with
- other
- nodes

## Slide 4

- Z-scale - visual
- Reception
- Logic
- DataAccess
- Data A
- Logic
- DataAccess
- Data B
- Logic
- DataAccess
- Data C

