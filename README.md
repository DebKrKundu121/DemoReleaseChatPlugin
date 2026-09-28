# DemoReleaseChatPlugin

Release management and administration, rebuilt from the specification in `ZenithChatPlugin`
(`Zenith` → `DemoReleaseChatPlugin` throughout).

- **Releases** — projects, stakeholder groups, release notices (scheduled / go-live / rollback),
  domain events, an audit trail, per-recipient email through a durable queue, and a public read API.
- **Administration** — database-backed users and roles, applied as claims at sign-in, with a
  staff-directory search for adding users.

## Projects

```
DemoReleaseChatPlugin.Domain      (aggregates, value objects, domain events - no EF, no
        ▲                          Mediator, no FluentValidation, no ASP.NET Core;
        │                          zero NuGet packages, direct or transitive)
DemoReleaseChatPlugin.Contracts   (netstandard2.0, dependency-free)
        ▲            ▲
        │            │
DemoReleaseChatPlugin.Application  DemoReleaseChatPlugin.Sdk
   (use cases, ports, CQRS)               ▲
        ▲                                 │
DemoReleaseChatPlugin.Infrastructure   DemoReleaseChatPlugin.RazorComponents
        ▲                                 ▲
        └────────┬────────────────────────┘
                 │
      DemoReleaseChatPlugin.Website   (the Blazor Server host)
```

Four layers, in order: **Domain** → **Application** → **Infrastructure** → **Website**. Each is a
separate project so the dependency rule is enforced by the compiler rather than by review — adding
`using Microsoft.EntityFrameworkCore;` to an aggregate does not build.

`Contracts`, `Sdk` and `RazorComponents` are not layers; they are what other applications consume.
Neither is `Shared/` — see below.

### `Shared/`

Not a layer. Four projects, each reproducing one internal `DoverPort.*` NuGet package that does not
exist outside the Port of Dover network, **under that package's original namespace**:

```
Shared/DoverPort.Common.Models/        Entity<T>, Result, QueryResult, Maybe, IClock,
                                       GuidId<T>, ValueOptions<T>   — no dependencies
Shared/DoverPort.Common.Pipeline/      AddPodPipeline() and the mediator behaviours
Shared/DoverPort.Emails/               PodEmailBuilder               — no dependencies
Shared/DoverPort.AspNetCore.Common/    ClaimsPrincipal extensions, globalisation settings
```

On a machine with access to the internal feed, each is deleted and its `<ProjectReference>` becomes
a `<PackageReference>` of the same name — which is why the project names *are* the package names.
Do not rename the namespaces for tidiness; the solution's `using DoverPort.*;` lines are written
against the real packages.

`DoverPort.Common.Models` must stay dependency-free: it is the only thing
`DemoReleaseChatPlugin.Domain` references, so anything added to it lands in the domain's reference
graph. `Shared/README.md` repeats this where someone editing those projects will see it.

## Before it will run

Two prerequisites are not satisfiable from source and must be supplied:

### 1. SQL Server  —  already set up on this machine

SQL Server has no native macOS build, so it runs as a Linux (x86-64) container under Rosetta.
Colima is used rather than Docker Desktop because it is MIT-licensed and needs no commercial
subscription.

Day to day you only need these:

```bash
colima start                      # after a reboot; the DB container restarts itself
docker ps                         # demoreleasechatplugin-sql should be Up
```

The container was created with:

```bash
colima start --vm-type vz --vz-rosetta --cpu 4 --memory 8 --disk 30
docker run -d --name demoreleasechatplugin-sql --platform linux/amd64 \
  -e ACCEPT_EULA=Y -e "MSSQL_SA_PASSWORD=<password>" -e MSSQL_PID=Developer \
  -p 1433:1433 --restart unless-stopped \
  mcr.microsoft.com/mssql/server:2022-latest
```

**The connection string lives in user-secrets, not in any committed file**, because it carries the
SA password. `appsettings.Development.json` still shows the Windows-auth form for reference, but
`Trusted_Connection=True` cannot work against SQL Server on Linux — SQL authentication is required:

```bash
dotnet user-secrets list --project DemoReleaseChatPlugin.Website
```

For `dotnet ef`, the design-time factory reads `DEMORELEASECHATPLUGIN_CONNECTION`:

```bash
export DEMORELEASECHATPLUGIN_CONNECTION="Server=localhost,1433;Database=DemoReleaseChatPlugin;User Id=sa;Password=<password>;TrustServerCertificate=True;Encrypt=False"
```

#### If you would rather not keep a container

Set the two values above to any reachable SQL Server — an Azure SQL Database works unchanged.

### (original notes) SQL Server

Set a reachable connection string. `appsettings.Development.json` currently points at
`Server=localhost`. LocalDB, Developer Edition or a container all work; Express is enough.

```bash
dotnet ef database update --project DemoReleaseChatPlugin.Infrastructure --startup-project DemoReleaseChatPlugin.Infrastructure
```

In Development the app also migrates on startup and runs the seed script. Failures there are logged
and swallowed, so the app still starts without a database — the Releases module simply will not work.

`deploy/migrate-production.sql` is the idempotent script for non-Development environments.

### 2. An Entra ID app registration

Fill in `AzureAd` in `appsettings.json` — `Domain`, `ClientId`, `TenantId` are placeholders today,
and OIDC metadata retrieval fails until they are real.

1. Entra admin centre → App registrations → New registration. Single tenant.
2. Redirect URI: *Web* → `https://localhost:7217/signin-oidc`
3. Front-channel logout URL: `https://localhost:7217/signout-callback-oidc`
4. Certificates & secrets → New client secret → store it in user-secrets, **not** in a file:
   ```bash
   dotnet user-secrets set "AzureAd:ClientSecret" "<value>" --project DemoReleaseChatPlugin.Website
   ```
5. API permissions → Microsoft Graph → Delegated → `User.Read`, `User.ReadBasic.All`. Grant admin
   consent. Without `User.ReadBasic.All`, staff search returns nothing.
6. Token configuration → optional claims → ID token → add `email`, `family_name`, `given_name`.

### 3. Seed yourself as an administrator

`DemoReleaseChatPlugin.Infrastructure/Persistence/Scripts/seed-authorization-users.sql` still carries
`<Your Name>` / `<your Entra object id>` placeholders. **Replace them before first run.** Without at
least one seeded `IT Admin`, no menu item appears and every page 403s — the whole app gates on that
role.

Find your object ID: Entra admin centre → Users → your account → Object ID.

### Optional — SendGrid

Set `SendGrid:ApiKey` and `EmailSettings:FromEmail`. Without them the app runs fine: every send fails,
CAP retries ten times and marks the message failed, and you can read the full rendered email on the
queue dashboard at `/releases/queue`. That is a perfectly good development loop.

## Build and run

```bash
dotnet build DemoReleaseChatPlugin.sln
```

```bash
dotnet run --project DemoReleaseChatPlugin.Website
```

There is no test project in this solution — do not assume a `dotnet test` target exists. The domain
is now written to be testable without one (injected clock, injected ids, invariants returning
`Result`), so adding one is a smaller job than it was.

If the app is already running, `dotnet build` fails with `MSB3021`/`MSB3027` file-lock errors. That is
not a compile failure; stop the app, or compile-check to a scratch directory with `-o`.

## The rules that make this codebase make sense

1. **Command handlers never call `SaveChangesAsync`.** They stage changes and call
   `IDemoReleaseChatPluginUnitOfWork.CommitAsync()`, which in *one transaction* saves rows, copies
   every domain event into `Audit.DomainEvents`, and publishes those events. Getting that order wrong
   fails silently.
2. **Entities raise their own domain events, in their factories.** `ScheduledRelease.File(...)`
   raises `ScheduledReleaseFiled`. There is no method a handler can forget to call.
3. **Domain events carry identifiers, not detail.** A listener that needs detail re-reads the record.
   `ProjectId` is the one deliberate exception. (Queued *emails* are the other exception — they carry
   their rendered body, so a retry days later delivers what was composed.)
4. **Validators are never called by hand.** `AddPodPipeline()` runs them as a mediator pipeline
   behaviour. A failure now **returns a failed `Result`** rather than throwing, so a page reads
   `.IsFailure` like it would for any other outcome. Pages still call `IsValidAsync(Validator)`
   first, because per-field messages beat one combined sentence in a dialog — but it is a UX choice
   now, not a crash guard.
5. **Handlers return `Result<T>`, they don't throw.** Call sites check `.IsFailure` / `.Message` /
   `.Value`. Reads of a single item return `QueryResult<T>` instead, which distinguishes "it isn't
   there" from "the read failed" — check `.IsNotFound` before `.IsFailure`.
6. **Business rules live in the aggregate; validators check shape and uniqueness.** If the answer
   depends on the entity's own state ("is this project archived?"), it is a guard clause inside the
   aggregate returning a `Result`. If it depends on *other rows* ("is this name taken?"), it is a
   validator. Handlers only load, invoke and relay.
7. **Identifiers are typed.** `ProjectId`, `ReleaseNoticeId`, `StakeholderGroupId`, `StakeholderId`.
   They convert implicitly *to* `Guid` and never *from* one — an edge that receives a raw Guid (a
   route parameter) converts explicitly with `ProjectId.From(...)`.
8. **Commands take the write context; everything read-only takes `IDemoReleaseChatPluginReadDb`.**
   The read port has no `Add` and no `SaveChangesAsync`, and every set is already `AsNoTracking`.
9. **The domain takes a clock, never reads one.** No `DateTime.Now` or `Guid.NewGuid()` anywhere in
   `DemoReleaseChatPlugin.Domain`; ids are minted in the handler and timestamps come from `IClock`.

Two libraries are easy to get wrong: the mediator is **`Mediator` (martinothamar)**, not MediatR —
handlers return `ValueTask<T>`, and `AddMediator(...)` can only be called from the Website, where the
source generator runs. The queue is **`DotNetCore.CAP`**, which owns its own `cap.*` tables and
creates them itself; they are never in your migrations.

## Architecture

`ARCHITECTURE-CHANGES.md` records the pass made to align this solution with the team's Clean
Architecture handbook — what moved, what deliberately did not, and what is not covered by
verification. Read it before making structural changes.

Note for anyone returning to this repo with older notes: the application layer used to be called
`DemoReleaseChatPlugin.Core`, and the four `Shared/` projects used to be a single
`DemoReleaseChatPlugin.Shared`. Both were renamed for the reasons in §2 and §16 of that document.

## Deviations from the specification

- **Config sources.** `AddPodKeyVault` / `AddPodConfigurationStorage` are omitted (the spec's
  documented local-only path). Everything comes from appsettings + user-secrets.
- **`Projects.razor` is role-gated.** The spec notes the original omission is an oversight, not a
  decision, and says to add `[Authorize(Roles = ItAdmin)]` on rebuild. Done.
- **Nullable-flow warnings are suppressed** in Core/Infrastructure/Website. The spec pins
  `Nullable=enable` *and* prescribes domain code written nullable-oblivious; keeping both produced
  several hundred CS86xx warnings that buried real ones. The annotation context stays on. See the
  comment in each csproj.
- **Two transitive security pins added** (`System.Security.Cryptography.Xml`,
  `Microsoft.AspNetCore.DataProtection` at 10.0.11), in the same spirit as the spec's own Graph.Core
  and OpenTelemetry pins. `Microsoft.Identity.Web 4.3.0` pulls 10.0.0 of both, which carry eight high
  and one critical advisory. At runtime these resolve from the ASP.NET Core shared framework anyway;
  the pins clear the restore graph and matter under a self-contained publish.
