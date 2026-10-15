# DexGameBacklog.Api

The backend for the Dex game backlog desktop app: a controller-based ASP.NET Core
API backed by PostgreSQL through Entity Framework Core. It implements the HTTP
contract that the Dex React/Tauri frontend was built against, and enforces the
business rules on the server, where the frontend cannot override them.

This is the backend capstone of a training programme. The frontend lives in the
`game-backlog` repository.

## Assignment goal

Build an ASP.NET Core API that:

- satisfies the frontend's existing HTTP contract;
- keeps each user's data private, with other users' records returning `404`;
- enforces validation, status transitions, progress, and history on the server;
- uses PostgreSQL with EF Core, transactions for multi-write operations, and
  automated tests that prove the rules.

The full brief is in [`Docs/backend-capstone.md`](Docs/backend-capstone.md).

## Features

- **Games:** create, read, update, and delete. The library list supports title
  search, platform, status, rating range, objective presence, full completion,
  sorting, and pagination.
- **Objectives:** create, update, delete, complete, reopen, and reorder. Reordering
  is transactional.
- **Status:** `PATCH /status` enforces allowed transitions, sets `startedAt` and
  `finishedAt`, clears the rating on `UNPLAYED`, and writes a history row in the
  same transaction. The response says whether objectives are still outstanding.
- **Progress:** calculated in SQL. A game without objectives reports `null`, not `0`.
- **Dashboard:** summary totals for the signed-in user only.
- **Authentication:** development JWT login for configured accounts.
- **Sample data:** in Development, the app seeds games for two users.

## Getting started

### Prerequisites

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- PostgreSQL, running locally on port 5432
- The EF Core CLI: `dotnet tool install --global dotnet-ef`

### Configure secrets

Secrets are kept out of the repository with .NET user secrets. Run these once
per machine, from the project folder, replacing the placeholders:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=dex_game_backlog;Username=postgres;Password=<password>"
dotnet user-secrets set "Jwt:Key" "<a long random string, at least 32 characters>"
dotnet user-secrets set "Authentication:Users:0:Username" "ben"
dotnet user-secrets set "Authentication:Users:0:Password" "<password>"
dotnet user-secrets set "Authentication:Users:1:Username" "jane"
dotnet user-secrets set "Authentication:Users:1:Password" "<password>"
```

Non-secret settings (JWT issuer and audience, logging) are in `appsettings.json`
and `appsettings.Development.json`.

### Run

```bash
dotnet ef database update
dotnet run
```

In Development, Swagger is at `https://localhost:7232/swagger`, and the HTTP
profile listens on `http://localhost:5069`. Sign in with `POST /api/auth/login`,
then use the `Authorize` button in Swagger with the token.

### Troubleshooting

- **`307 Temporary Redirect` on HTTP requests:** the app redirects HTTP to HTTPS.
  Call `https://localhost:7232`, or accept the development certificate.
- **`401 Unauthorized`:** the token is missing, or it has expired. Development
  tokens last 8 hours; sign in again.
- **Empty library after startup:** the seed only runs when the database exists
  and the schema is up to date. Run `dotnet ef database update`, then restart.
- **Validation errors look different:** model-validation failures (for example a
  missing required field) return ASP.NET's `ProblemDetails` body, with the details
  in `errors`. Business-rule failures return `{ "message": "..." }`.

## Commands

| Command | What it does |
| --- | --- |
| `dotnet build` | Build the API and the test project |
| `dotnet run` | Start the API in Development |
| `dotnet test` | Run the tests (needs `TEST_DATABASE_CONNECTION_STRING`, see [Testing](#testing)) |
| `dotnet ef database update` | Apply migrations to the development database |
| `dotnet ef migrations add <Name>` | Create a migration after changing a model |

## Architecture

### Layers

| Layer | Responsibility |
| --- | --- |
| `Controllers/` | HTTP only: routes, status codes, and mapping to responses |
| `Services/` | Business rules and reusable queries |
| `Contracts/` | Request and response shapes that cross the HTTP boundary |
| `Domain/` | Entities that persistence stores |
| `Data/` | EF Core context, entity configuration, and the development seeder |
| `Mapping/` | Converts entities into response shapes |
| `Migrations/` | Generated schema history |

### File structure

```text
DexGameBacklog.Api/
├── Controllers/        AuthController, GamesController, ObjectivesController, DashboardController
├── Contracts/          Request and response records (GameContracts.cs, AuthContracts.cs)
├── Data/               AppDbContext, entity configurations, DevelopmentSeeder
├── Domain/             Entities.cs: User, Game, Objective, GameStatusHistory, enums
├── Mapping/            ResponseMapper: entities to response shapes
├── Migrations/         EF Core migrations
├── Services/           Rules and queries (see below)
├── Docs/               Assignment brief, decisions, learning notes
├── Tests/
│   └── DexGameBacklog.Api.Tests/   Test project, separate from the API
├── Program.cs          Startup, DI registration, middleware
└── DexGameBacklog.Api.sln
```

`Services/` holds the rules, one responsibility per file:

| File | Responsibility |
| --- | --- |
| `GameStatusService` | Status transitions, timestamps, history, and the status event |
| `StatusTransitionRules` | The table of allowed status moves |
| `GameInputValidator` | Game field rules shared by create and update |
| `UniquenessChecks` | Duplicate game titles and objective labels |
| `OwnedRecords` | Ownership-filtered lookups, so other users' records look missing |
| `GameProgressQuery` and `ProgressCalculator` | Progress counts in SQL, and the percentage rule |
| `DevelopmentAuthService` and `CurrentUser` | Login and the signed-in user |

### Why the tests are nested under `Tests/`

The test project lives in its own folder with its own `.csproj`, and the solution
file ties both projects together. The API project excludes `Tests/**` from
compilation, so test code and test packages never ship with the API. The test
project references the API, which lets the tests call controllers and services
directly.

### Request flow

A request reaches the controller, which takes the user from the token and calls a
service or a query helper. The service applies the rules and saves through
`AppDbContext`, inside a transaction when several rows change. The controller
maps the result to a response shape and returns the status code.

## Testing

Tests run against PostgreSQL, using a separate database from the development
one, so test data never touches the demo games.

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
- **Pure rules.** `StatusTransitionRulesTests`, `ProgressCalculatorTests`, and
  `InputValidationTests` don't touch the database.
- **Database fixture.** `PostgresDatabaseFixture` applies migrations once per
  test run. Each test creates its own users with random names and removes them
  at the end, so tests don't depend on each other.
- **Cleanup.** A status history row restricts deletion of its user, so the
  cleanup in `StatusHistoryTests` deletes those rows first.

Not covered yet: full HTTP requests through the pipeline (routing, model
validation, JSON). That needs integration tests.

## API reference

All game and objective routes need `Authorization: Bearer <token>`. A game or
objective that belongs to another user returns `404`, the same as a missing one.

| Method | Route | Success | Errors |
| --- | --- | --- | --- |
| `POST` | `/api/auth/login` | `200` with token and username | `401` |
| `GET` | `/api/games` | `200`, paged; `X-Total-Count`, `X-Page`, `X-Page-Size` headers | `400` |
| `POST` | `/api/games` | `201` | `400`, `409` duplicate title and platform |
| `GET` | `/api/games/{id}` | `200` with objectives and progress | `404` |
| `PUT` | `/api/games/{id}` | `200` | `400`, `404`, `409` status change or duplicate |
| `DELETE` | `/api/games/{id}` | `204` | `404` |
| `PATCH` | `/api/games/{id}/status` | `200` with `hasOutstandingObjectives` | `400`, `404`, `409` same status or invalid transition |
| `GET` | `/api/games/{id}/progress` | `200` | `404` |
| `GET` | `/api/games/{id}/history` | `200` | `404` |
| `GET` | `/api/games/{id}/objectives` | `200` | `404` |
| `POST` | `/api/games/{id}/objectives` | `201` | `400`, `404`, `409` duplicate label |
| `GET` | `/api/games/{id}/objectives/{oid}` | `200` | `404` |
| `PUT` | `/api/games/{id}/objectives/{oid}` | `200` | `400`, `404`, `409` duplicate label |
| `DELETE` | `/api/games/{id}/objectives/{oid}` | `204` | `404` |
| `PATCH` | `/api/games/{id}/objectives/{oid}/complete` | `200` | `404` |
| `PATCH` | `/api/games/{id}/objectives/{oid}/reopen` | `200` | `404` |
| `PUT` | `/api/games/{id}/objectives/order` | `200` | `400` list must match exactly |
| `GET` | `/api/dashboard/summary` | `200` | |

Enum values on the wire are uppercase strings, for example `PLAYING`. Dates are
ISO 8601.

## Experience and challenges

Starting from the frontend's TypeScript types and mock handlers gave a concrete
contract to build against. Once the real frontend started calling the API, the
mocks hid several gaps. At the first contract check, the frontend sent no token,
changed status through a route the backend rejects, and read only a `message`
field that validation errors don't include. The contract review listed them,
and they were handled one at a time.

Main lessons:

- **Separate DTOs from entities.** Entities hold persistence concerns such as
  foreign keys; responses hold only what the client needs.
- **Ownership is a query filter, not a check after loading.** Filtering in SQL
  means another user's record is never read, and `404` gives nothing away.
- **Cascades and restrictions have consequences.** Deleting a game removes its
  objectives and history. A history row restricts deleting its user, which is why
  test cleanup has to delete history first.
- **Readable data needs joins.** Stored rows contain GUIDs and status numbers.
  Reading them in pgAdmin means joining to `Games` and decoding the numbers.
- **Controllers grew quickly.** The first `GamesController` mixed HTTP, validation,
  rules, and queries in about 650 lines. Extracting mapping, validation, ownership
  lookups, uniqueness checks, and the status service made each file answer one
  question. EF Core projections stay inline, because EF must translate them to SQL.
- **Tests against a real database catch more.** Mutation checks, where a rule is
  deliberately broken to confirm a test fails, showed which tests protect what.

## Known gaps

- No integration tests yet, so routing, model validation, and JSON are only
  covered indirectly.
- The frontend has not yet been verified end to end against this API.
- Development authentication only: no password hashing, a single symmetric key,
  and an 8-hour token lifetime set in code rather than configuration.
- Deleting an objective leaves gaps in positions. Reordering fixes them.
- Production hardening (secret management, CORS policy, deployment) is out of scope.

## Project notes

- [`Docs/backend-capstone.md`](Docs/backend-capstone.md): the assignment brief.
- [`Docs/decisions.md`](Docs/decisions.md): design decisions and their tradeoffs.
- [`Docs/learning-notes.md`](Docs/learning-notes.md): explanations of the concepts
  behind those decisions, written while learning them.

## Roadmap

- Integration tests for the HTTP pipeline.
- Verify the frontend end to end against this API.
- Replace development login with production authentication.
