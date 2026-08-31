# Shuttle.BinarySerde.Dsl

`BinaryRecordCodecBuilder<T>.StreamingField(...)` declares a terminal
`IEnumerable<TItem>` collection in the same reflection-free schema used for both writing and
materialized reading. The count field must occur earlier in the schema and its getter must agree
with the number of items written.

```csharp
var codec = Record<Inventory>()
    .Field(x => x.Count, (x, value) => x.Count = value, int32)
    .StreamingField(x => x.Items, (x, value) => x.Items = value, x => x.Count, item)
    .Build();

using var read = codec.ReadStreaming(source, "inventory");
var header = read.Value.Count;
foreach (var item in read.Value.Items) { /* sequential use */ }
```

`ReadStreaming` does not own or close `source`; the source must stay open until enumeration
finishes. The returned owner keeps the reader buffer alive and must be disposed. Its collection is
lazy, sequential, and can be enumerated once. `codec.Read(reader)` instead materializes the same
schema's collection into a reusable list.
