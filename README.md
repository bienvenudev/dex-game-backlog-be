# DexGameBacklog.Api

Backend capstone for the Dex game backlog desktop application. The API is
being built with ASP.NET Core and will provide persistence, authentication,
ownership isolation, game and objective management, progress calculation,
status history, dashboard data, and automated tests for the existing React
and Tauri frontend.

## Assignment goal

The goal is to build a controller-based ASP.NET Core API that satisfies the
frontend's existing HTTP contract while implementing the backend business
rules on the server. The finished project will use Entity Framework Core with
PostgreSQL, protect each user's data, validate requests, expose predictable
REST responses, and remain understandable enough to explain during review.

## Current status

This repository is a work in progress. The current stage establishes the
foundation:

- the ASP.NET Core starter API runs successfully;
- the frontend contract has been translated into request and response
  contracts;
- domain entities for users, games, objectives, and status history exist;
- EF Core dependencies and an initial `AppDbContext` have been added;
- database configuration classes are being developed incrementally;
- learning decisions and open questions are recorded in `Docs/`.

The API is not complete yet. PostgreSQL registration, migrations,
authentication, controllers, business operations, dashboard behavior, and
automated tests remain to be implemented.

## Learning approach

This is an incremental training project. Each step is designed to be small,
runnable, and explainable. The design records in `Docs/decisions.md` and
`Docs/learning-notes.md` capture important tradeoffs and concepts.

## Technology

- ASP.NET Core
- C#
- Entity Framework Core
- PostgreSQL through Npgsql
- Controller-based REST API

## Experience and challenges so far

The most useful experience has been learning how the frontend contract
influences backend design without copying the frontend architecture. In
particular, the backend distinguishes API DTOs from domain entities, keeps
progress server-owned, and preserves the frontend's uppercase enum values.

The main challenges so far have been deciding where responsibilities belong:
HTTP contracts, domain concepts, and database mapping are related but are not
the same thing. Understanding foreign keys versus navigation properties,
separate EF Core configuration, validation layers, and the request lifecycle
has been more important than adding code quickly.

## Development commands

```bash
dotnet build
dotnet run
dotnet test
```

## Local development setup

Secrets are kept out of the repository with .NET user secrets. Set these once
per machine, from the project folder:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<postgres connection string>"
dotnet user-secrets set "Jwt:Key" "<any long random string>"
dotnet user-secrets set "Authentication:Users:0:Username" "ben"
dotnet user-secrets set "Authentication:Users:0:Password" "<password>"
dotnet user-secrets set "Authentication:Users:1:Username" "jane"
dotnet user-secrets set "Authentication:Users:1:Password" "<password>"
```

Apply migrations before the first run:

```bash
dotnet ef database update
```

In Development, startup adds sample data: games 1-8 belong to `ben` and games
9-12 belong to `jane`. Restarting the app never duplicates it. Log in as
either user at `POST /api/auth/login` to see that users only see their own
games.

## Testing

Tests run against PostgreSQL, using a separate database from the development
one so test data never touches the demo games.

Create the test database, then point the tests at it with an environment
variable. Use your own password:

```bash
TEST_DATABASE_CONNECTION_STRING="Host=localhost;Port=5432;Database=dex_game_backlog_test;Username=postgres;Password=<password>" dotnet test
```

In Rider, set the same variable in the test run configuration.

How the tests are organised:

- **Business rules and controller behaviour.** Most tests build a controller
  directly, with a fake `ICurrentUser`, and call its action method. They check
  the returned `ActionResult` and the database state.
- **Pure rules.** `StatusTransitionRulesTests` and `ProgressCalculatorTests`
  don't touch the database.
- **Database fixture.** `PostgresDatabaseFixture` applies migrations once per
  test run. Each test creates its own users with random names and removes them
  at the end, so tests don't depend on each other.
- **Cleanup.** A status history row restricts deletion of its user, so the
  cleanup in `StatusHistoryTests` deletes those rows first.

Not covered yet: full HTTP requests through the pipeline (routing, model
validation, JSON). That needs integration tests.
