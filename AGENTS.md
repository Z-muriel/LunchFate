# AGENTS.md

Repo: ABP Framework 10.6.1 layered "app" template on .NET 10 — Blazor Web App (server + WASM), EF Core / SQL Server LocalDB, OpenIddict, Blazorise, multi-tenancy, default language `zh-Hans`. Product domain is the "午餐命运" lunch wheel (the template's Books/Authors sample was removed).

## Commands (run from repo root unless noted)

```bash
dotnet build LunchFate.slnx                                   # currently 0 warnings / 0 errors — keep it that way
dotnet test LunchFate.slnx --no-build                         # 10 tests, all in EntityFrameworkCore.Tests
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

- Only `test/LunchFate.EntityFrameworkCore.Tests` has runnable tests (10: 3 template samples + 7 shop tests). The other test projects print `没有可用测试` / "no tests available" and exit 0 — that's expected: their test classes are abstract; concrete tests live in the EF Core project. Shop tests are written once as `test/LunchFate.Application.Tests/Shops/ShopAppServiceTests.cs` and driven by `EfCoreShopAppServiceTests`.
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

# 开发规范总结

本文档整合所有开发规范，供开发者快速查阅。

## 一、开发流程

### 1.1 工作流程

1. **开始前**
    - 使用中文协作
    - 明确目标、验收标准和不改动范围
    - 需求不清时先提问
    - 只读分析可先执行，创建/修改/删除文件前必须进入方案审批

2. **规则加载**
    - 优先加载 `always_on` 规则
    - 按文件匹配加载对应规则
    - 规则冲突时以项目规则为准
    - 完成代码改动后加载自检规则

3. **方案与审批**
    - 修改文件前说明方案、影响范围、涉及文件、改动原因、规则依据、风险和验证计划
    - 未经明确批准不修改任何文件
    - 用户同意只覆盖上一条明确方案
    - 发现规则冲突时先说明并等待决策

4. **实施**
    - 严格按批准范围实施，保持最小改动
    - 不猜测需求、设计稿或业务意图
    - 新增/修改字段前必须能追溯到依据
    - 按注释规则补充 XML 注释和业务步骤注释

5. **验证**
    - 优先运行受影响项目的编译或测试
    - 编译通过不能替代规则合规判断
    - 无法验证时说明原因和残留风险

6. **收尾自检**
    - 对照规则检查分层、命名、注释、查询、异常
    - 列出新增/修改项逐项核对
    - 运行格式检查
    - 最终汇报修改摘要、验证结果和风险

### 1.2 自检核心原则

**角色定位**
- 基于当前加载规则、变更事实、上下文证据进行逐项自检
- 专注事实、证据、规范符合性和代码健康
- 输出可执行、可追溯、低误报的结论

**最高优先级规则**
1. 默认输出自检结论，不擅自延伸为实施任务
2. 自检标准以当前实际加载的规则为准
3. 不得猜测需求、设计稿、验收标准或业务意图
4. 规范优先于现有代码
5. 规范冲突必须确认
6. 逐项分析，禁止笼统结论
7. 证据优先于偏好
8. 问题级别必须与实际风险匹配
9. 尊重作者的合理选择
10. 证据不足时不得强行裁决
11. 避免误报
12. 业务边界独立性

**严重度定义**
- **必须修改**：违反明确规范、存在明显风险
- **建议优化**：风险低但仍应修改
- **可选改进**：不影响通过的改进建议

**执行边界**
- 只对本次任务触达的代码输出问题
- 不把个人偏好包装成规范问题
- 不自检 `#if DEBUG` 代码块

---

## 二、技术架构

### 2.1 分层职责

| 层 | 职责 |
|----|------|
| Domain.Shared | 枚举、常量、错误码、共享模型 |
| Domain / Core | 聚合根、子实体、领域服务、仓储接口、领域规则、外部系统代理 |
| Application.Contracts | AppService 接口、Dto、Result |
| Application | AppService 实现、事件处理器 |
| EntityFramework | DbContext、仓储实现、实体配置 |
| WebApi | Controller、认证、路由、中间件 |

### 2.2 调用链规范

**HTTP 调用链**
```
Controller / 动态接口 -> AppService -> DomainService / Repository
```

**外部系统调用链**
```
AppService / DomainService -> Proxy（领域层）-> External Service
```

### 2.3 DbContext 与持久化

- 业务 DbContext 命名：`{ProjectName}DbContext`
- 复杂映射使用 `EntityTypeConfiguration<T>`
- 子实体必须显式配置外键关联
- 聚合内子实体使用 `WillCascadeOnDelete(true)`

### 2.4 文件放置规则

| 文件类型 | 正确层 |
|----------|--------|
| 枚举、常量 | Domain.Shared |
| 聚合根、领域服务、仓储接口、Proxy | Domain / Core |
| Dto、Result、AppService 接口 | Application.Contracts |
| AppService 实现、事件处理器 | Application |
| 仓储实现、DbContext | EntityFramework |
| Controller | WebApi |

---

## 三、DDD 领域建模

### 3.1 聚合边界

**核心原则**
- 聚合是一致性边界
- 聚合内部强一致，聚合之间最终一致
- 外部只能通过聚合根访问
- 保持聚合尽可能小
- 跨聚合引用只通过 Id，不持有对象引用

**边界判断**
- 必须在同一事务内保持一致 → 同一聚合
- 删除根对象时应一起删除 → 同一聚合
- 能独立存在并被单独维护 → 不同聚合

### 3.2 跨聚合通信

**允许与禁止**
- ✅ 本聚合内注入写仓储
- ✅ 跨聚合注入只读仓储做查询
- ✅ 通过命令/事件驱动跨聚合更新
- ❌ 跨聚合注入写仓储直接修改别的聚合

**事务边界**
- 一个事务优先只修改一个聚合实例
- 跨聚合状态变更通过领域事件实现最终一致

### 3.3 领域服务使用

**何时使用**
- 业务规则优先封装在实体方法中
- 仅当逻辑涉及跨实体协作、依赖外部服务时使用
- 需要封装领域规则编排但仍属于领域概念时使用

**领域不变量识别**（应提取到领域服务）
- 唯一性校验：手机号/编号不能重复
- 存在性校验：关联对象必须存在
- 业务状态约束：已完成订单不能修改
- 跨实体一致性规则：总额不能超过预算

**使用约束**
- 放在领域层
- 只表达领域规则，不承担接口适配
- 可依赖本聚合仓储
- 默认不定义接口
- 不定义纯查询公开方法
- 不使用 `ICurrentUser` 等认证上下文
- 方法参数和返回值使用领域对象
- 方法名使用具体业务语义

### 3.4 值对象使用

- 建模时优先考虑值对象
- 没有唯一标识的概念
- 通过属性值判断相等性
- 创建后保持不可变

---

## 四、代码实现规范

### 4.1 聚合与实体

**聚合根**
- 按审计需求继承 `AggregateRoot<TPrimaryKey>` 或其派生类
- 使用 `ICollection<TEntity>` 导航属性，在构造函数初始化
- 跨聚合引用只通过 Id
- 提供 protected 无参 + public 有参构造函数
- 属性和方法优先使用 `virtual`

**子实体**
- 继承 `Entity<TPrimaryKey>`
- 必须包含聚合根外键
- 不创建独立仓储
- 不引入聚合根导航属性

**枚举**
- 持久化的状态类枚举优先考虑 `: byte`

### 4.2 仓储规则

**核心口径**
- 业务代码优先依赖自定义仓储接口
- 简单 CRUD 可直接依赖泛型仓储
- 自定义仓储接口允许暴露 `IQueryable`

**自定义仓储方法**
- 统一使用异步签名
- 返回单实体：`bool includeDetails = true`
- 返回列表：`bool includeDetails = false`

### 4.3 查询构建

**导航属性查询（EF6）**
```csharp
// 正确 - 显式加载
var query = repository.GetAllIncluding(x => x.OrderItems);

// 错误 - 未显式加载就访问
var query = repository.GetAll();
var itemCount = query.Select(x => x.OrderItems.Count);
```

**查询构建顺序**
```
构建基础查询 → 应用筛选 → 关联/JOIN → 投影 → 排序 → 分页 → 执行
```

**JOIN 与筛选**
- 筛选条件与 JOIN 逻辑分离
- 主表必须存在、关联表也必须存在 → INNER JOIN
- 主表必须存在、关联表可空 → LEFT JOIN

**投影**
- 列表/批量查询使用 `Select` 投影
- 不先查完整实体再映射
- 例外：单条详情查询且使用大部分字段

### 4.4 分页

**输入**
- 继承 `PagedAndSortedResultRequestDto`

**输出**
- 必须返回 `PagedResultDto<T>`
- 禁止自定义分页 Result

**标准顺序**
```csharp
// 1. 筛选
query = query.WhereIf(condition, x => x.Field == value);

// 2. 计数
var totalCount = await asyncQueryableExecuter.CountAsync(query);

// 3. 排序 + 分页 + 投影
var items = await asyncQueryableExecuter.ToListAsync(
    query.OrderByDescending(x => x.CreationTime)
         .PageBy(input)
         .Select(x => new ListItemResult { ... }));

// 4. 返回
return new PagedResultDto<ListItemResult>(totalCount, items);
```

**常见错误**
- ❌ 先分页再计数
- ❌ 无默认排序
- ❌ 加载完整实体后再映射
- ❌ 使用 `PageBy(skipCount, maxResultCount)`

### 4.5 查询性能

**避免问题**
- N+1 查询
- 多次串行数据库往返
- 全量加载后内存分组/聚合
- 不必要的中间集合

**正确做法**
- 一次性查询并投影
- 数据库端聚合
- 批量操作使用 EF Plus 或原生批量能力

---

## 五、命名规范

### 5.1 命名法

| 类型 | 规则 | 示例 |
|------|------|------|
| Pascal | 每个单词首字母大写 | `RentalOrderManager` |
| Camel | 首单词小写，后续首字母大写 | `rentalOrderId` |

### 5.2 缩写词规则

| 类型 | 规则 | 示例 |
|------|------|------|
| 多单词首字母缩略词 | 全大写或 PascalCase 均可，同上下文保持一致 | `WMS` / `Wms`、`HTTP` / `Http` |
| 单个单词的缩写 | 一律 PascalCase，禁止全大写 | `Id`、`Db` (不是 `ID`、`DB`) |

### 5.3 通用标识符

| 标识符 | 命名法 | 示例 |
|--------|--------|------|
| 类 | Pascal | `CreateNewSignOrderHandler` |
| 接口 | Pascal，`I` 前缀 | `IRentalOrderAppService` |
| 方法 | Pascal | `GetOrderDetailAsync` |
| 属性 | Pascal | `RentalDuration` |
| 参数 | Camel | `rentalOrderId` |
| 局部变量 | Camel | `discountAmount` |
| 私有字段 | Camel，`_` 前缀 | `_repositoryRentalOrder` |
| 常量 | Pascal | `DefaultPageSize` |
| 私有静态只读字段 | Pascal | `DistributionRecipients` |

### 5.4 仓储命名

**写仓储**
- 接口：`IRentalOrderRepository`
- 主构造函数参数：`repositoryRentalOrder`
- 私有字段：`_repositoryRentalOrder`

**只读仓储**
- 接口：`IRentalOrderReadOnlyRepository`
- 主构造函数参数：`rentalOrderReadOnlyRepository`
- 私有字段：`_rentalOrderReadOnlyRepository`

### 5.5 Application 命名

- AppService 接口：`IProductAppService`
- AppService 实现：`ProductAppService`
- 主构造函数参数：`productAppService`
- 私有字段：`_productAppService`

### 5.6 DTO / Result 命名

- 输入参数：`CreateRentalOrderDto`
- 返回结果：`CreateRentalOrderResult`
- 数据层临时对象：`ProjectAuthorizationDo`
- 应用层临时对象：`DiscountCalculationBo`

### 5.7 领域对象命名

- 聚合根：`RentalOrder`
- 子实体：`RentalOrderItem`
- 领域服务：`PromotionActivityManager`
- 主构造函数参数：`promotionActivityManager`
- 私有字段：`_promotionActivityManager`
- 领域事件传输对象：`RentalOrderPaidEto`
- 领域事件处理器：`RentalOrderPaidEventHandler`

### 5.8 方法命名

- 异步方法统一以 `Async` 结尾
- 查询方法以 `Get` 或 `Search` 开头
- 不要把参数名重复写进方法名

```csharp
// ✅ 正确
GetOrderDetailAsync(Guid rentalOrderId)

// ❌ 错误
GetOrderDetailByRentalOrderIdAsync(Guid rentalOrderId)
```

---

## 六、注释规范

### 6.1 注释原则

- 注释解释"为什么"和"业务意图"
- 不重复代码字面含义
- 临时注释、调试注释在提交前清理
- 避免行尾注释、装饰性注释

### 6.2 必须写注释

- 公共类、公共接口
- 公共接口方法、对外公共方法
- **当前变更中新增或修改的 `private` / `protected override` 方法**
- 聚合根关键业务属性
- 复杂业务分支、状态变更、跨系统调用
- 枚举类型及枚举值

### 6.3 可简化或省略

- 自解释的局部变量
- 纯转发方法
- 依赖注入参数

### 6.4 方法注释

```csharp
/// <summary>
/// 方法用途描述
/// </summary>
/// <param name="rentalOrderId">租用订单Id</param>
/// <returns>订单详情</returns>
Task<GetRentalOrderResult> GetOrderDetailAsync(Guid rentalOrderId);
```

**要求**
- 公共方法必须写 `<summary>`
- 新增/修改的 `private` / `protected override` 方法默认必须补 `<summary>`
- 多参数、简单类型参数必须补 `<param>`
- 单个对象类型参数可省略 `<param>`
- 依赖注入参数不要求补 `<param>`
- 返回值语义不直观时补 `<returns>`

### 6.5 属性与字段注释

- 聚合根公共属性必须注释
- DTO / Result 中含义不直观的字段必须注释
- 常量必须注释
- 私有属性、私有字段：含义不直观、存在业务约束、参与状态缓存或影响业务分支时必须注释

### 6.6 单行与步骤注释

```csharp
// 1. 查询订单
var order = await repository.GetAsync(id);

// 2. 校验支付状态
if (order.IsPaid)
{
    ...
}
```

- 单行注释放在代码上方
- 步骤注释使用 `// 1.`、`// 2.` 形式
- 复杂业务流程必须添加关键步骤注释

### 6.7 Region 规则

- 仅在类较长、分区明显时使用
- 所有 `#endregion` 必须带标题

```csharp
#region 属性
...
#endregion 属性
```

---

## 七、接口与异常处理

### 7.1 路由与公开方式

**ABP 动态接口**
- 格式：`/api/services/{namespace}/{service}/{action}`
- 配置：`DynamicApiControllerBuilder`

**手写 Controller**
- 以路由特性和启动配置为准

### 7.2 HTTP 动词

| 动词 | 适用操作 |
|------|----------|
| GET | 查询 |
| POST | 创建、复杂查询、回调 |
| PUT | 完整更新 |
| PATCH | 部分更新 |
| DELETE | 删除 |

### 7.3 输入校验

- 前端和后端都要校验
- 输入对象使用数据注解：`[Required]`、`[StringLength]`、`[MaxLength]`、`[Range]`、`[RegularExpression]`
- 业务规则校验放在应用层/领域层

### 7.4 异常处理

**原则**
- ❌ 不要用 `try-catch` 包裹整段业务后 `throw ex`
- ✅ 让框架处理通用异常日志
- ✅ 业务错误使用 `UserFriendlyException`

**常用异常类型**
- `UserFriendlyException`：直接向前端展示业务提示
- `AbpAuthorizationException`：权限不足
- `AbpValidationException`：参数校验失败

### 7.5 授权与安全

- 需要鉴权的接口明确加 `[AbpAuthorize]` 或 `[Authorize]`
- 不在日志中记录敏感信息
- 不信任来自客户端的状态字段

---

## 八、自检专项检查

### 8.1 配置文件检查

- 环境一致性：新增配置项是否在所有环境添加
- 配置相关性：配置变更是否与本次需求相关
- 不检查硬编码敏感信息
- 不自动标记生产配置为 critical

### 8.2 调用链检查

| 自检对象 | 必须同时自检 |
|----------|-------------|
| 后台作业 | 调用的 Handler / Command |
| Handler | 调用的其他 Handler、领域服务、仓储查询 |
| AppService | 调用的 Handler / Command |

### 8.3 性能专项检查

**Contains 例外判断**
1. 找到两个仓储接口
2. 检查命名空间/项目
3. 判断是否来自不同 DbContext
4. 不同 DbContext 禁止标记为问题

**性能评估量级**
- `< 1000` 条：结合场景判断
- `1000-10000` 条：需重点关注
- `> 10000` 条：优先视为必须优化

### 8.4 IQueryable 契约暴露验证

标记前必须确认：
1. 是业务契约暴露还是实现细节
2. 代码位于哪个层
3. 场景属于哪种查询类型
4. 本质问题是抽象泄漏还是仅仅出现 `IQueryable`

### 8.5 文件分层识别

1. 按所属层分组
2. 为每组匹配规范条款
3. 为每组判断需执行的专项检查

**新增/修改符号清单**
- 类型、接口、枚举、DTO / Result
- 方法、Handler、AppService 方法、仓储方法
- 属性、字段、常量、私有方法
- 复杂业务流程、关键分支

### 8.6 需求符合性检查

当提供需求上下文时检查：
- 实现与需求的偏差
- 超出需求范围
- 需求遗漏
- 公共代码影响
- 需要确认的点

### 8.7 逻辑正确性检查

**参数与校验一致性**
- 参数类型与校验匹配
- 集合校验语义
- 异常消息准确性

**条件分支完整性**
- 分支覆盖所有场景
- 无不可达分支
- switch 有 default

**数据流一致性**
- 输入输出对应
- 中间变量正确传递
- 返回值语义一致

**隐含假设**
- 集合非空假设
- 对象已初始化假设
- 顺序依赖假设

---

## 九、问题输出规范

### 9.1 问题输出原则

每条问题必须说明：
- 问题点：代码中具体哪里不符合规范
- 原因：违反了什么规则
- 影响范围：会影响什么
- 建议动作：建议如何修改

### 9.2 规范依据写法

**推荐格式**
```
{规则文件名} §{章节号} - {条款名}
```

**示例**
- `020-self-check-principles.md §4 - 问题提出前的强制验证`
- `050-architecture.md §6 - 聚合边界`
- `060-coding.md §4 - 查询性能规则`
- `requirement - 实现与需求不一致`

**禁止写法**
- 只写"违反规范"但不写文件与条款
- 引用与当前问题无关的章节
- 引用不存在的条款名称

### 9.3 低置信度建议抑制

- 包含条件免责的建议不应输出
- suggestion 必须有明确改进依据
- 前提条件取决于不可见信息时放弃输出

---

## 十、项目特例（TextileSEGit）

### 10.1 技术栈事实

- **ABP 7.1.0 + .NET Framework 4.6.2 + EF6**
- `Aser.TextileSE.Server.EntityFrameworkCore/` 目录名不代表 EF Core
- 实际依赖仍为 `Abp.EntityFramework` 与 `EntityFramework 6.4.4`

### 10.2 项目结构

- 混合仓库：服务端 + Winform 客户端 + 多宿主
- 服务端主链路按 `Aser.TextileSE.Server.sln` 识别
- 目录名与实际程序集名不总是一致，先看 `csproj`

### 10.3 历史兼容层

- `Aser.TextileSE.DAL`：按历史 Domain 兼容层理解
- `Aser.TextileSE.Common`：按 Domain.Shared 兼容层理解
- 不是对任意新职责的无限豁免

### 10.4 数据源与持久化

- 主持久化：EF6
- 允许 EF6 与 Dapper / 原生 SQL 并存
- 多账套切换：`UsingDatabase` Header + `Book` 表 + `TenantId` 回退

### 10.5 宿主与接口

- `Aser.TextileSE.Winform.WebAPI`：正式宿主/协议适配层
- 同时存在动态接口与手写 Controller
- 多个独立宿主：Mobile.WebAPI、ImageService、CAServer.WebAPI 等

### 10.6 特定豁免

**AResponse 已处理异常**
```csharp
var response = await someService.DoSomethingAsync(input);
if (!response.Success)
{
    throw new UserFriendlyException(response.Error);
}
// 不报告"未处理异常"
```

**业务保证的类型转换**
- 上下文业务逻辑已保证数据有效性时不报安全问题

**AsyncDataSource 缓存管理**
- 依赖框架自动缓存生命周期，不报"缺少过期策略"

**报表/打印模板模块独立性**
- 不同业务单据模块独立维护不报"代码重复/抽公共基类"

---

## 十一、快速检查清单

### 开始前
- [ ] 需求明确了吗？
- [ ] 验收标准清楚了吗？
- [ ] 不改动范围确定了吗？

### 方案阶段
- [ ] 说明了影响范围吗？
- [ ] 列出了涉及文件吗？
- [ ] 给出了规则依据吗？
- [ ] 用户明确批准了吗？

### 实施阶段
- [ ] 按批准范围实施了吗？
- [ ] 没有猜测需求吗？
- [ ] 补充了必要注释吗？
- [ ] 保持了最小改动吗？

### 验证阶段
- [ ] 运行了编译/测试吗？
- [ ] 检查了规则合规吗？
- [ ] 说明了验证结果吗？

### 自检阶段
- [ ] 检查了分层职责吗？
- [ ] 检查了命名规范吗？
- [ ] 检查了注释完整性吗？
- [ ] 检查了查询性能吗？
- [ ] 检查了聚合边界吗？
- [ ] 检查了异常处理吗？
- [ ] 运行了格式检查吗？

### 问题输出
- [ ] 每条问题有明确依据吗？
- [ ] 严重度分级准确吗？
- [ ] 没有误报吗？
- [ ] 没有重复问题吗？

---

**本文档整合了所有开发规范，是开发和自检的唯一参考。遵循本规范可确保代码质量、架构合理、可维护性强。**