# AGENTS.md

Repo: ABP Framework 10.6.1 layered "app" template on .NET 10 — Blazor Web App (server + WASM), EF Core / SQL Server LocalDB, OpenIddict, Blazorise, multi-tenancy, default language `zh-Hans`. Product domain is the "午餐命运" lunch wheel (the template's Books/Authors sample was removed).

## Commands (run from repo root unless noted)

```bash
dotnet build LunchFate.slnx                                   # currently 0 warnings / 0 errors — keep it that way
dotnet test LunchFate.slnx --no-build                         # 3 tests, all in EntityFrameworkCore.Tests
dotnet test test/LunchFate.EntityFrameworkCore.Tests/LunchFate.EntityFrameworkCore.Tests.csproj --no-build \
  --filter "FullyQualifiedName~Should_Query_AppUser"          # single test
dotnet run --project src/LunchFate.Blazor                     # https://localhost:44352, login admin / abc123!
dotnet ef migrations list --project src/LunchFate.EntityFrameworkCore --no-connect
abp install-libs                                              # after clone: wwwroot/libs is gitignored
```

- **DbMigrator must run from its own directory**: `dotnet run` inside `src/LunchFate.DbMigrator/` (or use `etc/scripts/migrate-database.ps1`). From the repo root it fails with `ConnectionString 属性尚未初始化`. It creates/migrates the LocalDB database and seeds the admin user (password `abc123!`, see `LunchFateConsts`).
- Global tools only (no `.config/dotnet-tools.json`): `dotnet-ef` 10.0.12, `abp` (`volo.abp.studio.cli`) 3.1.4.
- No CI, no lint/format/typecheck beyond `.editorconfig` and `Directory.DotSettings`. Don't look for a formatter config — there isn't one.

## Tests

- Only `test/LunchFate.EntityFrameworkCore.Tests` has runnable tests (3). The other test projects print `没有可用测试` / "no tests available" and exit 0 — that's expected: their test classes are abstract; concrete tests live in the EF Core project.
- EF tests run against **in-memory SQLite** (`AbpUnitTestSqliteDatabase` in `LunchFateEntityFrameworkCoreTestModule`) — no SQL Server needed.
- The test host boots the full ABP module (seed data on startup, always-allow auth, background jobs disabled). EF tests share xUnit collection `"LunchFate collection"`.

## Layout / entry points

- Layers in `src/`: Domain.Shared → Domain → Application.Contracts → Application → HttpApi / HttpApi.Client → EntityFrameworkCore → Blazor (+ Blazor.Client) → DbMigrator.
- Web entry: `src/LunchFate.Blazor/Program.cs` → `LunchFateBlazorModule`. Migration/console entry: `src/LunchFate.DbMigrator/Program.cs`.
- Every csproj imports `common.props` (shared TargetFramework etc.).
- One EF migration exists: `src/LunchFate.EntityFrameworkCore/Migrations/20261010024930_Initial`. The design-time factory (`LunchFateDbContextFactory.cs`) reads the connection string from `../LunchFate.DbMigrator/appsettings.json` — edit that file, not the Blazor one, when changing the DB connection for migrations.

## Gotchas

- `src/LunchFate.Blazor/openiddict.pfx` + `AuthServer:CertificatePassPhrase` in its `appsettings.json` are required for the app to start (`AddProductionEncryptionAndSigningCertificate`). Don't delete either.
- Scripts in `etc/scripts/` (`initialize-solution.ps1`, `migrate-database.ps1`) are the canonical setup steps ABP Studio uses; follow their CWD conventions.
- Client-side deps live in `wwwroot/libs` and are gitignored — a fresh clone needs `abp install-libs` before the Blazor app will serve pages.
