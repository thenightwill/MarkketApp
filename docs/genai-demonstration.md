# Critical Use of GenAI — Worked Example (Tasks Module)

The brief for this project asks for "a demonstration of the critical use of GenAI." This document
is that demonstration: the actual prompt used, a representative sample of what the tool produced,
and — the part that matters most — how the output was validated, what was wrong with it, and how
it was corrected before it shipped.

**Tool used:** Claude Code (Anthropic), working directly in this repository across a multi-turn
session. The Tasks module (`UserTask`, its use cases, repository, controller and the Angular
`features/tasks` folder) was designated as the module that carries this demonstration — it is the
only part of the codebase that keeps inline comments, specifically to narrate this process. Every
claim below is verifiable against the code and tests in this repository, not a hypothetical.

Related documents: [`README.md`](../README.md), [`docs/requirements.md`](./requirements.md)
(functional/non-functional requirements and TDD traceability),
[`docs/technical-presentation.md`](./technical-presentation.md) (architecture and flows).

## 1. The prompt

The module wasn't generated from one isolated "build me a Tasks API" message — it was built inside
a longer conversation that had already established the architecture, the domain conventions and
the TDD workflow for the rest of the app (Products, Inventory, Sales). The prompt below is the
representative, reconstructed version of the instruction that actually shaped this module: it
carries the same constraints, the same acceptance criteria and the same TDD requirement that
governed the real session, phrased as a single self-contained request.

```text
We're building a Supermarket API in .NET 10 with Clean Architecture (Domain, Application,
Infraestructure, WebAPI) and MSTest, following strict TDD. Add the Tasks module.

Existing conventions to follow:
- Domain entities are immutable-looking: private setters, a static Create() factory, no public
  setters. Domain rules throw DomainException(code, message) with a stable error code from
  DomainErrorCodes.
- Application use cases are one class per operation (CreateXUseCase, GetXUseCase, ...), injected
  with repository interfaces and IUnitOfWork, and throw AppException.NotFound/.Forbidden/.Conflict
  with a stable code from AppErrorCodes.
- Controllers are thin: [Authorize], delegate straight to a use case, map the result to an HTTP
  status. The current user's id and role always come from ICurrentUser (backed by the JWT), never
  from the request body.
- Every request DTO is a C# record with DataAnnotations for shape validation; business rules live
  in the Domain, not in the controller or the DTO.

Business requirements for Tasks:
- A task belongs to exactly one user: Title, Description, Status, DueDate.
- Status starts at Pending and can only move Pending -> InProgress -> Completed. No skipping a
  step, no going backwards. An invalid transition is a domain error (INVALID_TASK_TRANSITION).
- A user must never be able to read, list, update or delete another user's task — not even an
  Administrator. This must be impossible by construction, not just checked in one place.
- Standard CRUD over HTTP: GET (list + by id), POST, PUT, DELETE, all under /api/tasks, all
  requiring authentication.

Follow TDD: write the failing test first (RED) for each rule above, at the lowest layer where
that rule actually lives (Domain test for a pure business rule, Application test with in-memory
fakes for orchestration, API test for the HTTP/ownership contract), then write the minimum code to
make it pass (GREEN). Do not write production code that isn't demanded by a test.
```

## 2. A representative sample of the output

Two files, shown in full because together they're the crux of the module: the aggregate that owns
the state machine, and the repository interface that makes ownership impossible to bypass.

`Domain/Entities/UserTask.cs` (as generated, now shipped unchanged in that respect):

```csharp
public class UserTask
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = null!;
    public string Description { get; private set; } = string.Empty;
    public UserTaskStatus Status { get; private set; }
    public DateTime DueDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private UserTask() { }

    public static UserTask Create(Guid userId, string title, string? description, DateTime dueDate)
    {
        if (userId == Guid.Empty)
            throw new DomainException(DomainErrorCodes.InvalidTask, "A task must belong to a user.");

        ValidateTitle(title);

        return new UserTask
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title.Trim(),
            Description = description?.Trim() ?? string.Empty,
            Status = UserTaskStatus.Pending,
            DueDate = dueDate,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Start() => Move(UserTaskStatus.Pending, UserTaskStatus.InProgress);

    public void Complete() => Move(UserTaskStatus.InProgress, UserTaskStatus.Completed);

    public void TransitionTo(UserTaskStatus target)
    {
        if (target == Status)
            return;

        switch (target)
        {
            case UserTaskStatus.InProgress: Start(); break;
            case UserTaskStatus.Completed: Complete(); break;
            default: throw InvalidTransition(target);
        }
    }

    public bool IsOwnedBy(Guid userId) => UserId == userId;

    private void Move(UserTaskStatus expectedCurrent, UserTaskStatus target)
    {
        if (Status != expectedCurrent)
            throw InvalidTransition(target);

        Status = target;
        UpdatedAt = DateTime.UtcNow;
    }

    private DomainException InvalidTransition(UserTaskStatus target) =>
        new(DomainErrorCodes.InvalidTaskTransition, $"A task cannot move from {Status} to {target}.");

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException(DomainErrorCodes.InvalidTask, "Task title is required.");
    }
}
```

`Application/Interfaces/Persistance/IUserTaskRepository.cs`:

```csharp
public interface IUserTaskRepository
{
    Task<UserTask?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserTask>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(UserTask task, CancellationToken cancellationToken = default);
    void Remove(UserTask task);
}
```

The one design choice worth calling out on first read: `GetByIdAsync` takes `userId` as a
**required** parameter. There is no overload that fetches a task by id alone. That's deliberate —
see §4.

## 3. How the output was validated

Every rule in the prompt was turned into a failing test before any production code existed for it,
then confirmed to compile and pass once implemented — the same RED → GREEN discipline used for the
rest of the app (see [`docs/requirements.md` §3](./requirements.md#3-from-requirement-to-use-case-to-test--the-methodology)
for the general methodology). For Tasks specifically:

| Layer | What it proved | Evidence |
|---|---|---|
| Domain (`Domain.Tests/TaskTests.cs`) | The state machine's every legal and illegal transition, title validation, ownership check | `TransitionTo_ShouldFollowTheAllowedSequence`, `TransitionTo_ShouldRejectSkippingInProgress`, `TransitionTo_ShouldRejectGoingBackwards`, `TransitionTo_ShouldBeNoOpWhenStatusDoesNotChange`, `IsOwnedBy_ShouldOnlyMatchTheOwner` |
| Application (`Application.Tests/UserTaskUseCasesTests.cs`) | The use cases assign the owner from `ICurrentUser` (not the request), and never expose another user's task — including to an Administrator | `Create_ShouldAssignTheTaskToTheCurrentUser`, `List_ShouldNotExposeTasksToAdministratorsEither`, `Get_ShouldReturnNotFoundForTasksOfOtherUsers` |
| Infrastructure (`Infraestructure.Tests/PersistenceTests.cs`) | The EF Core repository enforces the same ownership filter against a real SQL Server, not just an in-memory fake | `Task_RepositoryShouldNeverReturnTasksOfAnotherUser` |
| API (`Api.Tests/TasksApiTests.cs`) | The full HTTP contract: a `userId` sent in the body is ignored, illegal transitions return `409`, foreign tasks return `404` through real JWTs over Kestrel | `Post_ShouldCreateAPendingTaskOwnedByTheCaller`, `Put_ShouldRejectSkippingInProgressAndKeepTheTaskUntouched`, `GetPutDelete_ShouldReturnNotFoundForTasksOfOtherUsers`, `Get_ShouldNotLetAdministratorsReadATaskOfAnEmployee` |

42 tests across those four files exercise this module alone (18 Domain + 11 Application + 2
Infrastructure + 11 API; part of the 298 backend tests that
pass with `dotnet test MarketApp.slnx`). Validation wasn't "read the code and it looked right" —
it was "the suite was red until the rule was actually implemented, and it's still green now."

## 4. What was wrong with the output, and how it was corrected

Four real defects were caught this way during the build — not hypothetical "AI might get this
wrong" examples, but things that actually happened in this repository:

### 4.1 A naming collision the original stub was hiding

Before this module was written, the repository already had a `Domain/Entities/Task.cs` stub with
a `Domain/Enums/TaskStatus.cs` enum. With `<ImplicitUsings>enable</ImplicitUsings>`, a type or enum
named `Task`/`TaskStatus` in the `Domain` namespace silently shadows — or gets shadowed by —
`System.Threading.Tasks.Task`/`TaskStatus` depending on `using` order. The stub compiled, but not
provably against its own enum. Generating the full module surfaced this immediately: any method
returning `Task<TaskStatus>` was ambiguous about which `Task` it meant. **Fix:** renamed the entity
and enum to `UserTask`/`UserTaskStatus`, and applied that rename consistently to every supporting
type (`IUserTaskRepository`, `UserTaskRepository`, DTOs, use cases, the Angular model) rather than
leaving a mix of `Task`- and `UserTask`-prefixed names — a name confusion the reviewer (the user of
this project) explicitly asked to eliminate everywhere, not just in the entity.

### 4.2 A validation attribute that compiled but crashed at runtime

The first version of the request records for every module (not only Tasks) used
`[property: StringLength(200)]` on record primary-constructor parameters, e.g.:

```csharp
public sealed record CreateUserTaskRequest(
    [property: StringLength(200)] string Title,
    [property: StringLength(2000)] string? Description,
    DateTime DueDate);
```

This compiles cleanly. It only fails when the endpoint is actually called: ASP.NET Core's model
validator throws at request time —

```text
System.InvalidOperationException: Record type 'Application.Authentication.RegisterRequest' has
validation metadata defined on property 'Password' that will be ignored. 'Password' is a
parameter in the record primary constructor and validation metadata must be associated with the
constructor parameter.
```

— turning into a `500` for every POST/PUT across the API. Unit tests with in-memory fakes never
call ASP.NET Core's model binder, so this passed the whole Application test suite; it was only
caught by actually running the API and exercising it end to end (and later pinned down by the
`Api.Tests` integration suite). **Fix:** `[property: StringLength(...)]` → `[StringLength(...)]`
(the attribute on the constructor parameter itself, no `property:` target) across every request
DTO in the solution.

### 4.3 A test whose own premise was wrong

`SalesIntegrationTests.CreateSale_ShouldApplyFefoAcrossRealBatches` originally asserted that
selling 15 units from three real batches (10 + 20 + 30, ordered by expiration) would leave the
*second* batch fully drained too. That's arithmetically wrong for the seed used in that test (it
only needs 5 of the second batch's 20 units) — the assertion was written faster than it was
checked. Running the test against the real SQL Server container caught the mismatch immediately
with a clear `Assert.AreEqual` failure showing the actual remaining quantity. **Fix:** corrected
the expected values to `0 / 25 / 20` (fully drain the earliest batch, partially drain the second,
leave the third untouched) — the test's assertions were wrong, not the FEFO implementation.

### 4.4 Fakes that didn't simulate what they claimed to simulate

`FakeUnitOfWork.ConflictsToRaise`/`OnConflict` exists to simulate a concurrent sale winning a race
on inventory. Two tests (`ShouldRetryWhenAConcurrentSaleWonTheRaceButStockIsStillEnough`,
`ShouldReportInsufficientStockWhenTheConcurrentSaleTookTheStock`) failed on first run with `1`
sale persisted where `0` or `1` was expected at a different count — because the fake's
`OnConflict` callback mutated the stock quantity to simulate the other transaction's write, but
never cleared `_sales.Items`, so the "failed" attempt's sale row was still sitting in the fake
repository when the retry succeeded and added a second one. **Fix:** added `_sales.Items.Clear()`
to the `OnConflict` delegate in both tests, so the fake actually rolls back like a real aborted
transaction would.

### 4.5 A transitive dependency with a known CVE

Unrelated to Tasks specifically but caught the same way — by reading build output, not by
assuming the generated `.csproj` was safe: `dotnet build` reported `NU1903` warnings for
`System.Security.Cryptography.Xml 9.0.0`, pulled in transitively by
`Microsoft.EntityFrameworkCore.Design`, with eight linked GitHub advisories. **Fix:** pinned
`System.Security.Cryptography.Xml`, `Microsoft.EntityFrameworkCore.SqlServer/Design` and related
packages to the patched `10.0.12` line explicitly in `Infraestructure.csproj`, and re-ran the build
to confirm zero warnings.

None of these were caught by "reading the code carefully" alone — three of the four were only
visible by actually running something (the API, a real database, the full build) and watching it
fail.

## 5. Edge cases, authentication, and validation handled

- **Ownership isolation by construction, not by convention.** `IUserTaskRepository.GetByIdAsync`
  has no "fetch by id alone" overload. It is not possible to write a use case that accidentally
  loads someone else's task, because the type signature doesn't allow it. This is stronger than a
  code-review rule like "always filter by user" — there's nothing to forget.
- **404, not 403, for a foreign task.** Reading, updating or deleting a task that exists but
  belongs to someone else returns the same `404 TASK_NOT_FOUND` as a task that doesn't exist at
  all. This was a deliberate choice validated by
  `GetPutDelete_ShouldReturnNotFoundForTasksOfOtherUsers`: a `403` would confirm the id is valid
  and owned by somebody, which is itself information leakage.
- **Administrators get no special access.** The brief only requires that a user can't see other
  users' tasks; it was tempting to let Administrators see everything (as they can for Products,
  Inventory and Sales), but the requirement says "the user" without an exception, so
  `List_ShouldNotExposeTasksToAdministratorsEither` and
  `Get_ShouldNotLetAdministratorsReadATaskOfAnEmployee` pin down that Tasks has no admin override.
- **The user id never comes from the client.** `CreateUserTaskRequest` has no `userId` field at
  all — not optional, not ignored, simply absent from the DTO's shape — and
  `Post_ShouldCreateAPendingTaskOwnedByTheCaller` sends a `userId` in a raw JSON body anyway to
  prove the server-side owner (from `ICurrentUser`, backed by the JWT `sub` claim) is what actually
  gets persisted.
- **State machine edge cases.** Skipping a step (`Pending → Completed`), going backwards
  (`InProgress → Pending`), and re-submitting the current status (treated as a no-op, so a client
  resending an unchanged form doesn't trigger a false `409`) are each their own test — see §3.
- **Input validation is layered, not duplicated logic.** `[StringLength]` on the DTO catches
  malformed HTTP payloads before they reach application code (`400` via the standard ASP.NET Core
  model-validation pipeline); `UserTask.ValidateTitle` catches empty/whitespace titles that pass
  shape validation but violate the business rule (`400 INVALID_TASK`). Neither layer re-implements
  the other's job.
- **Authentication.** Every action requires `[Authorize]` (checked in `Api.Tests/AuthApiTests.cs`
  for the whole API, not re-tested per controller); `ICurrentUser.UserId`/`.Role` are resolved from
  JWT claims (`WebAPI/Security/CurrentUser.cs`), never trusted from anywhere else.
