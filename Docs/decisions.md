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

## Database string length limits

The EF Core configuration limits game titles and objective labels to 200
characters, notes to 4000 characters, and image URLs to 2048 characters.
These are deliberate persistence safeguards rather than copies of one
universal standard: titles and labels should remain short display values,
notes need substantially more room, and 2048 accommodates normal URLs.
Request validation should return a useful client error before the database is
reached, while the database limit remains a second line of defense.

## Development authentication and token lifetime

The first authentication slice uses one configured development account and
issues a signed JWT containing the user's database ID and name. This lets us
exercise authenticated ownership now without pretending that registration and
password management are complete.

Development tokens currently expire after eight hours. This is a temporary
convenience choice and should become configuration-driven before deployment.
Production authentication will require password hashing, secret management,
username uniqueness, and a deliberate token/refresh-token policy.
