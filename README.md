# Supermarket

Supermarket web application built for a .NET/Angular technical interview: a .NET 10 API with Clean Architecture, SQL Server, JWT authentication, and an Angular frontend. Built with TDD (MSTest) and covers products, batch-based inventory, FEFO sales, and tasks.

## Architecture

```text
WebAPI ─────────► Application ─────────► Domain
   │                  ▲
   └──► Infraestructure ┘   (implements Application's interfaces)
```

| Project | Responsibility |
|---|---|
| `Domain` | Entities, enums, `DomainException`, and the pure `FefoAllocator` service. Depends on nothing. |
| `Application` | Use cases, DTOs, and interfaces (repositories, `IUnitOfWork`, JWT, hasher, `ICurrentUser`). |
| `Infraestructure` | EF Core + SQL Server, configurations, repositories, migrations, JWT, PBKDF2, and seeding. |
| `WebAPI` | Thin controllers, error middleware, JWT Bearer, CORS, and DI. |
| `supermarket-web` | Angular (standalone components, signals, reactive forms, interceptor, and guards). |

Tests: `Domain.Tests`, `Application.Tests`, `Infraestructure.Tests`, `Api.Tests` (MSTest), and Vitest in `supermarket-web`.

## Getting started

Requirements: .NET 10 SDK, Node 20+, and Docker.

```bash
docker compose up -d
cp WebAPI/appsettings.Development.example.json WebAPI/appsettings.Development.json
dotnet run --project WebAPI --launch-profile http
cd supermarket-web && npm install && npm start
```

- API at `http://localhost:5203` and web app at `http://localhost:4200`.
- `docker-compose.yml` uses the password `Supermarket_Dev_2026!` for `sa`; you can change it with `MSSQL_SA_PASSWORD` and reflect it in the connection string.
- `appsettings.Development.json` is in `.gitignore`. It holds the connection string, `Jwt:Key`, and the seed passwords. The example file has placeholders; fill them in before starting.
- With `Database:MigrateOnStartup` and `Seed:Enabled` set to `true` (only in the Development file), the API migrates the database and loads the data described in `docs/seed.md` on startup.

### OpenAPI documentation

With the API running in `Development`:

- **Interactive UI**: `http://localhost:5203/scalar/v1` (Scalar). Every endpoint shows a summary, description, parameters, body, possible responses, and a `curl` example.
- **Raw document**: `http://localhost:5203/openapi/v1.json`.

To try a protected endpoint from the UI: call `POST /api/auth/login`, copy `accessToken` from the response, and paste it into the **Authentication** panel (top right) under the `Bearer` scheme — no `Bearer ` prefix needed, Scalar adds it for you.

The document is generated with `Microsoft.AspNetCore.OpenApi` (native to .NET, no `///` comments in the code, only attributes):
- `[EndpointSummary]` / `[EndpointDescription]` on every controller action.
- `[ProducesResponseType<T>]` with the real HTTP codes each endpoint can return (see the error table below).
- An `IOpenApiDocumentTransformer` (`WebAPI/OpenApi/BearerSecuritySchemeTransformer.cs`) declares the `Bearer` (JWT) security scheme from the already-registered authentication scheme.
- An `IOpenApiOperationTransformer` (`WebAPI/OpenApi/AuthorizeOperationTransformer.cs`) marks every operation as protected except the `[AllowAnonymous]` ones (login/register) and automatically adds the `401` response to the rest.
- A third transformer (`ApiInfoDocumentTransformer.cs`) sets the document's title, version, and description.

`app.MapOpenApi()` and `app.MapScalarApiReference()` are only registered in `Development`, same as in the original `Program.cs`.

### Seed users

| Email | Role |
|---|---|
| `admin@supermarket.local` | Administrator |
| `employee1@supermarket.local` | Employee (owner of the sample sales and tasks) |
| `employee2@supermarket.local` | Employee |

Passwords come from `Seed:AdminPassword` and `Seed:EmployeePassword`. Public registration always creates an `Employee`.

## Tests

```bash
dotnet test MarketApp.slnx
cd supermarket-web && npm test -- --watch=false
```

`Infraestructure.Tests` and `Api.Tests` spin up a real SQL Server with Testcontainers, so they need Docker. They verify rowversion concurrency, unique indexes, transaction rollback, and real concurrent sales.

| Suite | Tests |
|---|---|
| Domain | 111 |
| Application | 83 |
| Infraestructure | 39 |
| Api | 65 |
| Web (Vitest) | 65 |

## API

Every route except `auth` requires `Authorization: Bearer <token>`. Ready-to-use examples live in `WebAPI/WebAPI.http`, or interactively at `http://localhost:5203/scalar/v1` (see [OpenAPI documentation](#openapi-documentation)).

| Method & route | Access | Notes |
|---|---|---|
| `POST /api/auth/register`, `POST /api/auth/login` | Public | Return `accessToken`, `expiresAt`, and `user`. |
| `GET /api/products`, `GET /api/products/{id}` | Authenticated | `?includeInactive=true` only has effect for administrators. |
| `POST`, `PUT /api/products`, `DELETE /api/products/{id}` | Administrator | `DELETE` deactivates; `PUT` can reactivate via `isActive`. |
| `GET /api/inventory`, `GET /api/inventory/{id}` | Authenticated | Filters: `productId`, `lowStock`, `expired`. |
| `POST /api/inventory`, `PUT /api/inventory/{id}` | Administrator | `PUT` never changes the quantity. |
| `POST /api/inventory/{id}/add-quantity` | Administrator | The only way to restock (`{ "quantity": number }`); `RemoveQuantity` is only ever called by `CreateSale`. |
| `POST /api/sales` | Authenticated | Only accepts `items[{productId, quantity}]`. |
| `GET /api/sales`, `GET /api/sales/{id}` | Authenticated | Employees see their own; administrators see all. |
| `GET`, `POST`, `PUT`, `DELETE /api/tasks` | Authenticated | Always scoped to the token's owner, even for administrators. |

Errors are returned as `application/problem+json` with a `code` field:

| HTTP | Codes |
|---|---|
| 400 | `VALIDATION_ERROR`, `INVALID_PRODUCT`, `INVALID_PRODUCT_PRICE`, `INVALID_INVENTORY`, `INVALID_QUANTITY`, `SALE_EMPTY`, `INVALID_TASK` |
| 401 | `UNAUTHORIZED`, `INVALID_CREDENTIALS` |
| 403 | `FORBIDDEN` |
| 404 | `PRODUCT_NOT_FOUND`, `INVENTORY_NOT_FOUND`, `SALE_NOT_FOUND`, `TASK_NOT_FOUND` |
| 409 | `DUPLICATE_PRODUCT`, `DUPLICATE_BATCH`, `EMAIL_ALREADY_EXISTS`, `PRODUCT_INACTIVE`, `INSUFFICIENT_STOCK`, `INVENTORY_EXPIRED`, `INVALID_TASK_TRANSITION`, `CONCURRENCY_CONFLICT` |

## Sales: FEFO, transaction, and concurrency

1. The use case validates the products (exist, active, valid quantity) and merges repeated lines.
2. `FefoAllocator` (pure domain logic) splits each line across batches by the closest expiration date, ignoring expired, inactive, or empty batches.
3. Stock is only deducted, and the sale only created at the backend's current price, if **every** line can be covered. Nothing is touched if any line fails.
4. The sale, its items, and the stock deduction are saved in a single transaction (`IUnitOfWork.ExecuteInTransactionAsync`).
5. `WarehouseStock` carries a `rowversion` column. If another sale modified the batch, EF raises a concurrency conflict and the use case retries up to 3 times, re-reading inventory each time. Once exhausted, it responds `409 CONCURRENCY_CONFLICT`.

## Design decisions

- **SQL Server as the only provider**, with a real `rowversion`, a filtered unique index (`IsActive = 1`) for the product's logical identity, and a `CHECK (Quantity >= 0)`.
- **Product logical identity**: `Name + Brand + Category + UnitType + UnitValue`, case-insensitive.
- **`UserTask` instead of `Task`**: with `ImplicitUsings`, a type named `Task` or `TaskStatus` collides with `System.Threading.Tasks`. The rename was applied throughout the chain (the entity, `IUserTaskRepository`/`UserTaskRepository`, DTOs and use cases in Application, and the model/service in Angular) to avoid mixing the two names. The route (`/api/tasks`), the table (`Tasks`), `TasksController`, and the UI ("Tareas") keep the business name.
- **`SaleStatus`** is `Completed` or `Incomplete`. A sale starts as `Incomplete`, and `Complete()` requires at least one item.
- **`Unit` products** only accept whole-number quantities.
- **Resources belonging to someone else return 404** (another employee's sale, another user's task) so as not to reveal which ids exist.
- **`ICurrentUser`** comes from the JWT; no request carries a `UserId`, price, or batch.
- **Permissions**: the administrator manages the catalog and inventory; the employee browses and sells.
- **Cost hidden from employees**: `ProductResponse.Cost` is `decimal?`; `GetProductsUseCase`/`GetProductByIdUseCase` return `null` unless `ICurrentUser` is an administrator. The create/update endpoints are admin-only and always include it.
- No comments in the code, except for the Tasks module and the EF-generated migrations.

Deliberately out of scope: microservices, CQRS/MediatR, Event Sourcing, Redis, brokers, Outbox, and Kubernetes.

## Tasks module and critical use of GenAI

The Tasks module (`UserTask`, its use cases, repository, controller, and `features/tasks`) is the one documented with comments explaining how GenAI was used and what was reviewed in its output. Critical review of what was generated:

- **Naming**: the original stub compiled against the BCL's `TaskStatus` instead of the project's own enum. It was renamed and documented.
- **Data ownership**: the repository offers no "get by id" without a user, so no use case can forget the owner filter. Tests prove this across all four layers, including for administrators.
- **State machine**: lives only in the aggregate (`Pending → InProgress → Completed`). The UI only uses it to decide which buttons to show.
- **Errors found during review**: validation attributes on records declared with `property:` (MVC rejects these and returned a 500, caught by an integration test), a FEFO test with a wrong premise, fakes that didn't simulate rollback, and a transitive package with a known vulnerability (`System.Security.Cryptography.Xml`) that was pinned to a patched version.

See [`docs/genai-demonstration.md`](docs/genai-demonstration.md) for the full write-up: the actual prompt used, a representative code sample, how the output was validated, and how each of the issues above was found and corrected.

## Further documentation

- [`docs/requirements.md`](docs/requirements.md) — functional and non-functional requirements, and how each one was turned into a use case and a test (TDD traceability).
- [`docs/technical-presentation.md`](docs/technical-presentation.md) — architecture, design choices, flow diagrams, and a demo script for the technical panel.
- [`docs/genai-demonstration.md`](docs/genai-demonstration.md) — the critical-use-of-GenAI worked example described above.
- [`docs/seed.md`](docs/seed.md) — seed data and the business scenarios it covers.
