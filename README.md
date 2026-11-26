# Task Manager

A small ASP.NET Core app with a plain HTML page and an in-memory task store. Data is cleared when the app stops.

## Run

```sh
dotnet run
```

Open the URL printed by `dotnet run`. Swagger UI is available at `/swagger`.

## Admin account

Public registration always creates a `User`. To enable admin-only task deletion, set both admin values before starting the app:

```sh
Admin__Email=admin@example.com Admin__Password='choose-a-long-password' dotnet run
```

The admin password is hashed before it is kept in memory. Configure `Jwt__Key` to a stable, private value of at least 32 bytes when tokens need to remain valid across restarts. Without it, a fresh signing key is generated each time the app starts.

## API

- `POST /api/auth/register` and `POST /api/auth/login` return access and refresh tokens.
- `POST /api/auth/refresh` rotates a refresh token.
- Authenticated users can list, view, create, and update tasks.
- Only an `Admin` can delete tasks.
- `GET /api/tasks?page=1&pageSize=10&search=term` supports pagination and title/description search.
- Task status must be `Todo`, `InProgress`, or `Done`.
