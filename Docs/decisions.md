# Decisions

## API route prefix

Backend routes will use the `/api` prefix because the frontend service layer
defaults to `/api`.

## String enum values at the JSON boundary

ASP.NET Core will serialize `Platform` and `GameStatus` enum values as strings
through `JsonStringEnumConverter`, so the wire format is `"STEAM"` and
`"PLAYING"` rather than numeric values such as `0` and `1`. This matches the
frontend contract and makes payloads readable and less fragile if the enum
member order changes. Numeric enum values are smaller, but they would not
match the existing client contract and are harder to inspect.

## DTO records instead of interfaces

The initial request and response contracts use sealed records rather than
interfaces. These types are concrete JSON shapes crossing the HTTP boundary;
records keep them concise and immutable by default, while `sealed` avoids
inheritance that the API does not need. Interfaces remain useful for
replaceable behavior or multiple implementations, not for these fixed
payloads.

## Read-only collections in contracts

Response collections and the objective reorder request use
`IReadOnlyList<T>`. The API needs to expose or receive an ordered collection,
but the contract should not advertise that callers can mutate the collection
through the DTO. The reorder endpoint will validate the complete objective ID
list and assign positions transactionally; the dashboard and detail responses
use the same collection shape for returned lists.
