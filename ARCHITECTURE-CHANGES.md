# Architecture alignment — what changed and why

This records a pass made over the whole solution to bring it in line with the team's
.NET Clean Architecture handbook (sections 0–5). It is written for someone who knew the
codebase beforehand and wants to know what moved.

**Scope of the change:** no schema change, no new migration, no change to the public API's
wire format, and no change to the JSON shape of anything stored in `Audit.DomainEvents` or
queued in `cap.Published`. Everything below is a change to C# types and to where decisions
are made.

**Verified by:** a clean solution build (0 errors, 0 warnings); the app starting against the
local SQL Server and applying migrations with no pending model changes; the two public API
endpoints returning the same payloads as before (checked against temporary rows, since
removed); and a throwaway harness asserting the new domain invariants and that domain events
still serialise to the same flat JSON. There is no test project in this solution, so none of
this is guarded by automated tests — see *What is not covered* at the end.

---

## 1. Where the code already matched, and nothing was done

Worth stating, because most of the handbook was already being followed:

- Four layers with dependencies pointing inward only.
- Ports declared in the inner layer, adapters supplied by the outer one
  (`IEntraService`, `IReleaseNotificationSender`, `IReleaseEmailComposer`,
  `IReleaseNotesPublisher`, `ICurrentUserProvider`, the unit of work).
- CQRS through Mediator, one folder per use case, message + validator + handler together.
- A validation pipeline behaviour running ahead of every handler.
- `Result` / `Result<T>` returned for expected failures instead of thrown exceptions.
- Past-tense domain events carrying identifiers, raised by entities themselves, dispatched
  by the unit of work as part of the commit.
- Side effects (the announcement emails) happening after the commit, via a subscriber.
- Queries projecting into DTOs rather than loading aggregates.
- One exception handler at the edge of the host.

The gaps were all in the domain layer and in how cleanly the layers were separated.

---

## 2. A separate Domain project, and `Core` renamed to `Application`

**Before:** the domain model lived in `DemoReleaseChatPlugin.Core/Releases/Domain`, a folder
inside the application layer. Nothing stopped an entity from taking a dependency on EF Core,
Mediator or FluentValidation — all three are referenced by that project — except the reviewer
noticing.

**After:** a new `DemoReleaseChatPlugin.Domain` project holds the aggregates, value objects
and domain events. It references only `DoverPort.Common.Models` (see §16), which itself has no
dependencies at all. The dependency rule is now enforced by the compiler rather than by
convention: adding `using Microsoft.EntityFrameworkCore;` to an aggregate does not build.

Namespaces moved with it:

| Before | After |
|---|---|
| `DemoReleaseChatPlugin.Application.Releases.Domain` | `DemoReleaseChatPlugin.Domain.Releases` |
| `DemoReleaseChatPlugin.Application.Releases.Domain.Events` | `DemoReleaseChatPlugin.Domain.Releases.Events` |
| `DemoReleaseChatPlugin.Application.Releases.Domain.Enums` | `DemoReleaseChatPlugin.Domain.Releases.Values` |
| `DemoReleaseChatPlugin.Application.Authorization.Entities` | `DemoReleaseChatPlugin.Domain.Authorization` |

### `DemoReleaseChatPlugin.Core` → `DemoReleaseChatPlugin.Application`

The old name was ambiguous — in several popular Clean Architecture templates `Core` means the
*domain* project, whereas here it meant the application layer. That was defensible while the
project held both; once the domain moved out it pointed at the wrong thing.

Renamed: the folder, the project file, the `DemoReleaseChatPlugin.Core.*` namespace on every
file in it, the `using` lines across the solution, the `.sln` entry, the composition-root method
(`AddDemoReleaseChatPluginCore` → `AddDemoReleaseChatPluginApplication`), and the `.gitignore`
`[Rr]eleases/` exception. No logic changed. `ApplicationAssemblyMarker` — which was already
named after the layer — now sits in a project whose name agrees with it.

---

## 3. Strongly-typed identifiers

`ProjectId`, `ReleaseNoticeId`, `StakeholderGroupId` and `StakeholderId` replace raw `Guid`
throughout the domain, the commands, the queries and their DTOs.

The concrete thing this buys: `Rollback.Record(liveRelease, id, …)` and
`AddStakeholder(groupId, projectId, …)` could previously be called with the arguments the
wrong way round and compile cleanly — the mistake surfaced as a foreign-key violation, or
worse, as a row attached to the wrong parent. Now it is a build error.

Details worth knowing:

- They are records, so equality is by value. A Radzen dropdown bound to `ProjectId` works
  unchanged for that reason.
- There is a one-way implicit conversion **to** `Guid`, and deliberately none the other way.
  `Guid g = projectId;` compiles; `ProjectId p = someGuid;` does not. Conversion at an edge
  is explicit: `ProjectId.From(routeValue)`.
  - Known gap: because both sides convert to `Guid`, `projectId == stakeholderId` still
    compiles via `Guid.operator ==`. Argument-order mistakes — the common case — are caught;
    a direct cross-type comparison is not.
- `New()` returns a **version 7** GUID. Note this is *not* the "sequential key" win the usual
  advice promises: SQL Server orders `uniqueidentifier` by the last six bytes and v7 puts its
  timestamp in the first six, so clustered-index locality is no better than v4. It was adopted
  because the ids sort chronologically in application code and logs; the index-fragmentation
  argument in the handbook does not transfer to this database engine.
- Two places keep a raw `Guid` on purpose: `HistoryRecord.CorrelationId`, which holds a
  project id *or* a stakeholder-group id and so has no honest single type; and the
  `Contracts` package, which is netstandard2.0 and dependency-free and cannot see these types.
  The unwrapping to `Guid` for `Contracts` happens in memory after materialisation.

---

## 4. Closed vocabularies instead of enums

`RiskLevel`, `ReleaseEnvironment` and `NotificationAudience` became `ValueOptions<T>` smart
enums. `ReleaseNoticeKinds` — a class of `const string` — became a `ReleaseNoticeKind`
vocabulary.

All four already persisted as strings (`HasConversion<string>()` / a discriminator column),
so **the stored values are unchanged** and no migration was needed. What changed is that the
set is now closed (`(RiskLevel)47` is no longer representable), and behaviour can live with
the vocabulary — `ReleaseNoticeKind.HasAlreadyHappened`, `RiskLevel.NeedsProminentWarning`.

Because a vocabulary member is not a compile-time constant, a few `switch` expressions in the
Razor components changed from constant patterns to `when` guards. No magic strings were
introduced.

---

## 5. `EmailAddress` value object

`Stakeholder.Email` was a `string` normalised by a `Stakeholder.Normalise(...)` helper that
three separate validators had to remember to call — exactly the "validate everywhere" shape
the handbook warns about.

It is now an `EmailAddress` record with a private constructor and a validating
`Of(string) → Result<EmailAddress>` factory. Anywhere the code holds one, it is a trimmed,
lower-cased, well-formed address, with no re-checking, because there is no way to build one
that is not. The column is still `nvarchar(256)` holding the same normalised text.

The release *version* string was left as a string. `ReleaseVersion.TryCompare` already gives
it the behaviour that matters, and wrapping it would have touched the unique index and a
dozen query sites for very little.

---

## 6. Aggregates hardened

Applied to `Project`, `StakeholderGroup`, `Stakeholder` and the three release-notice kinds:

| Rule | What changed |
|---|---|
| Root is `sealed` | All are, except `ReleaseNotice` itself — it cannot be, the discriminator *is* the inheritance. Its three leaves are sealed. |
| No public setters | `protected set` → `private set` throughout. |
| Creation via a named factory | `Project.Create`, `ScheduledRelease.File`, `LiveRelease.Record`, `Rollback.Record`, `Stakeholder.For`. Public constructors are gone. `StakeholderGroup` already did this. |
| Collections read-only over a private list | Already true of `StakeholderGroup.Stakeholders`. |
| Other aggregates referenced by id | **Changed** — see below. |
| Guard clauses on mutating methods | **Changed** — see §7. |
| No `DateTime.Now`, `Guid.NewGuid()`, I/O | **Changed** — see §8. |

### Cross-aggregate navigation properties removed

`ReleaseNotice.Project`, `LiveRelease.ScheduledRelease` and `Rollback.RollbackOfRelease` each
pointed at a different aggregate root. Any code holding a notice could reach through and
mutate a neighbouring aggregate in the same transaction.

They are gone. The foreign keys and their delete behaviours are unchanged — the EF
configuration now declares them with `HasOne<Project>()` and no navigation. Reads that want
the project name use a correlated sub-select instead of an `Include`; it is still one SQL
statement per query, and the announcement subscriber projects what it needs rather than
loading the aggregate.

---

## 7. Business invariants moved out of the validators

This was the largest behavioural gap. `CheckRule`, `IBusinessRule` and
`BusinessRuleValidationException` existed in `Shared` and were called **zero times**; every
rule, including ones about an aggregate's own state, lived in a FluentValidation validator —
which is skippable in a way an aggregate is not.

Rules that moved **into** the aggregate, each returning a `Result`:

| Rule | Now lives in |
|---|---|
| "This project is archived, so releases cannot be filed against it." | `Project.EnsureCanTakeReleaseNotices()`, called by both notice factories |
| "This project is already archived." / "not archived." | `Project.Archive` / `Project.Restore` |
| "An archived project's details cannot be changed." | `Project.UpdateDetails` — *this was not enforced anywhere before* |
| "People cannot be added to an inactive group." | `StakeholderGroup.AddStakeholder` — *not enforced before* |
| "This address is already in this group." | `StakeholderGroup.AddStakeholder` |
| "An inactive group's / stakeholder's details cannot be changed." | `UpdateDetails` on each — *not enforced before* |
| "Already active / already inactive." | `Deactivate` / `Reactivate` on both |
| "A release cannot be rolled back before it went live." | `Rollback.Record` — *not enforced before* |

Rules that **stayed** in validators, deliberately: anything whose answer lives in other rows,
because the aggregate cannot see them. That is uniqueness (project name, version already
scheduled), "has this release already been rolled back?" (the answer is a *different* row —
a `Rollback` pointing back at it), "does this scheduled notice belong to this project?", and
"is this version newer than the last release?". Plus all shape validation. This is the
handbook's own split: input validation before the domain, state-dependent invariants inside it.

Handlers are correspondingly thinner — they now load, invoke and relay whatever the aggregate
says, with no business `if` of their own.

**Behaviour note:** the new "already in that state" guards mean a double-click on an
archive/deactivate toggle now produces a message rather than a silent no-op.

---

## 8. The clock is a dependency

`IClock` / `SystemClockAdapter` were added to `Shared` and registered in DI. Every domain
factory and mutating method takes one. `DemoReleaseChatPlugin.Domain` now contains **no** call
to `DateTime.Now` or `Guid.NewGuid()` — ids come from the application layer
(`ProjectId.New()` in the handler), timestamps from the injected clock.

`Entity.AddDomainEvent` gained an overload taking the clock, so an event's `OccurredOn` is
stamped from it rather than read from the ambient clock in the event's own initialiser. That
timestamp ends up in `Audit.DomainEvents` and in the history timeline, so it needed to be as
controllable in a test as the rest of the entity.

`SystemClockAdapter` delegates to the existing `SystemClock` static, so anything relying on
`SystemClock.Set(...)` keeps working.

---

## 9. Reads and writes use different ports

**Before:** `IDemoReleaseChatPluginDbContext` exposed `DbSet<T>` and `SaveChangesAsync` to
everything. "Queries never call `CommitAsync`" and "queries use `AsNoTracking`" were
conventions enforced by review.

**After:** `IDemoReleaseChatPluginReadDb` exposes `IQueryable<T>` only — no `Add`, no
`SaveChangesAsync` — and every set is already `AsNoTracking`. Query handlers and the read-only
rules inside validators take it; command handlers take the write context and the unit of work.

Two things this makes impossible rather than merely discouraged: a query writing, and a read
accidentally pulling an aggregate into the change tracker where an unrelated `CommitAsync`
later in the same request would save it.

The read adapter shares the request's `DbContext`, so there is still one connection and one
transaction per request.

---

## 10. `QueryResult<T>`: "not found" is an answer

Single-item reads return `QueryResult<T>`, which has three states — found, legitimately not
there, and the read failed — instead of squeezing "not found" into `Result<T>`'s failure state.

The presentation layer now decides what missing means:

- `ManageProject` shows an empty state for a stale link instead of an error dialog.
- The stakeholder group dialog says "not found" and closes.
- The public API returns **404** for an unknown application and **500** for a broken read.
  Previously every non-success became a 404, so a failing read told a consumer its application
  name was wrong.

Converted: `GetUpdateProjectQuery`, `GetStakeholderGroupQuery`,
`GetProjectStakeholderGroupsQuery`, `GetUpdateStakeholderGroupQuery`,
`GetUpdateStakeholderQuery`, `GetUpdateUserRolesQuery`, `GetApplicationReleasesQuery`,
`GetLatestApplicationReleaseQuery`. List queries were left on `Result<T>` — an empty list is
already a complete answer.

Every call site was audited: `IsFailure` is now `false` for a not-found, so anything that only
checked `IsFailure` before reading `.Value` would have dereferenced a null. All of them now
check `IsNotFound` first.

---

## 11. The validation pipeline returns instead of throwing

**Before:** `ValidationBehaviour` threw `ValidationException`. Consequences the codebase was
designed around: every page had to call `IsValidAsync(Validator)` before `Mediator.Send` or
get an unhandled exception, and the one place that could not — Radzen's inline grid editing —
wrapped `Mediator.Send` in `try/catch`.

**After:** it short-circuits with a failed `Result` / `Result<T>` / `QueryResult<T>`, carrying
the combined messages. A blank required field is a normal thing for a user to do, so it travels
back as a value on the response the caller was already inspecting.

- Call sites that already read `.IsFailure` and `.Message` needed no change.
- The two `try/catch (ValidationException)` blocks in `StakeholderGroupDialogBase` are gone —
  each method now has one path out instead of two.
- `IsValidAsync` is still called on every form, but it is now a **UX choice** (per-field
  messages beat one combined sentence in a dialog), not a crash guard.
- The failure log dropped from Warning to Information: an expected outcome is not a fault.
- A request whose response type has no failure state to carry the message still throws. There
  are none today; the branch exists so adding one fails loudly rather than silently dropping
  the result.

---

## 12. Persistence: value converters, no schema change

`Infrastructure/Persistence/Configurations/Common/ValueConverters.cs` maps the new types to the
columns they were always stored in — typed id ↔ `uniqueidentifier`, vocabulary ↔ the same
`nvarchar` string, `EmailAddress` ↔ `nvarchar(256)`. Value comparers are declared explicitly so
EF does not treat a reloaded reference type as "changed" on every load.

`dotnet ef migrations has-pending-model-changes` reports **no changes**, and the model snapshot
was regenerated to match. There is still exactly one migration.

The trade-off to know about: a converted property is opaque to the SQL translator. Comparing
it (`x.Id == someProjectId`) or projecting it whole (`Id = x.Id`) works; reading *inside* it
in a query (`x.Email.Value`, `x.RiskLevel.ToString()`) does not. Queries needing the raw string
project the value object out and unwrap in memory — `GetUpdateStakeholderHandler` and
`ApplicationReleases.LoadAsync` both do this, with a comment saying why.

### One bug found and fixed during the work

The first pass made `StakeholderGroup.ProjectId` and `Stakeholder.ProjectId` non-nullable
reference types, and EF scaffolded a migration turning both columns `NOT NULL`. That would have
broken mandatory (organisation-wide) groups entirely, which are identified by `ProjectId IS
NULL`, and the `HasFilter(null)` unique-index trick that depends on it. They are now declared
`ProjectId?`, with a comment saying the `?` is load-bearing. Same for
`LiveRelease.ScheduledReleaseId`.

---

## 13. Serialisation compatibility

Typed ids and vocabularies are inside domain events, which are serialised twice — into
`Audit.DomainEvents.Details`, an append-only table, and into CAP message bodies that may
already be sitting in `cap.Published` when a new build starts.

Left alone, `ProjectId` would have serialised as `{"Value":"…"}` instead of `"…"`, changing the
audit format and breaking in-flight messages. `GuidIdJsonConverterFactory` and
`ValueOptionsJsonConverterFactory` keep them as bare strings. They are registered in
`ReleaseSerializerOptions` (used by the unit of work), added to CAP's own options at startup,
**and** applied as `[JsonConverter]` attributes on each concrete type as a safety net.

Verified: a `ProjectCreated` event serialises to
`{"ProjectId":"1111…","Name":"Payments","Id":"…","OccurredOn":"…","OccurredBy":"…"}` — the same
shape as before — and round-trips back to the same typed id.

---

## 14. Files added

```
DemoReleaseChatPlugin.Domain/                                   (new project)
  Releases/Values/Identifiers.cs                                typed ids
  Releases/Values/ReleaseVocabularies.cs                        closed vocabularies
  Releases/Values/EmailAddress.cs                               value object
  Releases/… aggregates and events, moved from Core

DemoReleaseChatPlugin.Shared/Common/Models/
  IClock.cs                                                     clock port + adapter
  QueryResult.cs                                                found / not found / failed
  ValueObjects/GuidId.cs                                        typed-id base + JSON converter
  ValueObjects/ValueOptions.cs                                  smart-enum base + JSON converter
DemoReleaseChatPlugin.Shared/Common/Pipeline/Internal/
  ValidationFailureResponse.cs                                  builds the failure response

DemoReleaseChatPlugin.Application/Releases/Persistence/
  IDemoReleaseChatPluginReadDb.cs                               the read port

DemoReleaseChatPlugin.Infrastructure/Persistence/
  DemoReleaseChatPluginReadDb.cs                                read adapter
  ReleaseSerializerOptions.cs                                   shared JSON options
  Configurations/Common/ValueConverters.cs                      value-object ↔ column mapping
```

---

## 15. What is **not** covered

Stated plainly, because the change is large and the safety net is thin:

- **There are no automated tests.** The solution has no test project and one was not added —
  that was outside what was asked for. Verification was a clean build, a successful start
  against the real database, the public API returning unchanged payloads, and a throwaway
  harness (since deleted) covering the new invariants and the serialisation shape.
- **The authenticated UI was not exercised.** Signing in needs a real Entra app registration,
  so the Blazor write paths — filing a notice, editing a stakeholder group, the inline grid —
  were verified by compilation and by reading, not by clicking. The inline stakeholder grid is
  the one to try first: its email column now binds to a new `GroupStakeholderDto.EmailText`
  rather than to `Email` directly.
- **The email and CAP paths were not run end to end.** No message was actually published and
  consumed. The serialisation shape was verified directly; the dispatch was not.
- **The `==` gap on typed ids** described in §3.
- ~~`DemoReleaseChatPlugin.Domain` pulls in ASP.NET Core and `Microsoft.Identity.Web` through
  the shared project.~~ **Fixed** — see §16.

---

## 16. `Shared` split into the four packages it stands in for

The old `DemoReleaseChatPlugin.Shared` was one project reproducing **four** internal Port of Dover
NuGet packages. Because it was one project, every consumer got all four — so
`DemoReleaseChatPlugin.Domain`, which wants nothing but `Entity<T>` and `Result`, also had an
ASP.NET Core `FrameworkReference`, `Microsoft.Identity.Web`, `Newtonsoft.Json`, FluentValidation
and Mediator in its reference graph.

It is now four projects under `Shared/`, named after the packages they replace:

| Project | Package references | Referenced by |
|---|---|---|
| `DoverPort.Common.Models` | **none** | Domain, Application, Infrastructure, Website |
| `DoverPort.Common.Pipeline` | ASP.NET Core, Mediator, FluentValidation, Newtonsoft, `Common.Models` | Application |
| `DoverPort.Emails` | **none** | Infrastructure |
| `DoverPort.AspNetCore.Common` | ASP.NET Core, `Microsoft.Identity.Web` + the two security pins | Website |

**Namespaces are untouched** — `DoverPort.Common.Models`, `DoverPort.Common.Pipeline`,
`DoverPort.Emails`, `DoverPort.AspNetCore.Common`, exactly as the real packages declare them. Not
one `using` line in the solution changed. That is the point: on a machine with access to the
internal feed, each project is deleted and its `<ProjectReference>` becomes a `<PackageReference>`
with the same name, and nothing else moves.

Measured result for the domain project:

```
$ dotnet list DemoReleaseChatPlugin.Domain/DemoReleaseChatPlugin.Domain.csproj package --include-transitive
   [net10.0]: No packages were found for this framework.

$ ls DemoReleaseChatPlugin.Domain/bin/Debug/net10.0/*.dll
DemoReleaseChatPlugin.Domain.dll
DoverPort.Common.Models.dll
```

Zero NuGet packages, direct or transitive. The domain now builds against the BCL and one
dependency-free library — which is what "the domain knows about nothing" is supposed to mean.

The two transitive security pins (`System.Security.Cryptography.Xml`,
`Microsoft.AspNetCore.DataProtection` at 10.0.11) moved to `DoverPort.AspNetCore.Common`, the only
project that now references `Microsoft.Identity.Web`. The Website still picks them up through it,
exactly as it did through the old single project, so the restore graph is unchanged.

`Shared/README.md` states the rule for anyone adding to these projects: anything added to
`DoverPort.Common.Models` lands in the domain's reference graph, so it must stay dependency-free.
