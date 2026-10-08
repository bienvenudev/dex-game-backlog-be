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
