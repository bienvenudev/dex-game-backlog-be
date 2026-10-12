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

## Modern C# syntax in `AppDbContext`

The primary-constructor form:

```csharp
public class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options)
```

is a concise equivalent of declaring a constructor that receives
`DbContextOptions<AppDbContext>` and passes it to the base `DbContext`.

This getter-only property:

```csharp
public DbSet<Game> Games => Set<Game>();
```

returns EF Core's set for `Game` without exposing a replaceable setter. EF
Core can still query and track entities through the returned `DbSet`.

`ApplyConfigurationsFromAssembly` scans the current compiled assembly for
classes implementing `IEntityTypeConfiguration<T>` and applies their
`Configure` methods. It avoids manually registering every configuration in
`OnModelCreating`.

## Concise syntax does not replace explicit checks

`CurrentUser` uses a primary constructor to inject
`IHttpContextAccessor` without a separate field and constructor. That is only
syntax for dependency injection; it does not perform authentication.

The `UserId` property still explicitly checks that the request is
authenticated, that the ID claim exists, and that it contains a valid
`Guid`.
Modern C# syntax can reduce boilerplate, but it should not hide important
security or validation decisions.

## Reading an EF Core one-to-many relationship

```csharp
builder.HasMany<Objective>()
    .WithOne()
    .HasForeignKey(objective => objective.GameId)
    .OnDelete(DeleteBehavior.Cascade);
```

Read this as: one `Game` has many `Objective` rows, and each `Objective` has
one parent `Game`. `Objective.GameId` is the foreign key that connects them.
`WithOne()` does not mean an objective has many games; it means each objective
has one game.

The same relationship can be started from the objective side:

```csharp
builder.HasOne<Game>()
    .WithMany()
    .HasForeignKey(objective => objective.GameId);
```

That means each objective has one game, and each game has many objectives.
Both snippets describe the same relationship, so configuring both is
unnecessary duplication.

The equivalent conceptual SQL join is:

```sql
SELECT *
FROM "Games" AS game
JOIN "Objectives" AS objective
  ON objective."GameId" = game."Id";
```

`OnDelete(DeleteBehavior.Cascade)` means deleting a game also deletes its
objectives. EF uses the configuration to create the database foreign key;
normal queries still use LINQ, which EF translates into SQL joins or related
queries.

## How the dashboard summary is built

`DashboardController` does not store a separate dashboard object in the
database. It calculates the summary from the authenticated user's games and
objectives:

- `GroupBy` counts games by their current status.
- `Average` calculates the average of ratings that exist.
- grouped objective counts identify fully complete games and games with no
  objectives.
- the recent-games query orders by `UpdatedAt` and uses `Take(5)` to return
  only a small dashboard list.

Every query includes the current user's ID. This is both an ownership rule and
a correctness requirement: dashboard totals must not include another user's
games.

The recent-games limit is a presentation/performance decision. It is not the
same kind of rule as "a title is required"; changing it should not alter the
meaning of the stored data.

## IDs in status history responses

`GameStatusHistory.Id` is the identifier of the history row itself. It is
generated with `Guid.NewGuid()` when a successful status transition creates
the row. `GameStatusHistory.GameId` identifies the related game, while
`ChangedBy` identifies the user who made the change. Returning `entry.Id`
therefore lets a client distinguish one history event from another; it is not
the game's ID.

## Validate before persistence

Request validation belongs at the HTTP boundary as well as in the database.
The controller can return a useful `400 Bad Request` for an invalid enum,
blank/oversized text, or invalid rating before EF Core sends SQL. Database
length limits and unique indexes remain important backstops, but database
errors are less useful to an API client than a deliberate validation response.

The validation helper is shared by the game and objective controllers so the
same limits are not reimplemented differently in each endpoint.

## DTO validation versus database configuration

Data-annotation attributes on request DTOs validate the incoming HTTP payload.
For example, `[Required]` rejects a missing title and `[StringLength(200)]`
rejects an oversized label before the controller performs its work.

EF Core configuration protects the database schema, even if data reaches the
database through another path. Controller or service validation remains
necessary for rules that need application context, such as uniqueness,
ownership, rating conditions, and allowed status transitions. These layers
overlap intentionally but serve different boundaries.

Request DTOs use records with explicit properties rather than positional
parameters. This lets attributes such as `[Required]` and `[StringLength]` be
placed directly on the properties that ASP.NET Core validates. Positional
records are still useful for response DTOs because those responses do not need
input validation.

## Filtering and paginating list queries

The game-list query applies ownership and filters before materializing results.
It calculates objective totals in the database projection, so the API does not
load every objective into memory just to calculate progress. `CountAsync`
determines the total matching rows, then `Skip` and `Take` fetch only the
requested page.

The response remains an array for frontend compatibility. Pagination metadata
is sent in headers, which lets newer clients paginate without changing the
existing response body shape.

Query-string binding converts values such as `platform=STEAM` and
`pageSize=20` into the `GameQueryParameters` object. Data annotations validate
simple ranges and formats, while explicit checks reject enum numbers that are
technically representable by C# but are not supported by the API contract.

## Why request DTOs use `init`

Request DTO properties use `get; init;` instead of `get; set;`. ASP.NET Core
can populate the object during model binding, but application code cannot
accidentally change the request values afterward. This makes the input
snapshot effectively read-only once it has been created. It is different from
`get; set;`, which permits mutation at any time.
