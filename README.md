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

The implementation will continue after this initial pull request.
