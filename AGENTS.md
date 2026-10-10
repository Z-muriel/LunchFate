# AGENTS.md

Repo: ABP Framework 10.6.1 layered "app" template on .NET 10 — Blazor Web App (server + WASM), EF Core / SQL Server LocalDB, OpenIddict, Blazorise, multi-tenancy, default language `zh-Hans`. Product domain is the "午餐命运" lunch wheel + shop CRUD (the template's Books/Authors sample was removed). Feature spec: `docs/需求文档.md` — existing code cites its requirement IDs (e.g. `需求文档 FR-W-01`) in comments.

## Commands (run from repo root unless noted)

```bash
dotnet build LunchFate.slnx                                   # currently 0 warnings / 0 errors — keep it that way
dotnet test LunchFate.slnx --no-build                         # 12 tests, all in EntityFrameworkCore.Tests
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

- Only `test/LunchFate.EntityFrameworkCore.Tests` has runnable tests (12: 3 template samples + 9 shop tests). The other test projects print `没有可用测试` / "no tests available" and exit 0 — that's expected: their test classes are abstract; concrete tests live in the EF Core project. Shop tests are written once as `test/LunchFate.Application.Tests/Shops/ShopAppServiceTests.cs` and driven by `EfCoreShopAppServiceTests`.
- EF tests run against **in-memory SQLite** (`AbpUnitTestSqliteDatabase` in `LunchFateEntityFrameworkCoreTestModule`) — no SQL Server needed.
- The test host boots the full ABP module (seed data on startup, always-allow auth, background jobs disabled). EF tests share xUnit collection `"LunchFate collection"`.

## Layout / entry points

- Layers in `src/`: Domain.Shared → Domain → Application.Contracts → Application → HttpApi / HttpApi.Client → EntityFrameworkCore → Blazor (+ Blazor.Client) → DbMigrator.
- Web entry: `src/LunchFate.Blazor/Program.cs` → `LunchFateBlazorModule`. Migration/console entry: `src/LunchFate.DbMigrator/Program.cs`.
- Every csproj imports `common.props` (shared `LangVersion` / `Version` / `NoWarn(CS1591)` / `NuGetAudit` / `AbpProjectType`); `TargetFramework` is per-csproj (`net10.0`).
- Three EF migrations exist: `20261010024930_Initial`, `20261010075123_AddShops` and `20261010094539_AddShopNameUniqueIndex`. The design-time factory (`LunchFateDbContextFactory.cs`) reads the connection string from `../LunchFate.DbMigrator/appsettings.json` (relative to the EF project dir) — edit that file, not the Blazor one, when changing the DB connection for migrations.

## Gotchas

- `src/LunchFate.Blazor/openiddict.pfx` + `AuthServer:CertificatePassPhrase` in its `appsettings.json` are required for the app to start (`AddProductionEncryptionAndSigningCertificate`). Don't delete either.
- Scripts in `etc/scripts/` (`initialize-solution.ps1`, `migrate-database.ps1`) are the canonical setup steps ABP Studio uses; follow their CWD conventions.
- Client-side deps live in `wwwroot/libs` and are gitignored — a fresh clone needs `abp install-libs` before the Blazor app will serve pages.

## 开发规范要点（中文）

> 本节是原《开发规范总结》(约 750 行) 的压缩版，只保留会改变你写代码方式的硬性约定；需要完整条文时向用户索取。规范与代码冲突时，以项目现有代码为准并先确认。

### 协作流程（硬性）

- 全程用中文协作；注释、说明也用中文。
- 只读分析可直接执行；**创建/修改/删除文件前必须先给方案**（目标、涉及文件、影响范围、风险、验证计划），获用户明确批准后才动手。
- 严格按批准范围做最小改动；不猜需求或业务意图；无法验证时说明残留风险。

### 分层与文件放置

- Domain.Shared：枚举/常量/错误码；Domain：聚合根、领域服务、仓储接口；Application.Contracts：Dto/Result/AppService 接口；Application：AppService 实现、事件处理器；EntityFrameworkCore：DbContext、仓储实现；HttpApi：Controller。
- 调用链：Controller/动态接口 → AppService → DomainService/Repository；AppService 内查询用 `AsyncExecuter.CountAsync/ToListAsync` 执行。
- 简单 CRUD 直接用泛型 `IRepository<T, Guid>`（现 Shops 即如此）；确需自定义查询才建自定义仓储接口，方法统一 async，单实体 `includeDetails = true`、列表默认 `false`。

### 命名

- 单词缩写一律 Pascal：`Id`、`Db`（禁 `ID`、`DB`）；多词缩写同上下文保持一致。
- 私有字段 `_camelCase`；主构造参数 = 小写开头的接口名（`repositoryShop` → `_repositoryShop`；只读仓储 `shopReadOnlyRepository`）。
- 入参 `CreateXxxDto`、出参 `XxxResult`；领域事件 `XxxPaidEto` + 处理器 `XxxPaidEventHandler`；领域服务 `XxxManager`。
- 异步方法 `Async` 结尾；查询方法 `Get`/`Search` 开头；参数名不重复进方法名：`GetOrderDetailAsync(orderId)` 而非 `GetOrderDetailByOrderIdAsync(orderId)`。

### 注释

- 公共类/公共接口/公共方法必须 XML `<summary>`；**本次新增或修改的 `private`/`protected override` 方法同样必须补 `<summary>`**；多个简单类型参数补 `<param>`，返回值不直观补 `<returns>`（DI 参数、单个对象参数可省）。
- 复杂业务流程在代码上方加 `// 1.` `// 2.` 步骤注释；聚合根关键属性、常量、枚举值必须注释；`#endregion` 必须带标题。
- 注释写"为什么/业务意图"并尽量引用需求编号（见 `ShopAppService` 的写法）；临时/调试注释提交前清理。

### DDD 要点

- 聚合是一致性边界：内部强一致，跨聚合只持有 Id；删聚合根时子实体一起删。
- 跨聚合写操作走命令/领域事件，**禁止注入别的聚合的写仓储直接修改**；只读查询可用只读仓储。
- 领域不变量（唯一性/存在性/状态约束）放领域服务；领域服务不使用 `ICurrentUser`、不定义纯查询方法、默认不定义接口。
- 子实体继承 `Entity<T>`、必带聚合根外键、不建独立仓储；持久化的状态类枚举优先 `byte`。

### 查询与分页

- 固定顺序：筛选 → 计数 → 排序+分页+投影 → 返回。禁止先分页再计数；必须有默认排序；列表必须 `Select` 投影到 Dto，不加载整实体再映射。
- 入参继承 `PagedAndSortedResultRequestDto`，出参 `PagedResultDto<T>`（禁止自定义分页 Result）。
- 动态排序必须先白名单校验再交给 `System.Linq.Dynamic.Core` — 参照 `ShopAppService.SanitizeSorting`。
- 避免 N+1、全量加载后内存聚合；聚合/统计放数据库端。

### 异常、校验与授权

- 业务错误抛 `UserFriendlyException`，消息经 `IStringLocalizer<LunchFateResource>`（`Domain.Shared/Localization/LunchFate/zh-Hans.json` + `en.json`），不硬编码中文；不要 try-catch 包裹业务后 `throw ex`，让框架记日志。
- AppService 类与各操作显式授权 `[Authorize(LunchFatePermissions.Shops.Xxx)]`，公开端点标 `[AllowAnonymous]`；不信任客户端传来的状态字段（排序、分页、启用状态都要服务端校验）。
- 输入用数据注解校验（`[Required]`/`[StringLength]`/`[Range]`…），业务规则校验放应用/领域层；前后端都要校验。

### 自检 / 评审输出

- 严重度三级：**必须修改** / **建议优化** / **可选改进**；每条问题写清问题点、原因、影响范围、建议动作。
- 只对本次触达的代码下结论；证据不足不裁决，避免误报，不把个人偏好当规范；不自检 `#if DEBUG`。
