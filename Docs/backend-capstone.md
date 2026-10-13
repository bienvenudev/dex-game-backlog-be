## **Instructions**

1. Build the capstone project below as an ASP.NET project.
2. Push the source code to a public Github repository.

## **Project Introduction**

Build a backend for the game backlog tracker desktop application.

The platform allows an authenticated user to record the games they own, track whether each game is unplayed, in progress, finished or abandoned, rate and take notes on the games they have played, and maintain a checklist of personal objectives for each game.

Ticking objectives off updates a game's completion progress, so the user can see how close each game is to being done. The backend also provides summary data for the library overview.

The React desktop client for this platform has already been built against a mocked service layer. The API must satisfy the same contract that the client was designed against.

## Requirements

- Build an API using **ASP.NET Core** that allows a user to manage a game backlog and its related objectives.
- The backend should support authentication.
- The platform should have one authenticated user type only (role).
    - The authenticated user should be able to manage and view everything they own.
    - Separate admin and user roles are not required.
    - Public user access is not required.
- A user should only ever see and modify their own games and objectives.
    - Requesting a game belonging to another user should behave the same as requesting a game that does not exist.
- The authenticated user should be able to create, view, update, delete, and search games.
- A game should have:
    - **Title**
    - **Platform**
    - **Status**
    - **Rating** optional
    - **Notes** optional
    - **Started At** optional
    - **Finished At** optional
    - **Created At**
    - **Updated At**
- Platforms should include:
    - `STEAM`
    - `EPIC`
    - `GOG`
    - `XBOX`
    - `PLAYSTATION`
    - `OTHER`
- Game statuses should include:
    - `UNPLAYED`
    - `PLAYING`
    - `FINISHED`
    - `ABANDONED`
- A game should be created in `UNPLAYED` status by default.
- The combination of title and platform should be unique for a given user.
    - The same user cannot record `Hollow Knight` on `STEAM` twice.
    - The same user may record `Hollow Knight` on `STEAM` and on `GOG` as two separate games.
- A rating should be a whole number from 1 to 10.
- A rating may only be set on a game that is not `UNPLAYED`.
    - Moving a game back to `UNPLAYED` should clear its rating.
- The authenticated user should be able to add objectives to a game.
- A game can have multiple objectives.
- An objective should have:
    - **Label**
    - **Completed**
    - **Position**
    - **Completed At** optional
    - **Game ID**
    - **Created At**
    - **Updated At**
- An objective must belong to exactly one game.
- An objective label must be unique within the same game.
    - Example: Game A cannot have two objectives labelled `Beat the game on hard`.
    - Game B may have an objective with the same label, because uniqueness is per game rather than global.
- Objectives should have a stable order within a game, and the user should be able to reorder them.
- The authenticated user should be able to update an objective without re-submitting the entire game.
- The authenticated user should be able to tick and untick an objective.
    - Ticking an objective should record when it was completed.
    - Unticking an objective should clear that timestamp.
- The authenticated user should be able to remove an objective.
- Deleting a game should delete its objectives.
- The system should calculate completion progress for a game.
- Completion progress should include:
    - The number of completed objectives.
    - The total number of objectives.
    - A percentage, rounded to the nearest whole number.
- A game with no objectives should report no progress rather than zero or full progress.
    - The percentage should be null, not `0`.
- The authenticated user should be able to change a game's status.
- Status transitions should follow these rules:
    - `UNPLAYED` may move to `PLAYING`, `FINISHED` or `ABANDONED`.
    - `PLAYING` may move to `FINISHED`, `ABANDONED` or back to `UNPLAYED`.
    - `FINISHED` may move to `PLAYING` or `ABANDONED`.
    - `ABANDONED` may move to `PLAYING` or `FINISHED`.
    - A transition to the status a game already holds should be rejected.
- Moving a game to `PLAYING` for the first time should record when it was started.
- Moving a game to `FINISHED` should record when it was finished.
- A game may be moved to `FINISHED` while objectives remain unticked.
    - The response should indicate that the game was finished with outstanding objectives, so the client can inform the user.
    - The backend should not block the transition.
- The system should record game status history.
- Game status history should include:
    - **Game ID**
    - **Previous Status**
    - **New Status**
    - **Changed By**
    - **Changed At**
    - **Comment** optional
- The authenticated user should be able to view a library list of all their games.
- The library list should support searching and filtering by:
    - Title keyword
    - Platform
    - Status
    - Rating range
    - Whether the game has objectives
    - Whether the game is fully complete
- The library list should support sorting by title, rating, completion progress and date last updated.
- Each entry in the library list should include the game's title, platform, status, rating and completion progress.
    - The library list should not return the full objective list for every game.
- The authenticated user should be able to view a game detail response that includes:
    - Game details
    - The full objective list, in order
    - Completion progress
    - Game status
- The authenticated user should be able to view dashboard summary data.
- The dashboard summary should include:
    - Total number of games
    - Number of games in each status
    - Number of games fully complete
    - Number of games with no objectives
    - Average rating across rated games
    - Recently updated games

## **Technical Requirements**

- Use **ASP.NET Core** for the backend API.
- Use **controllers** to define the API endpoints (rather than minimal APIs).
- Use **PostgreSQL** as the database.
- Use **Entity Framework Core** for data access.
- Use clear REST methods:
    - `GET`
    - `POST`
    - `PUT`
    - `PATCH`
    - `DELETE`
- Use proper HTTP status codes:
    - `200 OK` for successful reads and updates.
    - `201 Created` for new resources.
    - `204 No Content` for successful deletes where no response body is needed.
    - `400 Bad Request` for validation errors.
    - `401 Unauthorized` when authentication is required.
    - `404 Not Found` if an ID does not exist, or belongs to another user.
    - `409 Conflict` for duplicate titles, duplicate objective labels, or invalid status transitions.
- Use database constraints to protect important relationships.
- An objective must not exist without a parent game.
- A game must not exist without an owning user.
- Title and platform together must be unique per user.
- Objective label must be unique within the same game.
- Use transactions when creating or updating related data.
- Creating a game with objectives should be transactional.
    - If one objective fails validation, the entire game creation should roll back.
- Changing a game's status should be transactional.
    - The status update and the status history record should both succeed or both fail.
- Reordering objectives should be transactional.
    - Either every objective receives its new position, or none of them do.
- Clearing a rating when a game returns to `UNPLAYED` should happen in the same transaction as the status change.
- Use server-side validation for all important fields.
- Validation rules should include:
    - Game title is required.
    - Platform is required and must be a valid value.
    - Status must be a valid value.
    - Rating, when present, must be a whole number from 1 to 10.
    - Rating must not be set on an `UNPLAYED` game.
    - Objective label is required.
    - Invalid status transitions should be rejected.
- The API should not trust the frontend to enforce business rules.
    - In particular, completion progress must be calculated by the backend and never accepted from the client.
- The backend should expose endpoints similar to the following.

### Games

```
GET    /api/games
POST   /api/games
GET    /api/games/{gameId}
PUT    /api/games/{gameId}
DELETE /api/games/{gameId}
PATCH  /api/games/{gameId}/status
GET    /api/games/{gameId}/progress
GET    /api/games/{gameId}/history
```

### Objectives

```
GET    /api/games/{gameId}/objectives
POST   /api/games/{gameId}/objectives
GET    /api/games/{gameId}/objectives/{objectiveId}
PUT    /api/games/{gameId}/objectives/{objectiveId}
DELETE /api/games/{gameId}/objectives/{objectiveId}
PATCH  /api/games/{gameId}/objectives/{objectiveId}/complete
PATCH  /api/games/{gameId}/objectives/{objectiveId}/reopen
PUT    /api/games/{gameId}/objectives/order
```

### Dashboard

```
GET    /api/dashboard/summary
```

- The game detail endpoint should return game details, objectives and completion progress in a single response.
- Avoid inefficient database queries.
    - The library list endpoint should not query the database separately for every game to calculate its progress.
    - Completion progress for the library list should be produced by the database through a projection or grouped query rather than by loading every objective into memory.
    - Use appropriate Entity Framework Core query techniques such as `Include`, projections, or split queries where appropriate.
- Use DTOs for API requests and responses.
    - Do not expose database entities directly from API endpoints.
- Use pagination for list endpoints.
- List endpoints should support filtering and sorting where appropriate.
- The system should be designed so that game status changes can later be published as events to an external system.
- Add an abstraction for future event publishing.
- The system should be designed so that it can run either as a local process bundled with the desktop client or as a remote service.
    - Do not hard-code the host or port the client will use.
    - Keep configuration such as connection strings and allowed origins in configuration rather than in code.

## **Testing Requirements**

- The project must include automated tests.
- Tests should cover crucial behaviour with both positive and negative cases.
- At minimum, cover the following areas.

### **Create Game**

- **Positive:** Creating a game with valid details saves the game successfully.
- **Positive:** Creating a game with valid objectives saves the game and objectives successfully.
- **Positive:** A newly created game is in `UNPLAYED` status.
- **Negative:** Creating a game with the same title and platform as an existing game returns `409 Conflict`.
- **Negative:** Creating a game with an invalid objective causes the whole save to roll back.
- **Negative:** Creating a game without a title returns `400 Bad Request`.
- **Negative:** Creating a game with a rating while it is `UNPLAYED` returns `400 Bad Request`.

### **Objective Management**

- **Positive:** Adding a valid objective to an existing game succeeds.
- **Positive:** Ticking an objective marks it complete and records the completion time.
- **Positive:** Unticking an objective marks it incomplete and clears the completion time.
- **Positive:** Reordering objectives updates every position.
- **Negative:** Adding an objective to a non-existing game returns `404 Not Found`.
- **Negative:** Adding a duplicate objective label for the same game returns `409 Conflict`.
- **Negative:** Adding an objective with a missing label returns `400 Bad Request`.
- **Negative:** A failed reorder leaves the original order untouched.

### **Completion Progress**

- **Positive:** A game with three objectives, one complete, reports one of three and 33 percent.
- **Positive:** A game with every objective complete reports 100 percent.
- **Positive:** Progress in the library list matches progress in the game detail response.
- **Positive:** Deleting an objective recalculates progress.
- **Negative:** A game with no objectives reports a null percentage rather than zero.
- **Negative:** Progress supplied by the client in a request body is ignored.

### **Status Transitions**

- **Positive:** Moving an `UNPLAYED` game to `PLAYING` succeeds and records the start date.
- **Positive:** Moving a `PLAYING` game to `FINISHED` succeeds and records the finish date.
- **Positive:** Every status change creates a status history record.
- **Positive:** Finishing a game with unticked objectives succeeds and reports the outstanding objectives.
- **Negative:** Moving a game to the status it already holds returns `409 Conflict`.
- **Negative:** An invalid status value returns `400 Bad Request`.
- **Negative:** A status change on a non-existing game returns `404 Not Found`.

### **Rating Rules**

- **Positive:** Setting a rating on a `PLAYING` game succeeds.
- **Positive:** Moving a rated game back to `UNPLAYED` clears its rating.
- **Negative:** A rating of `0` or `11` returns `400 Bad Request`.
- **Negative:** A non-integer rating returns `400 Bad Request`.

### **Ownership**

- **Positive:** A user can read their own game.
- **Negative:** Requesting another user's game returns `404 Not Found`.
- **Negative:** Updating another user's game returns `404 Not Found`.
- **Negative:** Adding an objective to another user's game returns `404 Not Found`.
- **Negative:** Another user's games never appear in the library list or dashboard summary.

### **Deletion**

- **Positive:** Deleting a game returns `204 No Content`.
- **Positive:** Deleting a game also deletes its objectives.
- **Negative:** Deleting a non-existing game returns `404 Not Found`.

### **Validation**

- **Positive:** Valid game and objective data passes validation.
- **Negative:** Missing required fields return `400 Bad Request`.
- **Negative:** Invalid enum values return `400 Bad Request`.
- **Negative:** Duplicate unique values return `409 Conflict`.

### **Not Found Behaviour**

- **Positive:** Requesting an existing game ID returns `200 OK`.
- **Negative:** Requesting a non-existing game ID returns `404 Not Found`.
- **Positive:** Requesting an existing objective ID returns `200 OK`.
- **Negative:** Requesting a non-existing objective ID returns `404 Not Found`.
- **Negative:** Requesting an objective ID that belongs to a different game returns `404 Not Found`.