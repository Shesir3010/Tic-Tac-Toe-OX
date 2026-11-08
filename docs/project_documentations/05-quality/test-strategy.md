# Test Strategy

*How the Tic Tac Toe backend is verified: one NUnit + Moq + Shouldly unit suite in `test/TicTacToe.Application.UnitTests` covering the engine, handlers, store and invitation managers — fast, containerless, and run on every push ([ADR-0008](../01-architecture/adr/0008-nunit-moq-shouldly-testing.md)).*

Parent document: [PRD](../00-product/PRD.md) · Related: [CQRS & Mediator](../01-architecture/cqrs-and-mediator.md) · [Security](security.md) · Decision records: [ADR-0008](../01-architecture/adr/0008-nunit-moq-shouldly-testing.md) · [ADR-0015](../01-architecture/adr/0015-vertical-slices-and-unit-only-tests.md)

---

## 1. Principles

1. **One project, one runner, one assertion library**: `test/TicTacToe.Application.UnitTests`, NUnit 4, Shouldly — no other test project exists (ADR-0008). 
2. **The suite is pure unit**: no database, no network, no Docker, no server — it runs on any machine with the .NET SDK, and in CI in seconds.
3. **Domain rules are the crown jewels**: the engine invariants E1–E6 ([ADR-0006](../01-architecture/adr/0006-pure-game-engine-in-domain.md)) are exercised exhaustively (all 8 win lines, draw, finish immutability, forgery replay).
4. **Handlers are tested through their seams**: repositories and services are **Moq** substitutes; expected failures are `OperationResult` errors, never exceptions (ADR-0010).
5. **Tests follow the feature folders**: a handler's test sits next to its slice under `Application/Features/{Area}/{Feature}/…Tests` (ADR-0015) — mirror test trees, not a flat pile.

## 2. Tools

| Tool | Role |
| --- | --- |
| **NUnit 4** | Framework: `[Test]`, `[TestCase]`, `[SetUp]` |
| **Shouldly** | Sole assertion library: `ShouldBe`, `ShouldThrow`, `ShouldContain` — no raw `Assert.*` chains |
| **Moq** | Substitutes for `IUnitOfWork`, repositories, `IRoomStore`, `IPresenceTracker`, `IInvitationManager`; `Verify` / `VerifyNoOtherCalls` |
| **Autofac.Extras.Moq (AutoMock)** | Handler-heavy tests only: plain constructor dependencies get auto-mocked; blank usage obscures plain dependencies |

Excludes (deliberate): **NSubstitute** (Moq is our substitute), **FluentValidation.TestHelper** (validators are plain `IValidator<T>` — no extra package, tested via the handler tests).

### 2.1 Standard test class structure

```csharp
using Autofac.Extras.Moq;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace TicTacToe.UnitTests.Application.Features.Game.Moves;

[TestFixture]
public class MakeOnlineMoveCommandHandlerTests
{
    private AutoMock _mock;
    private MakeOnlineMoveCommandHandler _handler;

    private Mock<IRoomStore> _mockRoomStore;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        _mock = AutoMock.GetLoose();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _mock?.Dispose();
    }

    [SetUp]
    public void Setup()
    {
        _mockRoomStore = _mock.Mock<IRoomStore>();

        _handler = _mock.Create<MakeOnlineMoveCommandHandler>();
    }

    [TearDown]
    public void TearDown()
    {
        _mockRoomStore?.Reset();
    }

    [Test]
    public async Task Handle_ValidMove_ReturnsSuccess()
    {
        // Arrange — prepare the command and configure dependencies.

        // Act — execute the handler.

        // Assert — verify the result and expected interactions.
    }
}
```

### 2.2 Arrange – Act – Assert (AAA)

Every test follows three commented sections.

**Arrange** — prepare input data and configure mocked dependencies:

```csharp
var roomId = Guid.NewGuid();
var command = new MakeOnlineMoveCommand(roomId, 4);

_mockRoomStore
    .Setup(x => x.ApplyAsync(roomId,
        It.IsAny<Func<Room, Task<RoomInfo>>>(),
        It.IsAny<CancellationToken>()))
    .ReturnsAsync(new RoomInfo(Finished: false, Board: new Board(...)))
    .Verifiable();
```

**Act** — execute the method being tested:

```csharp
var result = await _handler.Handle(command, default);
```

**Assert** — verify the expected result:

```csharp
result.IsSuccess.ShouldBeTrue();
result.Value.ShouldNotBeNull();
```

### 2.3 Mocking conventions

Use Moq to configure dependencies (setup, `SetupGet`, callback, verify):

```csharp
_mockUnitOfWork
    .SetupGet(x => x.MatchRepository)
    .Returns(_mockMatchRepository.Object)
    .Verifiable();

Match? savedRow = null;
_mockMatchRepository
    .Setup(x => x.AddAsync(It.IsAny<Match>(), It.IsAny<CancellationToken>()))
    .Callback<Match, CancellationToken>(
        (match, _) => savedRow = match)
    .Returns(Task.CompletedTask)
    .Verifiable();

_mockMatchRepository.VerifyAll();  // when interaction verification is relevant
```

### 2.4 Assertion conventions

Use Shouldly for assertions (`ShouldBe`, `ShouldBeTrue`, `ShouldBeFalse`, `ShouldBeNull`, `ShouldNotBeNull`). Multiple related assertions belong together (`ShouldSatisfyAllConditions`); expected asynchronous exceptions — **unexpected failures only** (expected business failures are `OperationResult` values, `Should.ThrowAsync<InvalidOperationException>` is reserved for boundary/unexpected cases):

```csharp
this.ShouldSatisfyAllConditions(
    () => result.IsSuccess.ShouldBeTrue(),
    () => savedRow.ShouldNotBeNull(),
    () => _mockMatchRepository.VerifyAll()
);
```

### 2.5 Test isolation

- Never connect to a real database; never make real network requests; mock external dependencies.
- Each test is independent, order-free and deterministic (seeded `IRandomSource`, fake `TimeProvider` for time-dependent logic).

## 3. Layout (follows `src/TicTacToe.Application/Features`)

```text
test/TicTacToe.Application.UnitTests/
  Engine/GameEngineTests.cs                              # E1–E6 exhaustive rule book
  Common/OperationResultTests.cs                         # factories, error codes, messages
  Application/Features/
    Game/Moves/PlayMoveCommandHandlerTests.cs
    Matches/History/SaveMatchCommandHandlerTests.cs
    Matches/History/GetHistoryQueryHandlerTests.cs
    Rooms/Invitations/SendInvitationCommandHandlerTests.cs
    Rooms/Invitations/RespondToInvitationCommandHandlerTests.cs
    Rooms/Invitations/CancelInvitationCommandHandlerTests.cs
    Rooms/Invitations/GetOnlinePlayersQueryHandlerTests.cs
    Rooms/Online/MakeOnlineMoveCommandHandlerTests.cs
    Rooms/Online/LeaveRoomCommandHandlerTests.cs
    Rooms/Online/RequestRematchCommandHandlerTests.cs
    Account/Profile/ChangePasswordCommandTests.cs
  Infrastructure/
    InMemoryRoomStoreTests.cs                            # lock serialization, transitions
    InvitationManagerTests.cs                            # guards, TTL, cooldown (fake time)
    RoomCleanupServiceTests.cs                           # forfeit / orphan / expiry sweep
  Architecture/ArchitectureTests.cs                      # R-A1…R-A5 dependency rules
```

Naming: `<Name>CommandHandlerTests` / `<Name>QueryHandlerTests` / `<Thing>Tests` for domain/store logic. **Test method naming convention: `MethodName_Scenario_ExpectedResult`** — examples: `Handle_ValidMove_ReturnsSuccess`, `Handle_InvalidMove_ReturnsFailure`, `Handle_DuplicateInvite_ReturnsAlreadyPending`, `DeleteAccount_ActiveRoom_ForfeitsFirstThenDeletes`.

## 4. Engine rule tests (`Engine/GameEngineTests.cs`)

The engine is exercised directly — no mocks, real domain objects, deterministic (`IRandomSource` seeded):

| Invariant | Covered |
| --- | --- |
| E1 legality | index 0–8 bounds, occupied cell, mover vs turn — each with its exact failure |
| E2 alternation | X-first, strict alternation, no double-move by one symbol |
| E3 win lines | **All 8 winning lines** (`[TestCase]` per line), O-wins path included |
| E4 draw | Verified draw board: 9 moves, no winner |
| E5 immutability | Finished game rejects any further move |
| E6 determinism | Same state ⇒ same result (Evaluate twice, same output) |
| Forgery replay | `SaveMatchCommand` re-runs the engine over a move list; a tampered claimed result → `Game.ForgedResult`, no row written |
| AI | Easy stays legal on the seeded random source; Hard always at least a draw over the exhaustive sweep |

Gate: the engine must reach **100% line coverage** ([M1](../00-product/roadmap.md)).

## 5. Handler tests (per use case, fresh substitutes each test)

| Test cluster | Asserts |
| --- | --- |
| `PlayMove_*` | Legality → `OperationResult` error codes (`Game.*`); correct DTO shape |
| `SaveMatch_TamperedResult_ReturnsForgedResult` | No row written; error code verbatim |
| `SendInvitation_*` | Guard codes (`Invite.Self/Offline/Busy/Cooldown/AlreadyPending`); only one outgoing |
| `RespondToInvitation_Accept_CreatesRoomAtomically` | Room created exactly once; invitation terminal; inviter = X |
| `MakeOnlineMove_WinningMove_SavesBothRowsOnce` | Single commit, both rows present |
| `LeaveRoom_AfterGrace_ForfeitsToOpponent` | Result = Forfeit; rows saved once |
| `RequestRematch_OnePlayer_*` | Both-ready rule; board reset with X to start |
| `GetOnlinePlayers_StatusTable` | Available/Invited/In game derivation |
| `ChangePassword_WrongCurrent_ReturnsWrongPassword` | Exact error code |
| `DeleteAccount_InActiveRoom_ForfeitsFirst` | Leave semantics run, then the delete executes |
| `InMemoryRoomStore_TwoWritersSameRoom` | Exactly one coherent winner; the loser gets a coherent state (per-room lock) |
| `InvitationManager_ExpiryAndCooldown` | Fake `TimeProvider`: TTL fires once; cooldown re-arms after decline |
| `RoomCleanupService_*` | Forfeit sweep, orphaned close without saves, expired invitations — each fake-time traveled |

**Concurrency note:** the lock-based serialization (ADR-0013) is unit-tested per store/manager above with fake time; its real-connection manifestation is verified by the documented **manual two-browser walkthrough scripts** ([test-plan.md](test-plan.md) §concurrency), executed at milestone gates and the M7 sweep.

**Coverage checklist (per use case — no meaningless tests):** every slice covers (1) valid input + success, (2) invalid input, (3) missing data, (4) business-rule violations, (5) boundary conditions, (6) dependency interactions where relevant — expected business failures assert `OperationResult` codes, never exceptions.

## 6. Validators

Registration/login/account shapes (username rules, password policy, email shape) are validated in-handler (`IValidator<T>` run as the first handler statement — [cqrs-and-mediator](../01-architecture/cqrs-and-mediator.md) §3.2). Validator behavior is covered **indirectly through handler tests** (invalid input → `Validation.Error` / `Auth.*` codes) — a validator test helper package is deliberately not added; validator rules stay reviewable in the slice.

## 7. CI

`.github/workflows/dotnet.yml` — job: restore → build (warnings as errors) → `dotnet test test/TicTacToe.Application.UnitTests`. **Backend only**: there is no npm frontend job (no npm in this project) and no Docker test step.

## 8. Frontend (client)

No automated client suite — deliberate (the client has no bundler and no build step). Frontend behaviour is verified by: the documented **manual walkthrough scripts** (login, invite accept/reject, two-browser races, disconnect/forfeit, delete-account-in-room — [test-plan.md](test-plan.md)), executed at every milestone gate and the **M7 verification sweep**; results are recorded — an unrecorded script counts as *not passed* ([ADR-0008](../01-architecture/adr/0008-nunit-moq-shouldly-testing.md)). A client test harness (if ever needed) would arrive via a new ADR, never a silent add.

## 9. Definition of Done

- Code reviewed and merged via pull request.
- `dotnet build TicTacToe.slnx` clean (warnings as errors) and the unit suite green.
- New use cases ship with: command/query + handler **and their tests** in the matching `Features/{Area}/{Feature}/` folder.
- Test structure: AAA followed, `MethodName_Scenario_ExpectedResult` names, Moq `Verifiable()/VerifyAll()` where interactions matter, Shouldly assertions.
- Engine tests exercise the real rules (no mocks, no DB, no infrastructure) — covering every win line, draw, illegal move, AI decisions and the exhaustive Hard sweep.
- New error codes added to `02-backend/error-handling.md` and the catalog tests in the same change.
- Structured logs for new paths; **no secrets, passwords, tokens or full emails logged**.
- No existing tests weakened or deleted to make the suite pass; public behaviour tested (never private methods); production logic is never duplicated inside tests.

## Assumptions

- The unit suite is the **only** automated floor; integration and E2E suites were owner-removed (2026-11-07) and return only via a new ADR with the "same bug escapes twice" trigger.
- Coverage monitoring is a signal, not a gate — the gate is the DoD checklist plus engine 100% coverage.
- AutoMock stays restricted to handler-heavy paths; blanket usage is flagged at review.
