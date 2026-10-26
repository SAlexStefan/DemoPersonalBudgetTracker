# Personal Budget Tracker API

An ASP.NET Core Web API for managing a personal budget. It provides user registration and login, built-in and custom categories, and authenticated CRUD operations for transactions.

## Technology

- .NET 10 / ASP.NET Core
- PostgreSQL with Npgsql and Dapper
- JWT bearer authentication
- Swagger / OpenAPI (Development environment)
- xUnit, Moq, Testcontainers, and PostgreSQL for tests

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL 16 or later
- Docker Desktop (only for the integration tests)

## Getting started

1. Create a PostgreSQL database, for example `personal_budget_tracker`.
2. From the repository root, configure the connection string, JWT settings, and the front-end base URL for password-reset links using .NET user secrets:

   ```powershell
   dotnet user-secrets set --project PersonalBudgetTrackerApi "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=personal_budget_tracker;Username=postgres;Password=replace-me"
   dotnet user-secrets set --project PersonalBudgetTrackerApi "JWT:Secret" "replace-this-with-a-long-random-secret"
   dotnet user-secrets set --project PersonalBudgetTrackerApi "Frontend:BaseUrl" "http://localhost:5173"
   ```

   The included `appsettings.json` intentionally has empty values for these settings. Do not commit real credentials or JWT secrets.

3. Start the API:

   ```powershell
   dotnet run --project PersonalBudgetTrackerApi
   ```

4. In the Development environment, open the Swagger UI at the HTTPS address printed by `dotnet run`, followed by `/swagger`.

## Configuration

| Setting                               | Purpose                                                                   |
| ------------------------------------- | ------------------------------------------------------------------------- |
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string used by the API.                             |
| `JWT:Secret`                          | Secret used to sign and validate access tokens. Required.                 |
| `JWT:Issuer`                          | Token issuer. Defaults to `PersonalBudgetTrackerAPI`.                     |
| `JWT:Audience`                        | Token audience. Defaults to `PersonalBudgetTrackerClient`.                |
| `Frontend:BaseUrl`                    | Base URL for password-reset links sent through the forgot-password flow.  |
| `AllowOrigins`                        | Front-end origins permitted by CORS. Defaults to `http://localhost:5173`. |

For deployment, provide these values through environment variables, a secret manager, or your hosting platform’s configuration system.

## API overview

JSON uses camel-cased property names. Category type values are `income` and `expense`.

### Authentication

| Method | Route                                | Description                                                                                                                     |
| ------ | ------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------- |
| `POST` | `/api/Authorisation/register`        | Creates an account and returns an access token, refresh token, and expiry information. Passwords must be at least 8 characters. |
| `POST` | `/api/Authorisation/login`           | Signs in and returns an access token, refresh token, and expiry information.                                                    |
| `POST` | `/api/Authorisation/refresh`         | Exchanges the current refresh token for a new access token and a rotated refresh token.                                         |
| `POST` | `/api/Authorisation/logout`          | Requires a valid bearer token and revokes all refresh tokens for the signed-in user.                                            |
| `POST` | `/api/Authorisation/forgot-password` | Sends a password-reset link to the supplied email address when the account exists.                                              |
| `POST` | `/api/Authorisation/reset-password`  | Validates a reset token, updates the password, marks the token as used, and revokes all refresh tokens for the user.            |

Example registration request:

```json
{
  "email": "user@example.com",
  "password": "a-secure-password"
}
```

Successful registration and login return:

```json
{
  "token": "<jwt>",
  "email": "user@example.com",
  "refreshToken": "<raw-refresh-token>",
  "refreshTokenExpiresAt": "2026-09-14T12:34:56.789Z"
}
```

The refresh-token response follows the same shape and rotates the refresh token on each successful refresh.

Example forgot-password request:

```json
{
  "email": "user@example.com"
}
```

Example reset-password request:

```json
{
  "token": "<raw-reset-token>",
  "newPassword": "a-new-secure-password"
}
```

### Authenticated requests

All category and transaction endpoints require this header:

```http
Authorization: Bearer <jwt>
```

In Swagger, use **Authorize** and paste the token to make authenticated calls.

### Categories

| Method   | Route                | Description                                                          |
| -------- | -------------------- | -------------------------------------------------------------------- |
| `GET`    | `/api/Category`      | Lists the signed-in user’s categories, including default categories. |
| `POST`   | `/api/Category`      | Creates a custom category.                                           |
| `DELETE` | `/api/Category/{id}` | Deletes one of the user’s unused custom categories.                  |

Create-category request:

```json
{
  "name": "Freelance",
  "type": "income"
}
```

Default categories are shared, cannot be deleted by a user, and include Salary, Other income, Groceries, Rent, Utilities, Transport, Entertainment, and Other Expense.

### Transactions

| Method   | Route                   | Description                                                                       |
| -------- | ----------------------- | --------------------------------------------------------------------------------- |
| `GET`    | `/api/Transaction`      | Lists transactions. Optional query parameters: `year`, `month`, and `categoryId`. |
| `GET`    | `/api/Transaction/{id}` | Gets one transaction.                                                             |
| `POST`   | `/api/Transaction`      | Creates a transaction.                                                            |
| `PUT`    | `/api/Transaction/{id}` | Updates a transaction.                                                            |
| `DELETE` | `/api/Transaction/{id}` | Deletes a transaction.                                                            |

Create or update request:

```json
{
  "amount": 42.5,
  "transactionDate": "2026-08-30T12:00:00Z",
  "note": "Weekly groceries",
  "publicCategoryId": "00000000-0000-0000-0000-000000000000"
}
```

`amount` must be greater than zero. A transaction may use a default category or a custom category owned by the signed-in user.

## Tests

See [TESTING.md](TESTING.md) for more detail. Run the full suite from the repository root:

```powershell
dotnet test PersonalBudgetTrackerApi.Tests/PersonalBudgetTrackerApi.Tests.csproj
```

Integration tests use a temporary PostgreSQL container, so Docker must be running. They do not use your development database or its connection string.

## Project layout

```text
PersonalBudgetTrackerApi/        API project
  Controllers/                   HTTP endpoints
  Data/                          PostgreSQL connection factory
  DTOs/                          Request and response contracts
  Models/                        Domain models and category enum mapping
  Repositories/                  Dapper data access
  Services/                      JWT creation
PersonalBudgetTrackerApi.Tests/  Unit and integration tests
TESTING.md                       Test-suite details
```
