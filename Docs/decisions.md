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

The authentication slice reads a list of development accounts from
`Authentication:Users` in user secrets and issues a signed JWT containing the
user's database ID and name. The login response also returns the username so
the frontend can show who is signed in. This lets us exercise authenticated
ownership now without pretending that registration and password management are
complete.

Development tokens currently expire after eight hours. This is a temporary
convenience choice and should become configuration-driven before deployment.
Production authentication will require password hashing, secret management,
username uniqueness, and a deliberate token/refresh-token policy.

## Development seed data

In Development, `DevelopmentSeeder` adds sample games and objectives so the
frontend has data to show. Games 1-8 belong to `ben` and games 9-12 belong to
`jane`, which lets us demonstrate ownership isolation (jane gets `404` on
ben's games).

The seeder runs at startup, creates a configured user if it's missing, and
skips any user who already has games, so restarts never duplicate data. It
uses fresh GUIDs and maps the frontend's old string ids onto them.

We chose startup seeding over EF Core's `HasData`. `HasData` stores the rows in
a migration, which needs fixed GUIDs and users that exist before the migration
runs. A startup seeder keeps sample data out of the schema history and is
easy to remove before deployment.

The seeder runs only in Development and assumes the database is already
migrated.

## Dashboard recent-games limit

The dashboard returns at most five recently updated games by applying
`Take(5)` after ordering by `UpdatedAt` descending. The capstone requires
dashboard summary data and a `RecentlyUpdatedGames` collection, but it does
not prescribe a specific number of items. Five is an intentional small
response-size choice for the dashboard, not a business rule. It can be
changed later or made configurable if the UI needs a different limit.

## Game-list pagination metadata

The game-list endpoint keeps returning an array to remain compatible with the
existing frontend contract. It accepts filtering, sorting, `page`, and
`pageSize` query parameters, while pagination metadata is returned in
`X-Total-Count`, `X-Page`, and `X-Page-Size` response headers. This adds
pagination without forcing the current client to change from an array response
to a wrapper object.

## Status changes in a service

The status-change rules (allowed transitions, `startedAt` and `finishedAt`
timestamps, clearing the rating on `UNPLAYED`, the history row, and the
published event) live in `GameStatusService`. `GamesController.UpdateStatus`
only maps the service's outcome to an HTTP status code.

The service is registered as a concrete scoped class, not behind an interface.
A separate interface would only help if we needed a second implementation or a
fake, and neither exists yet. We can introduce `IGameStatusService` later
without changing the controller's behaviour.

## Kubernetes deployment

Kubernetes is a learning extension, not a capstone requirement. It does test
the requirement that the API can run as a remote service with its settings in
configuration.

- **No microservices.** The project has two workloads with different natures:
  a stateless API (a Deployment with replicas) and a stateful database (a
  StatefulSet with a volume). Splitting the API further would add complexity
  without teaching more.
- **Migrations in a Job, not at startup.** Calling `MigrateAsync` in
  `Program.cs` would make every replica try to migrate when it starts. An EF
  migrations bundle, built into the image and run once by a Job, migrates
  before the API rolls out.
- **Configuration through environment variables.** The ConfigMap and Secret use
  the same keys as user secrets (`ConnectionStrings__DefaultConnection`,
  `Jwt__Key`), so `Program.cs` reads them without new code.
- **Plain HTTP inside the cluster.** `HttpsRedirection:Enabled` turns off the
  redirect, because a real deployment ends TLS at an ingress in front of the
  pods.
- **Development environment in the cluster.** This brings back the seeder and
  Swagger for a local learning cluster. Both replicas seed at startup, and the
  unique index on `Users.Username` makes the losing replica fail and restart
  once instead of creating duplicate users. A production setup would seed
  separately or not at all.
- **Versioned image tags.** The node caches images by tag, so each rebuild gets a
  new tag (`dex-api:0.2`, `0.3`, …) instead of reusing `latest` or `local`.
