# Learning notes

Longer explanations of the backend concepts and tradeoffs I encounter while
building Dex. This file is for non-obvious reasoning.

## JSON and API contracts

### Why enums need explicit JSON configuration

C# enums are represented internally as integer values. Without configuration,
the default JSON serializer can emit an enum such as `Platform.STEAM` as
`0`. The frontend contract uses stable, readable names such as `"STEAM"` and
`"UNPLAYED"`, so `JsonStringEnumConverter` translates enum names to and from
JSON strings at the HTTP boundary. This is a serialization concern: it does
not change how the enum behaves inside C# or how it is stored in a database.

### Why `IReadOnlyList<T>` appears in the DTOs

`IReadOnlyList<T>` expresses an ordered collection without exposing mutation
operations as part of the contract. `ObjectiveOrderInput` uses it because the
client sends the complete desired order; the backend will validate the IDs
and assign positions. `GameDetailResponse` and `DashboardSummaryResponse`
use it because they return ordered or limited lists of objects. This is about
communicating the intended boundary, not about making the underlying list
impossible to mutate internally.
