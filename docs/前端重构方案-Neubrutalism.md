# LunchFate 前端重构方案 · 新粗野主义（Neubrutalism）

| 项目   | 内容                                                                              |
| ---- | ------------------------------------------------------------------------------- |
| 文档名称 | LunchFate 前端重构方案 · 新粗野主义（Neubrutalism）                                          |
| 版本   | v1.0                                                                            |
| 编写日期 | 2026-10-13                                                                      |
| 状态   | **待评审**（本方案为纯文档产出，尚未改动任何源码）                                                     |
| 配套文档 | `docs/需求文档.md`（功能基线）、`AGENTS.md`（仓库级规范）、`src/LunchFate.Blazor/AGENT.md`（前端开发引导） |
| 目标读者 | 后续参与前端重构与维护的开发者 / AI 编码代理                                                       |

---

## 0. 结论摘要

| 决策项   | 结论                                                                                                                       |
| ----- | ------------------------------------------------------------------------------------------------------------------------ |
| 重构深度  | **中等**：自建应用外壳替换 ABP BasicTheme `MainLayout`；全部自研页面与组件重写为 Neubrutalism；ABP 内置页（Identity / 租户 / 设置）与登录页仅用 CSS 覆盖层对齐，不重写其标记 |
| 组件库策略 | **保留 Blazorise**（DataGrid / Modal / Card / 表单），在其 Bootstrap 5 类之上做覆盖层；不引入自研表格/弹窗组件                                       |
| 主色方案  | **明黄 `#FFD23F` + 纯黑 `#000` + 米白 `#FFFDF5`**，高饱和青/粉/橙/绿作状态与强调色                                                            |
| 样式技术  | 原生 CSS + CSS 自定义属性（设计令牌），**不引入 npm / SASS / PostCSS**；维持 `dotnet build` 为唯一构建入口                                          |
| 令牌存放  | 唯一真源 `src/LunchFate.Blazor.Client/wwwroot/styles/tokens.css`；两个 bundle（MVC / Blazor+WASM）引用同一批文件，不复制粘贴                   |
| 暗色主题  | v1 **不做**，但令牌集中定义，为后续留出切换口                                                                                               |
| 功能改动  | **零**。不改后端、DTO、接口、权限、路由；不改 `global-scripts.js` 的 ES 模块契约；不改转盘的几何计算                                                       |

---

## 1. 目标与非目标

### 1.1 目标

1. **视觉统一**：全站（含登录页、ABP 内置页）呈现同一套 Neubrutalism 语言——粗黑描边、零模糊硬阴影、高饱和色块、粗字重、直角/微圆角。
2. **规范沉淀**：把视觉决策固化为**设计令牌 + 组件类**，新页面靠"用令牌、用组件类"即可获得一致外观，不依赖个人审美。
3. **结构改善**：借重构拆分超长组件（`Shops.razor` 813 行、`NearbyShopSearch.razor` 716 行、`Home.razor` 481 行），把可复用的展示片段抽成自有 UI 组件。
4. **零功能回归**：所有需求文档验收标准（§9，12 条）保持通过；`dotnet build` 0 警告 0 错误，92 个测试全绿。
5. **可维护性**：样式加载链路可解释、可预测，避免"为什么我的样式没生效"这类反复排查。

### 1.2 非目标（本轮明确不做）

- ❌ 不改任何后端代码（Domain / Application / EF Core / HttpApi / Integration）。
- ❌ 不改路由、菜单结构、权限点、DTO 形状、接口契约。
- ❌ 不重写 ABP 内置页（`/identity/users` 用户管理、租户管理、设置管理）的 Razor 标记——它们是编译在 NuGet 包里的组件，只做 CSS 覆盖。
- ❌ 不重写登录页（MVC 视图来自 `AbpAccountWebOpenIddictModule`），只做 CSS 覆盖。
- ❌ 不引入前端构建工具链（无 `package.json` 构建脚本改动、无 SASS、无 Tailwind、无 webpack/vite）。
- ❌ 不引入外网 CDN 字体或图标库（部署环境可能无外网，且高德 Key 有域名白名单约束）。
- ❌ 不做暗色主题、不做动画特效升级、不做布局大改（保持现有信息架构）。
- ❌ 不改 `global-scripts.js` 的导出契约（`loadAmap` / `getCurrentPosition` / `renderShopMap` / `getDefaultCenter` / `setDefaultCenter` / `clearDefaultCenter`）。

---

## 2. 现状盘点

### 2.1 前端资产清单

| 文件                                                                   | 行数    | 现状                                                   | 本轮动作                                                   |
| -------------------------------------------------------------------- | ----- | ---------------------------------------------------- | ------------------------------------------------------ |
| `src/LunchFate.Blazor/Components/App.razor`                          | 63    | HTML 外壳，挂 ABP bundle、scoped CSS、error UI             | **改**：加 `theme-color`、字体 preload、manifest 对齐           |
| `src/LunchFate.Blazor.Client/Routes.razor`                           | 13    | `DefaultLayout="typeof(MainLayout)"`（ABP BasicTheme） | **改**：指向自建 `LunchFate.Blazor.Client.Layout.MainLayout` |
| `src/LunchFate.Blazor/wwwroot/global-styles.css`                     | 25    | 仅 `#blazor-error-ui`                                 | **退役**：内容迁入 `styles/base.css`                          |
| `src/LunchFate.Blazor.Client/wwwroot/main.css`                       | 44    | `.loader` + `#blazor-error-ui`（与上者重复）                | **重写**：去重，loader 改为 `.nb-spinner`                      |
| `src/LunchFate.Blazor.Client/Pages/Home.razor`                       | 481   | 转盘 + 结果弹窗 + 附近搜索挂载                                   | **拆分 + 重写样式**，`@code` 逻辑逐行保留                           |
| `src/LunchFate.Blazor.Client/Pages/Home.razor.css`                   | 204   | scoped 样式，含颜色硬编码                                     | **拆分**：颜色/边框/阴影 → 全局令牌；几何/定位留 scoped                   |
| `src/LunchFate.Blazor.Client/Pages/Shops.razor`                      | 813   | Card + 筛选 + DataGrid + 4 个 Modal                     | **重写样式 + 抽组件**（弹窗、工具栏、筛选区）                             |
| `src/LunchFate.Blazor.Client/Pages/DrawRecords.razor`                | 4     | `<RecordList IsDraw="true" />`                       | **不改**                                                 |
| `src/LunchFate.Blazor.Client/Pages/ShopCreationRecords.razor`        | 4     | `<RecordList IsDraw="false" />`                      | **不改**                                                 |
| `src/LunchFate.Blazor.Client/Pages/Permissions.razor`                | 22    | 说明 + 内嵌 ABP `UserManagement`                         | **改**：自身标记样式重写；内嵌部分靠覆盖层                                |
| `src/LunchFate.Blazor.Client/Components/NearbyShopSearch.razor`      | 716   | 附近搜索（含地图、分页、批量加入）                                    | **重写样式 + 抽 `NearbyPoiItem`**                           |
| `src/LunchFate.Blazor.Client/Components/RecordList.razor`            | 292   | 筛选表单 + 原生 `<table>` + 分页                             | **重写样式 + 抽 `RecordFilterBar`**，表用 `.nb-table`          |
| `src/LunchFate.Blazor.Client/Components/ShopMap.razor`               | 245   | 高德地图容器 + 降级文案                                        | **只改容器样式**；容器常驻逻辑一字不动                                  |
| `src/LunchFate.Blazor.Client/Components/WeatherWidget.razor`         | 60    | 天气小组件                                                | **重写样式**为 `.nb-chip`                                   |
| `src/LunchFate.Blazor.Client/Navigation/LunchFateMenuContributor.cs` | 118   | 菜单数据（FontAwesome 图标）                                 | **不改**（图标可换更粗的 FA Solid 款式，可选）                         |
| `src/LunchFate.Blazor/LunchFateBlazorModule.cs`                      | 377   | `ConfigureBundles` 注册样式包                             | **改**：注册 `styles/*.css`                                |
| `src/LunchFate.Blazor/LunchFateStyleBundleContributor.cs`            | 11    | 只加 `main.css`                                        | **改**：按确定顺序加入全部样式文件                                    |
| `src/LunchFate.Blazor.Client/wwwroot/manifest.json`                  | 14    | `theme_color: #03173d`（模板深蓝）                         | **改**：`#FFD23F` / `#FFFDF5`；图标重做                       |
| `src/LunchFate.Blazor/wwwroot/favicon.ico`                           | 38 KB | ABP 模板图标                                             | **改**（可选，建议做）                                          |

### 2.2 现有样式加载链路（重构必须理解）

```
                        ┌─ MVC 侧（登录页 / 错误页）
AbpBundlingOptions ─────┤   BasicThemeBundles.Styles.Global
                        │     └─ /global-styles.css                ← 服务端 wwwroot
                        │
                        ├─ Blazor SSR 侧
                        │   BlazorBasicThemeBundles.Styles.Global
                        │     └─ /global-styles.css
                        │
                        └─ Blazor WASM 侧
                            BlazorWebAssemblyStandardBundles.Styles.Global
                              └─ LunchFateStyleBundleContributor → main.css   ← 客户端 wwwroot

App.razor 额外直链 scoped CSS 包：
  LunchFate.Blazor.styles.css         （服务端组件的 .razor.css）
  LunchFate.Blazor.Client.styles.css  （客户端组件的 .razor.css，如 Home.razor.css）
```

**关键事实（决定了重构手法）：**

1. **两个 `wwwroot` 都挂在站点根 `/`**：`src/LunchFate.Blazor/wwwroot/` 与 `src/LunchFate.Blazor.Client/wwwroot/` 的静态资源在运行时合并到同一根路径。因此把全部新样式放在**客户端** `wwwroot/styles/` 下，MVC bundle 与 WASM bundle 都能引用同一批 URL，天然满足"令牌唯一真源"。
2. **`App.razor` 用 `InteractiveAuto`**：首屏由服务端渲染，之后交互式会话在 WASM 渲染。**同一份 CSS 必须同时被 SSR 与 WASM 两条链路加载**，否则会出现"首屏无样式、刷新后才有"（项目里高德 Key 踩过同样的坑，见 `AGENTS.md`）。
3. **ABP BasicTheme 的 `MainLayout` 是 NuGet 包内已编译组件**（`Volo.Abp.AspNetCore.Components.Web.BasicTheme`），无法修改其标记，只能整体替换。
4. **框架样式（Bootstrap 5 / Blazorise / ABP `theme.css`）不在 CSS `@layer` 中**，因此**自己的样式放进 `@layer` 会输给它们**（未分层样式优先级高于任何分层样式）。→ 结论见 §4.5。

---

## 3. 技术决策

### 3.1 决策一：自建应用外壳，替换 ABP `MainLayout`

**做法**：新建 `src/LunchFate.Blazor.Client/Layout/MainLayout.razor`（命名空间 `LunchFate.Blazor.Client.Layout`），`Routes.razor` 的 `DefaultLayout` 指向它，移除对 `Volo.Abp.AspNetCore.Components.Web.BasicTheme.Themes.Basic` 的 `@using`。

**理由**：ABP 外壳是 Bootstrap 标记（`navbar` / `dropdown-menu` / `container`），CSS 覆盖到"粗黑描边 + 硬阴影"的程度需要大量高特异性选择器，脆弱且难维护；而侧边栏/顶栏正是 Neubrutalism 最大的视觉面。

**必须保留的功能能力清单**（自建外壳的验收基线，缺一即回归）：

| #  | 能力                      | 说明                                                                                       |
| -- | ----------------------- | ---------------------------------------------------------------------------------------- |
| 1  | 主菜单渲染                   | 注入 `IMenuManager`，`GetAsync(StandardMenus.Main)`；保留 `LunchFateMenuContributor` 的全部菜单项与排序 |
| 2  | 二级菜单（Administration 折叠） | 管理组下的店铺管理/权限管理/用户管理/租户/设置                                                                |
| 3  | 菜单权限过滤                  | 沿用 `ApplicationMenuItem` 的权限可见性（由 ABP 菜单系统处理）                                            |
| 4  | 当前用户显示                  | 用户名 / 租户名                                                                                |
| 5  | 用户菜单                    | "我的账号"（跳授权服务器 `Account/Manage`，`_blank`）、**登出**                                          |
| 6  | 消息提示挂载点                 | `<PageAlert />`（ABP `UiMessageAlert` / `UiNotificationAlert`）                            |
| 7  | 页面进度条                   | `<UiPageProgress />`                                                                     |
| 8  | 面包屑                     | 自建组件，从当前路由 + 菜单匹配生成（ABP 的 `Breadcrumbs` 是 Bootstrap 标记，不复用）                              |
| 9  | 语言切换                    | ABP 语言选择器（若当前模板未启用则不新增）                                                                  |
| 10 | 移动端侧栏抽屉                 | 汉堡按钮 + 全屏覆盖 + 焦点管理                                                                       |

> **实施第一步**：在浏览器 DevTools 中 dump 现有外壳的真实 DOM 与 `@code` 行为，逐项对照上表，避免"看起来一样但少了登出"这类静默功能丢失。

### 3.2 决策二：保留 Blazorise，做覆盖层

**做法**：继续使用 `Card` / `Modal` / `DataGrid` / `Form` 等 Blazorise 组件；新建 `styles/overrides/blazorise.css` 覆盖其 Bootstrap 5 类。

**理由**：

- ABP 内置页（Identity 用户管理、租户、设置）本身由 Blazorise 渲染，替换组件库会连带破坏这些页面。
- `DataGrid` 承载了服务端分页/排序（`ReadData`）、跨页多选（`FR-S-06`）等行为逻辑，自研替换的回归成本远高于视觉收益。

**约束**：覆盖层只允许改"外观属性"（颜色、边框、圆角、阴影、字重、间距），**不得改变 DOM 结构或尺寸计算**，避免影响 `DataGrid` 的列宽/滚动逻辑。

### 3.3 决策三：原生 CSS + 设计令牌，不引入构建工具链

**理由**：项目当前没有前端构建步骤（`abp install-libs` 只装第三方 libs），引入 SASS/Vite 会把"改样式"变成"改样式 + 跑构建 + 提交产物"，与现有 `dotnet build` 单一入口的简洁性冲突，也与 `AGENTS.md` 强调的最小改动原则相悖。

**代价与缓解**：原生 CSS 没有嵌套与变量运算 → 用 CSS 自定义属性 + `clamp()` / `min()` 覆盖 90% 场景；剩余 10%（如按令牌批量生成工具类）用"手写但成组"的方式，明确写在 `AGENT.md` 里。

### 3.4 决策四：样式文件分层，按固定顺序注册进 bundle

```
src/LunchFate.Blazor.Client/wwwroot/styles/
├── tokens.css              ① 设计令牌（唯一真源，只允许 :root 变量）
├── base.css                ② reset、排版、链接、滚动条、error UI、无障碍
├── layout.css              ③ 应用外壳（topbar / sidenav / breadcrumb / footer）
├── components.css          ④ 自有 UI 组件（.nb-btn / .nb-card / .nb-table …）
├── pages.css               ⑤ 页面专属样式（转盘、店铺页、记录页）
└── overrides/
    ├── blazorise.css       ⑥ Blazorise / Bootstrap 5 覆盖
    └── framework.css       ⑦ ABP 内置页 + 登录页覆盖（含 MVC 侧）
```

注册方式（`LunchFateBlazorModule.ConfigureBundles` + `LunchFateStyleBundleContributor`）：

- `BasicThemeBundles.Styles.Global`（MVC，登录页）→ `tokens.css`、`base.css`、`overrides/framework.css`
- `BlazorBasicThemeBundles.Styles.Global`（Blazor SSR）→ 同上
- `BlazorWebAssemblyStandardBundles.Styles.Global`（WASM，经 contributor）→ ①~⑦ 全部，**按序号顺序**

**不使用 `@import`**：`@import` 会产生串行额外请求；ABP bundling 会把同一 bundle 的文件按加入顺序合并，顺序即层叠顺序。

> ⚠️ **需实测项**：`BundleContributor` 中加入的文件是否真的排在主题模块的 Bootstrap/Blazorise 之后。验收方法：浏览器 DevTools 查看最终 `<link rel="stylesheet">` 顺序，以及 `getComputedStyle` 里 `.nb-btn` 与 `.btn` 的胜负关系。若顺序不理想，改用"提高选择器特异性"策略（见 §4.5），不引入 `!important` 泛滥。

### 3.5 决策五：scoped CSS 只保留结构相关样式

`Home.razor.css` 这类 `*.razor.css` 会编译进 `LunchFate.Blazor.Client.styles.css`，其加载顺序在 bundle 之外、不可控。因此：

- **scoped CSS 只允许写**：尺寸、定位、网格、几何变换、`z-index`。
- **禁止在 scoped CSS 写**：颜色、边框、圆角、阴影、字体 —— 一律走全局令牌与 `.nb-*` 类。

**理由**：避免"改了令牌但组件不跟着变"，以及 scoped CSS 与全局样式互相打架时难以定位。

---

## 4. 视觉规范（设计令牌）

### 4.1 配色

#### 基础色

| 令牌                 | 值         | 用途                    |
| ------------------ | --------- | --------------------- |
| `--nb-ink`         | `#000000` | **唯一的黑**：边框、正文、硬阴影    |
| `--nb-ink-soft`    | `#1A1A1A` | 次级正文                  |
| `--nb-ink-mute`    | `#4A4A4A` | 辅助文字（仍满足 AA 对比度）      |
| `--nb-paper`       | `#FFFDF5` | 页面底色（米白，比纯白柔和，衬出白色卡片） |
| `--nb-surface`     | `#FFFFFF` | 卡片 / 弹窗 / 输入框表面       |
| `--nb-surface-alt` | `#FFF8E1` | 次级表面：卡片头、表头、表格斑马纹     |

#### 主色与状态色

| 令牌                    | 值         | 用途                      |
| --------------------- | --------- | ----------------------- |
| `--nb-primary`        | `#FFD23F` | 主色：主按钮、卡片头、表头、选中态、转盘指针底 |
| `--nb-primary-hover`  | `#FFE066` | 主色 hover                |
| `--nb-primary-active` | `#F5C400` | 主色 active（按下）           |
| `--nb-success`        | `#3DDC97` | 启用、成功、批量启用              |
| `--nb-warning`        | `#FFB703` | 警告、批量停用                 |
| `--nb-danger`         | `#FF4D4D` | 删除、错误、危险操作              |
| `--nb-info`           | `#4CC9F0` | 提示、信息                   |

> **状态色语义固定**：`success` 恒表示"启用/成功"，`warning` 恒表示"停用/注意"，`danger` 恒表示"删除/错误"。**不得**因视觉好看而跨语义借用（例如用绿色做普通强调）。

#### 强调色（装饰、图标底、转盘扇区）

| 令牌                   | 值         |
| -------------------- | --------- |
| `--nb-accent-cyan`   | `#4CC9F0` |
| `--nb-accent-pink`   | `#FF6B9D` |
| `--nb-accent-orange` | `#FF8A3D` |
| `--nb-accent-purple` | `#B388FF` |
| `--nb-accent-lime`   | `#C4F000` |

#### 转盘扇区色环（替换现有 12 色）

```
#FFD23F  #4CC9F0  #FF6B9D  #3DDC97  #FF8A3D  #B388FF
#FF4D4D  #C4F000  #00B4D8  #FF9E00  #7B61FF  #FFB703
```


- 扇区文字统一**纯黑 `#000`**，去掉现有的 `text-shadow` 白字方案（现有配色是浅色底 + 白字 + 描边，可读性差）。
- 相邻扇区保证明度/色相错开；超过 12 个门店时循环取色（逻辑不变）。

### 4.2 字体与排版

```css
--nb-font-sans:    system-ui, -apple-system, "Segoe UI", "PingFang SC",
                   "Microsoft YaHei", "Noto Sans SC", sans-serif;
--nb-font-display: "Archivo Black", var(--nb-font-sans);  /* 仅西文/数字/拉丁标题 */
--nb-font-mono:    ui-monospace, "JetBrains Mono", Consolas, monospace;
```

| 令牌                    | 值         | 用途                      |
| --------------------- | --------- | ----------------------- |
| `--nb-fs-xs`          | `12px`    | 辅助说明、表格次要列              |
| `--nb-fs-sm`          | `14px`    | 表单标签、表格正文               |
| `--nb-fs-base`        | `16px`    | 正文基准                    |
| `--nb-fs-lg`          | `18px`    | 卡片标题                    |
| `--nb-fs-xl`          | `22px`    | 区块标题                    |
| `--nb-fs-2xl`         | `28px`    | 页面标题                    |
| `--nb-fs-3xl`         | `36px`    | 首页主标题                   |
| `--nb-fs-4xl`         | `48px`    | 空状态/结果卡片名称              |
| `--nb-lh-tight`       | `1.15`    | 标题                      |
| `--nb-lh-base`        | `1.6`     | 正文                      |
| `--nb-tracking-tight` | `-0.02em` | 大标题收紧字距                 |
| `--nb-weight-body`    | `500`     | 正文（**不用 400**，粗野主义整体偏粗） |
| `--nb-weight-bold`    | `700`     | 强调、按钮、标签                |
| `--nb-weight-black`   | `900`     | 页面标题、转盘按钮               |

**字体策略（重要）**：

- 中文**不引入**网络字体（`Noto Sans SC` 全量约数 MB，会显著拖慢 Blazor WASM 首屏）。中文标题靠系统字体的 `font-weight: 900` + `letter-spacing: -0.02em` 达成粗野观感。
- 西文/数字标题可选**自托管** `Archivo Black`（woff2，约 30 KB）到 `src/LunchFate.Blazor/wwwroot/fonts/`，**不走 CDN**。若嫌额外资产，可全部用 `--nb-font-sans`，本方案不强制。
- 数字（人均价格、评分、距离、页码）统一 `font-variant-numeric: tabular-nums`，避免表格数字跳动。

### 4.3 边框与圆角

| 令牌                   | 值               | 用途                     |
| -------------------- | --------------- | ---------------------- |
| `--nb-border-w-sm`   | `2px`           | 小控件：复选框、徽章、chip        |
| `--nb-border-w`      | `3px`           | **默认**：按钮、输入框、表格、弹窗、侧栏 |
| `--nb-border-w-lg`   | `4px`           | 强调：顶栏、卡片、转盘外圈          |
| `--nb-border-color`  | `var(--nb-ink)` | 所有边框恒为纯黑               |
| `--nb-radius`        | `4px`           | 卡片 / 按钮 / 输入框（微圆角）     |
| `--nb-radius-sm`     | `2px`           | 表格单元格、小标签              |
| `--nb-radius-pill`   | `999px`         | 徽章、chip、状态点            |
| `--nb-radius-circle` | `50%`           | 转盘、中心按钮、头像             |

**规则**：

- 边框**必须**是纯黑实线；**禁止**彩色边框（状态用背景色表达，不用边框色）。
- **禁止** 1px 边框 —— 太细，不符合粗野主义，且在缩放/高分屏下观感不稳。
- **禁止**虚线边框（现有空状态用了 `1px dashed`，改为 `3px solid` + 硬阴影）。

### 4.4 阴影（硬阴影，零模糊）

| 令牌                  | 值                                 |
| ------------------- | --------------------------------- |
| `--nb-shadow-xs`    | `2px 2px 0 var(--nb-ink)`         |
| `--nb-shadow-sm`    | `3px 3px 0 var(--nb-ink)`         |
| `--nb-shadow`       | `4px 4px 0 var(--nb-ink)`         |
| `--nb-shadow-lg`    | `6px 6px 0 var(--nb-ink)`         |
| `--nb-shadow-xl`    | `8px 8px 0 var(--nb-ink)`         |
| `--nb-shadow-inset` | `inset 3px 3px 0 rgba(0,0,0,.12)` |

**交互位移规则（全站统一）**：

```css
/* 静息 */
box-shadow: var(--nb-shadow);
/* hover：向左上"抬起"，阴影变大 */
transform: translate(-2px, -2px);
box-shadow: var(--nb-shadow-lg);
/* active：向右下"按下"，阴影归零 */
transform: translate(2px, 2px);
box-shadow: none;
```

**硬性约束**：

- `box-shadow` 的**模糊半径必须为 0**。**禁止** `rgba()` 模糊阴影（现有代码里 `0 10px 30px rgba(0,0,0,.18)` 这类全部替换）。
- **禁止**渐变背景（`linear-gradient` / `radial-gradient`）。唯一例外：转盘的 `conic-gradient`（承载扇区语义，非装饰）。
- 位移只在 `hover` / `active` / `focus-visible` 使用，**禁止**用于常驻布局偏移。

### 4.5 层叠与特异性策略（避坑重点）

**不要用 `@layer` 包自己的样式。** 原因：Bootstrap / Blazorise / ABP `theme.css` 均为未分层样式，未分层样式优先级**高于**任何分层样式，把 `.nb-btn` 放进 `@layer` 会让它彻底输给 `.btn`。

**推荐顺序（从优到劣）**：

1. **文件顺序**：确保 `styles/*.css` 在 bundle 中排在框架样式之后（§3.4 的实测项）。
2. **选择器特异性**：覆盖框架组件时用"同类双选择器"，如 `.nb-btn.btn`、`.nb-card.card`、`.nb-table.table`，比裸 `.btn` 高一级且语义清晰。
3. **`!important`**：**仅**在覆盖 Blazorise 生成的内联样式或框架 `!important` 规则时使用，且**必须同行注释说明原因**，例如：
   ```css
   /* !important：Blazorise Modal 以行内 style 设置 padding，必须强制覆盖 */
   .nb-modal .modal-body { padding: var(--nb-space-5) !important; }
   ```
4. **禁止**"整文件 `!important`"或"为了省事加 `!important`"。

### 4.6 间距与栅格

| 令牌             | 值      |   | 令牌             | 值      |
| -------------- | ------ | - | -------------- | ------ |
| `--nb-space-1` | `4px`  |   | `--nb-space-5` | `24px` |
| `--nb-space-2` | `8px`  |   | `--nb-space-6` | `32px` |
| `--nb-space-3` | `12px` |   | `--nb-space-7` | `48px` |
| `--nb-space-4` | `16px` |   | `--nb-space-8` | `64px` |

- 基准 4px 网格；**8px 为常用步长**。
- 组件内边距：`--nb-space-4`；区块间距：`--nb-space-5`；页面级间距：`--nb-space-6`。
- **栅格沿用 Bootstrap 5 断点**（`576 / 768 / 992 / 1200 / 1400`），因为 Blazorise 基于 Bootstrap 5 栅格，混用自定义断点会造成"栅格在某个宽度断了但组件没断"。

### 4.7 动效

| 令牌              | 值                                |
| --------------- | -------------------------------- |
| `--nb-dur-fast` | `120ms`                          |
| `--nb-dur-base` | `180ms`                          |
| `--nb-dur-slow` | `320ms`                          |
| `--nb-ease`     | `cubic-bezier(0.2, 0.8, 0.2, 1)` |

- 过渡只作用于 `transform` / `box-shadow` / `background-color` / `border-color` / `opacity`。
- **转盘旋转动画的参数一律不动**：`5s` + `cubic-bezier(0.15, 0.6, 0.2, 1)` + 6 圈，是 `FR-W-02` 与 `WinnerIndex` 对齐的既有实现（见 `Home.razor` 的 `SpinSeconds` / `_transition`）。
- **必须支持** `prefers-reduced-motion: reduce`：关闭位移与旋转，保留状态变化的即时反馈。

### 4.8 无障碍（硬性）

- 所有交互元素 `:focus-visible` 必须有可见焦点环：`outline: 3px solid var(--nb-ink); outline-offset: 2px;`（黄底元素上用黑色环 + 2px 白外圈保证可见）。
- 纯装饰色块必须满足正文对比度 ≥ 4.5:1：黑字 + 明黄/青/粉/绿均达标；**浅色文字禁止**放在 `--nb-primary` 上。
- 状态不能只靠颜色传达（徽章带文字"启用/停用"，不只用红绿）。
- 移动端触控目标 ≥ `44 × 44px`。

---

## 5. 整体布局设计

### 5.1 应用外壳（桌面 ≥ 992px）

```
┌───────────────────────────────────────────────────────────────────────┐
│  ▓▓ TOPBAR  高 64px  白底 · 底边 4px 纯黑 · 底部硬阴影 4px             │
│  ┌────┐                                                    ┌────────┐ │
│  │🍽 │  LunchFate · 午餐命运    (字重 900, 字距 -0.02em)     │ 👤 用户 ▾│ │
│  └────┘                                                    └────────┘ │
├──────────────────┬────────────────────────────────────────────────────┤
│  SIDENAV  248px  │  MAIN                                               │
│  白底 · 右边 4px │  ┌──────────────────────────────────────────────┐  │
│  纯黑 · 无阴影   │  │ 首页 / 管理 / 店铺管理      ← 面包屑 14px     │  │
│                  │  └──────────────────────────────────────────────┘  │
│  ┌────────────┐  │  ┌──────────────────────────────────────────────┐  │
│  │ 🏠 首页     │◄─│  │                                              │  │
│  └────────────┘  │  │   PAGE CONTENT                               │  │
│   黄底 3px 黑边  │  │   max-width 1200px · padding 32px            │  │
│   硬阴影 4px     │  │                                              │  │
│  ┌────────────┐  │  │                                              │  │
│  │ 🕘 抽取记录 │  │  │                                              │  │
│  └────────────┘  │  │                                              │  │
│  ┌────────────┐  │  │                                              │  │
│  │ 📋 门店记录 │  │  │                                              │  │
│  └────────────┘  │  │                                              │  │
│                  │  └──────────────────────────────────────────────┘  │
│  ▾ 管理          │                                                    │
│    ├ 店铺管理    │                                                    │
│    └ 权限管理    │                                                    │
│                  │                                                    │
└──────────────────┴────────────────────────────────────────────────────┘
```

**外壳组成与样式要点**

| 区域            | 结构                  | 样式要点                                                                                                                 |
| ------------- | ------------------- | -------------------------------------------------------------------------------------------------------------------- |
| `TopBar`      | 品牌 + 用户菜单 + 移动端汉堡   | 白底、底边 `4px` 纯黑、底部硬阴影 `--nb-shadow`、高 `64px`；品牌文字 `900` 字重                                                            |
| `SideNav`     | `IMenuManager` 主菜单  | 白底、右边 `4px` 纯黑、**无阴影**（避免与内容区阴影打架）；菜单项默认透明，当前项 `--nb-primary` 底 + `3px` 黑边 + `--nb-shadow-sm`；图标与文字间距 `--nb-space-3` |
| `Breadcrumbs` | 自建，从路由 + 菜单推导       | 纯文本 `--nb-fs-sm`，分隔符 `/`，字重 `700`；末级不可点                                                                              |
| `UserMenu`    | 用户名 + 下拉（我的账号 / 登出） | 触发按钮同 `.nb-btn` 外观；下拉面板白底 `3px` 黑边 + `--nb-shadow-lg`，无圆角过渡                                                          |
| `AppFooter`   | 可选                  | 白底、上边 `3px` 纯黑、`--nb-fs-xs`                                                                                          |

### 5.2 移动端（< 992px）

- `SideNav` 变为**抽屉**：`position: fixed`，宽 `min(84vw, 320px)`，右侧 `4px` 纯黑边 + `--nb-shadow-xl`；打开时遮罩 `rgba(0,0,0,.55)`。
- `TopBar` 左侧出现汉堡按钮（`44×44px` 触控目标，`.nb-btn` 外观）。
- 抽屉打开时锁 `body` 滚动，`Esc` 关闭，焦点回到汉堡按钮。
- **弹窗改为底部抽屉**（需求文档 §3.1 交互细节已要求）：`Modal` 在 `≤768px` 时 `align-items: flex-end`、宽度 `100%`、顶部 `4px` 纯黑边 + 向上硬阴影。

### 5.3 首页 · 午餐转盘布局

```
┌───────────────────────────────────────────────────────────┐
│  午餐命运                              ┌────────────────┐  │
│  让转盘替你决定                          │ ☀️ 晴 24°C    │  │ ← WeatherWidget → .nb-chip
│  (--nb-fs-3xl / 900)                     │ 🌧 就近选择   │  │
│                                          └────────────────┘  │
│                                                             │
│                    ▼  ← 指针：纯黑三角 + 无模糊投影          │
│           ╭───────────────────────╮                         │
│          ╱  扇区（12 色环，黑字）   ╲                        │ ← 4px 纯黑外圈
│         │    ┌───────────────┐      │                       │   硬阴影 8px
│         │    │   开 始 ！    │      │ ← .nb-wheel-hub       │   无白边、无模糊
│         │    │  黄底黑边      │      │   （--nb-radius-circle）│
│         │    └───────────────┘      │                       │
│          ╲                         ╱                        │
│           ╰───────────────────────╯                         │
│                                                             │
│              已加入 3 家附近门店    [ 清空附近候选 ]          │
│                                                             │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  🗺 附近店铺搜索        （NearbyShopSearch · .nb-card） │  │
│  │  关键字 [______]  类型 [餐饮▾]  半径 [3km▾]            │  │
│  │  [ 搜索附近 ] [ 定位 ] [ 设为默认 ] [ 恢复默认 ]        │  │
│  │  ┌────────── 地图容器 3px 黑边 ──────────┐             │  │
│  │  └────────────────────────────────────────┘             │  │
│  │  ▢ 老王面馆          · 320m   ← .nb-list-item           │  │
│  │  ▢ 沙县小吃          · 480m                             │  │
│  │  [ 加入本次转盘 (2) ]                                   │  │
│  └───────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────┘
```

**结果弹窗（`FR-W-03` / `FR-W-05`）**：

```
┌─────────────────────────────────────────┐
│  今日午餐之选  🎉                  [ ✕ ] │ ← 弹窗头：黄底 + 3px 黑边
├─────────────────────────────────────────┤
│  ┌───────────────────────────────────┐  │
│  │  老王面馆                          │  │ ← --nb-fs-4xl / 900
│  │  ✓ 已保存到抽取记录                 │  │ ← --nb-success 文字
│  │  分类      面食                     │  │
│  │  人均      ¥ 28                     │  │ ← tabular-nums
│  │  地址      ××路 123 号              │  │
│  │  🧭 点击卡片唤起高德导航             │  │
│  └───────────────────────────────────┘  │ ← 整卡可点（保留现有 <a> 行为）
├─────────────────────────────────────────┤
│  [ 就吃这家 ]  [ 再来一次 ]  [ 查看记录 ] │ ← 主按钮黄，次按钮白
└─────────────────────────────────────────┘
```

### 5.4 店铺管理页布局

```
┌──────────────────────────────────────────────────────────────┐
│ ┌─ 店铺管理 ─────────────────────────────[ + 新增店铺 ] ─┐   │ ← .nb-card__header 黄底
│ ├────────────────────────────────────────────────────────┤   │
│ │ 关键字[____] 分类[全部▾] 状态[全部▾] [搜索][重置]        │   │ ← .nb-toolbar
│ │                                                          │   │
│ │ ▸ 附近店铺搜索（可折叠，默认收起）                        │   │
│ │                                                          │   │
│ │ 已选 3 家  ▢ 全选本页  [清空选择]   [批量启用][批量停用]  │   │ ← .nb-toolbar--sticky
│ │                                                          │   │
│ │ ┌──┬────────┬──────┬──────┬─────┬──────┬────┬──────┬──┐ │   │
│ │ │▢ │ 名称   │ 分类 │ 人均 │评分 │ 地址 │地图│ 状态 │操│ │   │ ← .nb-table
│ │ ├──┼────────┼──────┼──────┼─────┼──────┼────┼──────┼──┤ │   │   表头黄底 3px 黑边
│ │ │▢ │老王面馆│ 面食 │ ¥28  │ 4.5 │ ...  │[地图]│启用 │…│ │   │   斑马纹 白/米白
│ │ └──┴────────┴──────┴──────┴─────┴──────┴────┴──────┴──┘ │   │
│ │                        分页 [上一页] 1/3 [下一页]         │   │
│ └────────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────┘
```


- 表单弹窗：两列栅格（`≥768px`），必填标签带 `*` 与 `--nb-danger` 色；错误信息 `--nb-fs-sm` + `--nb-danger`，**不使用** `alert` 样式，避免与业务错误提示混淆。
- 删除 / 批量确认弹窗：标题栏用 `--nb-danger` 底 + 白字？→ **否**。统一用黄底标题栏，正文里用 `--nb-danger` 强调"不可撤销"文案。理由：状态色语义固定，标题栏是装饰位，不应承载状态语义。

### 5.5 记录页布局

- 筛选区（`.nb-toolbar`）→ 说明文案（`.nb-hint`）→ 表格（`.nb-table`）→ 分页（`.nb-pagination`）。
- 现有 `table-striped` 替换为 `.nb-table`：表头 `--nb-surface-alt` + `3px` 黑边 + `900` 字重；行 `1px` 黑边（表格内部细线允许 1px，因为它不是"组件边框"而是分隔线）→ **改为 `2px` 以符合视觉语言**，并统一 `--nb-ink` 30% 透明度的分隔线色 `--nb-rule: rgba(0,0,0,.35)`。
  > 说明：`--nb-rule` 是本规范中**唯一**允许的"非纯色黑"边框色，专用于表格/列表内部的分隔线，避免表格过于沉重。组件外框仍必须是纯黑 `3px`。

---

## 6. 页面与组件改造清单

### 6.1 新建文件

**外壳层 `src/LunchFate.Blazor.Client/Layout/`**

| 文件                     | 职责                                                                                   |
| ---------------------- | ------------------------------------------------------------------------------------ |
| `MainLayout.razor`     | 外壳骨架：`TopBar` + `SideNav` + `Breadcrumbs` + `PageAlert` + `UiPageProgress` + `@Body` |
| `MainLayout.razor.css` | 仅栅格/定位（`grid-template`、`position`、断点切换）                                              |
| `TopBar.razor`         | 品牌、汉堡、用户菜单挂载                                                                         |
| `TopBar.razor.css`     | 顶栏布局                                                                                 |
| `SideNav.razor`        | 注入 `IMenuManager`，渲染一/二级菜单，当前项高亮                                                     |
| `SideNav.razor.css`    | 侧栏与抽屉布局、断点                                                                           |
| `UserMenu.razor`       | 当前用户 + 我的账号 + 登出                                                                     |
| `Breadcrumbs.razor`    | 自建面包屑                                                                                |
| `AppFooter.razor`      | 页脚（可选）                                                                               |

**自有 UI 组件 `src/LunchFate.Blazor.Client/Components/Ui/`**

| 文件                   | 对应类               | 说明                                                                                 |
| -------------------- | ----------------- | ---------------------------------------------------------------------------------- |
| `NbButton.razor`     | `.nb-btn`         | `Color` / `Size` / `Disabled` / `Block` / `Icon` 参数；替代裸 `<button class="btn ...">` |
| `NbCard.razor`       | `.nb-card`        | `Title` / `TitleRight` / `Body` / `Footer` 片段；替代 Blazorise `Card` 的裸用              |
| `NbBadge.razor`      | `.nb-badge`       | `Color` 参数：`primary/success/warning/danger/info/neutral`                           |
| `NbAlert.razor`      | `.nb-alert`       | `Color` / `Icon` / `Dismissible`；承载业务错误/提示                                         |
| `NbEmptyState.razor` | `.nb-empty`       | 图标 + 标题 + 说明 + 操作按钮（转盘空状态、列表空状态复用）                                                 |
| `NbSpinner.razor`    | `.nb-spinner`     | Neubrutalism 方块旋转指示器；替代现有 `.loader` 与 `.wheel-spinner`                             |
| `NbPageHeader.razor` | `.nb-page-header` | 页面标题 + 副标题 + 右侧操作区                                                                 |
| `NbToolbar.razor`    | `.nb-toolbar`     | 筛选/批量操作条容器                                                                         |

**样式层 `src/LunchFate.Blazor.Client/wwwroot/styles/`**：`tokens.css`、`base.css`、`layout.css`、`components.css`、`pages.css`、`overrides/blazorise.css`、`overrides/framework.css`

**转盘子组件 `src/LunchFate.Blazor.Client/Components/Wheel/`**

| 文件                      | 职责                                                             |
| ----------------------- | -------------------------------------------------------------- |
| `LunchWheel.razor`      | 转盘本体：`conic-gradient` 背景、扇区标签、指针、中心按钮（几何计算从 `Home.razor` 原样搬入） |
| `WheelResultCard.razor` | 结果卡片（含导航链接 / 无坐标降级两态，`RenderFragment` 复用逻辑保留）                  |

### 6.2 改造顺序（7 个批次）

| 批次             | 内容                                                                                                                          | 交付物                    | 验收                                                  |
| -------------- | --------------------------------------------------------------------------------------------------------------------------- | ---------------------- | --------------------------------------------------- |
| **B0 样式地基**    | 建 `styles/` 全部文件骨架与令牌；改 `LunchFateStyleBundleContributor` + `LunchFateBlazorModule.ConfigureBundles`；退役 `global-styles.css` | 令牌可用，**不改任何 Razor 标记** | DevTools 确认样式加载顺序；`.nb-btn` 能压过 `.btn`；登录页底色已变米白    |
| **B1 外壳**      | 新建 `Layout/*`；`Routes.razor` 切 `DefaultLayout`                                                                              | 新外壳上线                  | §3.1 能力清单 10 项逐条通过；360/768/1024/1440 无横向滚动          |
| **B2 自有组件**    | `Components/Ui/*` + `components.css`                                                                                        | 8 个 Nb 组件              | 组件 Demo 页（临时）覆盖全部变体与状态                              |
| **B3 首页转盘**    | `LunchWheel` / `WheelResultCard` 抽取；`Home.razor` 瘦身；`WeatherWidget` 重写                                                      | 转盘页完成                  | `FR-W-01~06`、`FR-W-12~13` 回归；扇区顺序与 `WinnerIndex` 一致 |
| **B4 店铺页**     | `Shops.razor` 样式重写 + 弹窗/工具栏抽组件；`NearbyShopSearch` 重写 + `NearbyPoiItem`                                                      | 店铺页完成                  | `FR-S-01~06` 回归；`FR-W-14/15`、`FR-M-06` 回归；地图正常渲染与降级 |
| **B5 记录页与权限页** | `RecordList` 重写 + `RecordFilterBar`；`Permissions` 标记调整                                                                      | 两页完成                   | `FR-R-04` 回归；筛选/分页/跨页选择正常                           |
| **B6 覆盖层与收尾**  | `overrides/blazorise.css`、`overrides/framework.css`；登录页；ABP 内置页；`manifest.json` + favicon；`prefers-reduced-motion`          | 全站风格一致                 | 全站走查；`dotnet build` 0/0；`dotnet test` 92 全绿         |

**批次原则**：每批独立可验收、可回滚；每批结束跑一次完整构建 + 测试 + 手工回归；**不跨批次混合改动**。

---

## 7. 目录结构（重构后）

```
src/LunchFate.Blazor/
├── AGENT.md                     ← 前端开发引导规范（本次产出）
├── Components/App.razor         ← 改：meta / manifest / 字体 preload
├── LunchFateBlazorModule.cs     ← 改：样式 bundle 注册
├── LunchFateStyleBundleContributor.cs  ← 改：按序加入 styles/*.css
└── wwwroot/
    ├── fonts/                   ← 新增（可选）：自托管西文标题字体
    ├── global-scripts.js        ← 不动（ES 模块契约）
    └── global-styles.css        ← 退役（内容迁入 styles/base.css）

src/LunchFate.Blazor.Client/
├── Layout/                      ← 新增：应用外壳
│   ├── MainLayout.razor(+.css)
│   ├── TopBar.razor(+.css)
│   ├── SideNav.razor(+.css)
│   ├── UserMenu.razor
│   ├── Breadcrumbs.razor
│   └── AppFooter.razor
├── Components/
│   ├── Ui/                      ← 新增：自有 UI 组件
│   │   ├── NbButton.razor
│   │   ├── NbCard.razor
│   │   ├── NbBadge.razor
│   │   ├── NbAlert.razor
│   │   ├── NbEmptyState.razor
│   │   ├── NbSpinner.razor
│   │   ├── NbPageHeader.razor
│   │   └── NbToolbar.razor
│   ├── Wheel/                   ← 新增：转盘子组件
│   │   ├── LunchWheel.razor
│   │   └── WheelResultCard.razor
│   ├── NearbyShopSearch.razor   ← 重写样式 + 抽 NearbyPoiItem
│   ├── NearbyPoiItem.razor      ← 新增
│   ├── RecordList.razor         ← 重写样式 + 抽 RecordFilterBar
│   ├── RecordFilterBar.razor    ← 新增
│   ├── ShopMap.razor            ← 只改容器样式
│   └── WeatherWidget.razor      ← 重写样式
├── Pages/
│   ├── Home.razor(+.css)        ← 拆分 + 重写
│   ├── Shops.razor              ← 重写 + 抽组件
│   ├── DrawRecords.razor        ← 不动
│   ├── ShopCreationRecords.razor← 不动
│   └── Permissions.razor        ← 标记样式调整
├── Navigation/                  ← 不动
├── Routes.razor                 ← 改：DefaultLayout
└── wwwroot/
    ├── styles/                  ← 新增：全部样式（唯一真源）
    │   ├── tokens.css
    │   ├── base.css
    │   ├── layout.css
    │   ├── components.css
    │   ├── pages.css
    │   └── overrides/{blazorise.css, framework.css}
    ├── main.css                 ← 退役（合并进 styles/）
    └── manifest.json            ← 改：theme_color / background_color
```

---

## 8. 风险与缓解

| #   | 风险                                               | 影响                                 | 缓解措施                                                                                                               |
| --- | ------------------------------------------------ | ---------------------------------- | ------------------------------------------------------------------------------------------------------------------ |
| R1  | 自建外壳丢失 ABP 功能性能力（登出、语言切换、`PageAlert`、进度条、菜单权限过滤） | **高**：静默功能失效，不易发现                  | 以 §3.1 的 10 项能力清单为验收基线；实施前先 dump 现有外壳 DOM 与交互行为；B1 批次逐项走查并回归                                                       |
| R2  | 样式加载顺序不符合预期，覆盖层失效                                | **高**：视觉目标达不到，可能诱发 `!important` 泛滥 | B0 批次先用 DevTools 验证最终 `<link>` 顺序与 `getComputedStyle` 胜负；顺序不理想时改用"双类选择器"策略（§4.5），不引入 `!important`                  |
| R3  | `InteractiveAuto` 双端渲染导致首屏与交互态样式不一致              | **中**：刷新后样式跳变                      | 所有样式走两条 bundle（SSR + WASM），禁止依赖仅 WASM 可用的机制；B0 批次在"硬刷新首屏"与"导航后交互态"两种情形下各验证一次                                       |
| R4  | Blazorise `DataGrid` / `Modal` DOM 嵌套深，覆盖困难      | **中**：表格与弹窗风格不彻底                   | 覆盖层只改外观属性，不动结构与尺寸；用 `overrides/blazorise.css` 集中管理，必要时用 `!important` + 同行注释                                        |
| R5  | 转盘视觉改造误伤几何计算                                     | **高**：抽中结果与指针不一致（`FR-W-12` 级缺陷）    | 只改颜色/边框/阴影/字体；`conic-gradient` 角度、`SectorAngle()`、`_rotation` 计算、`WinnerIndex` 对应关系**逐行保留**；B3 批次专门回归"指针所指 = 弹窗结果" |
| R6  | 地图容器被样式改动破坏（`ShopMap.razor` 的容器必须常驻渲染树）          | **中**：地图不渲染                        | 只改容器 `border` / `border-radius` / `overflow`；**禁止**给容器加 `display`/`position` 的变更，**禁止**把容器放进条件块                    |
| R7  | 登录页与 ABP 内置页风格残留，视觉不统一                           | **低**：可接受，但需说明                     | 明确"覆盖层尽力对齐，不追求像素级一致"；`overrides/framework.css` 只覆盖底色、按钮、输入框、卡片、表头等高频元素                                             |
| R8  | 中文粗体在部分系统渲染过粗/过细                                 | **低**：观感不一致                        | 用 `font-weight: 900` + 系统字体栈；不引入网络中文字体（性能考虑）                                                                       |
| R9  | 移动端抽屉/弹窗的焦点与滚动锁未处理                               | **中**：可用性与无障碍问题                    | B1/B3 批次在 360px 视口实测；`Esc` 关闭、焦点回归、`body` 滚动锁写进组件 checklist                                                        |
| R10 | 一次性大范围改动导致回归难以定位                                 | **高**                              | 严格按 B0→B6 分批；每批独立可回滚；每批结束跑构建 + 92 测试 + 需求文档 §9 验收清单                                                                |

---

## 9. 验证计划

### 9.1 自动化

```bash
dotnet build LunchFate.slnx                                   # 必须 0 警告 / 0 错误
dotnet test LunchFate.slnx --no-build                         # 必须 92 个测试全绿
dotnet run --project src/LunchFate.Blazor                     # https://localhost:44352，admin / abc123!
```

> 前端重构不应影响测试结果。若测试失败，说明改到了不该改的地方（大概率是误改了 Razor 里的 C# 逻辑）。

### 9.2 视觉走查清单（每批次执行）

- [ ] 全站无 1px 组件边框、无模糊阴影、无渐变背景（转盘 `conic-gradient` 除外）
- [ ] 所有交互元素有 `:focus-visible` 可见焦点环
- [ ] hover 抬起 / active 按下位移一致，无元素"跳位"
- [ ] 视口 `360 / 768 / 1024 / 1440` 均无横向滚动条
- [ ] 硬刷新首屏（SSR）与导航后交互态（WASM）样式一致
- [ ] `prefers-reduced-motion: reduce` 下无位移/旋转动画
- [ ] 登录页、`/identity/users`、租户管理、设置管理页与主风格不冲突
- [ ] 控制台无 CSS 404、无 JS 报错

### 9.3 功能回归清单（对齐 `docs/需求文档.md` §9）

| #  | 验收项                                 | 关联批次     |
| -- | ----------------------------------- | -------- |
| 1  | 店铺新增/编辑/删除/停用，刷新后数据正确               | B4       |
| 2  | 必填校验与超长提示正确                         | B4       |
| 3  | 访客跳登录；无权限访问 `/shops` 被拦截            | B1 / B4  |
| 4  | 转盘扇区 = 启用店铺；停用后刷新该扇区消失              | B3       |
| 5  | 点击开始 → 动画 4~6s → 弹窗结果与指针扇区一致        | B3       |
| 6  | 无启用店铺显示空状态，不白屏                      | B3       |
| 7  | 连点"开始"不触发多次抽取                       | B3       |
| 8  | 附近候选**追加 + 按 PoiId 去重**；清空后回到纯门店库候选 | B4       |
| 9  | 附近搜索翻页正确，总页数/总数准确                   | B4       |
| 10 | 定位 / 设为默认 / 恢复默认 三态正确               | B4       |
| 11 | 批量操作跨页累计，三类计数提示正确                   | B4       |
| 12 | `dotnet build` 0/0；`dotnet test` 全绿 | B0~B6 每批 |

### 9.4 专项回归（本次重构新增）

- [ ] 高德地图在 `ShopMap` / `NearbyShopSearch` 正常渲染；无 Key 时降级显示地址文本
- [ ] 天气组件服务不可用时静默隐藏，不阻断转盘（`FR-M-05`）
- [ ] 记录页"查看全部"权限开关行为不变（`FR-R-01`）
- [ ] 移动端弹窗为底部抽屉（`≤768px`）
- [ ] 侧栏抽屉在移动端可开合，`Esc` 可关，焦点可回归

---

## 10. 待确认事项

以下事项需在开工前确认，否则会影响 B0/B1 批次：

1. **西文标题字体**：是否接受自托管 `Archivo Black`（约 30 KB，放 `wwwroot/fonts/`）？还是全部使用系统字体栈？（默认：自托管，走本地不依赖外网）
2. **`AGENTS.md` 指针**：是否允许在根 `AGENTS.md` 增加一行指向 `src/LunchFate.Blazor/AGENT.md` 的引用？（本次未改动 `AGENTS.md`）
3. **PWA 图标与 favicon**：是否本轮一并重做（`icon-192.png` / `icon-512.png` / `favicon.ico`）？
4. **登录页覆盖深度**：登录页是 MVC 视图，`overrides/framework.css` 覆盖到"底色 + 按钮 + 输入框 + 卡片"即止，是否可接受？
5. **组件 Demo 页**：B2 批次是否保留一个 `/ui-kit` 临时页面用于组件走查（后续可删）？

---

## 11. 说明

- 本方案已于 2026-10-13 **按 B0 → B6 全部实施完成**，实施结果、与初稿的偏差、实测结论与残留风险见 §12。
- 按仓库规范（`AGENTS.md`「协作流程」），每批次遵循"目标 → 涉及文件 → 影响范围 → 风险 → 验证计划"的提交方式推进。
- 规范与现有代码冲突时，以**现有代码行为**为准，并先确认后再调整。
- **目录结构与组件规范的权威版本已迁移到 `src/LunchFate.Blazor/AGENT.md`**；本文 §7 保留初稿设想，实际结构以 `AGENT.md` 为准。

---

## 12. 实施结果（2026-10-13 追加）

方案已按 B0 → B6 全部实施完成，本节记录**实际落地情况、与初稿的偏差、实测结论与残留风险**。
前端开发规范已固化为 `src/LunchFate.Blazor/AGENT.md`。

### 12.1 与初稿的偏差

| # | 初稿设想 | 实际做法 | 原因 |
| --- | --- | --- | --- |
| 1 | 自建 `UserMenu.razor` | **复用 ABP `NavToolbar`** 放进 TopBar | 零风险保留当前用户、我的账户、登出、语言切换；自建需重新实现登出协议 |
| 2 | 为 `MainLayout` / `TopBar` / `SideNav` 各建 scoped CSS | **不建 scoped CSS**，布局样式全部集中在 `layout.css` | 少 3 个文件；彻底避免 scoped CSS 加载顺序不可控的问题 |
| 3 | 抽 `RecordFilterBar.razor` | **未抽**，`RecordList` 就地重写样式 | 筛选表单与页面状态强耦合，抽取需引入多处双向绑定，风险大于收益，且视觉收益为零 |
| 4 | `overrides/blazorise.css` 只覆盖 Blazorise | **扩展为覆盖全部 Bootstrap 5 类**（`.btn`/`.card`/`.table`/`.form-*`/`.badge`/`.alert`/`.modal`/`.pagination`/`.accordion`/`.nav-tabs` …） | 这是让 ABP 内置页（Identity / 租户 / 设置）与登录页真正统一的关键；否则框架页仍是 Bootstrap 观感 |
| 5 | 自托管西文标题字体 `Archivo Black` | **未引入字体文件**，`--nb-font-display` 暂等于正文栈 | 避免新增二进制资产与字体许可问题；标题靠 `font-weight: 900` + `letter-spacing: -0.02em` 达成观感。将来只需改 `tokens.css` 一处 |
| 6 | 目录清单未含辅助文件 | 新增 `Layout/MenuLink.cs`、`Layout/SideNavItem.razor`、`Components/NearbyPoiItem.razor`、`Components/Ui/NbTypes.cs`、`Components/Ui/NbCss.cs` | 抽公共逻辑、递归菜单项、枚举与类名工具，避免重复实现 |
| 7 | 未提及本地化键 | 新增 5 个键：`Close` / `Loading` / `Breadcrumb` / `Menu:Main` / `Menu:ToggleNav`（`zh-Hans.json` 与 `en.json` 同步） | 新外壳需要无障碍标签与加载文案，禁止硬编码 |

### 12.2 实施过程中发现并修复的两个缺陷

| # | 现象 | 根因 | 修复 |
| --- | --- | --- | --- |
| 1 | 侧栏菜单高亮全部失效、面包屑退化为只显示首页 | ABP 的 `IMenuManager` 会把部分菜单地址解析为**绝对地址**（`https://host/shops`），初版 `MenuLink.Normalize` 只处理相对路径，比较必然不等 | `Normalize` 先剥掉协议与主机再比较；`Resolve` 对同源绝对地址回退为站内相对地址 |
| 2 | 传入 `SideNavItem` 的 `CurrentPath` 收到字面量 `"CurrentPath"` | Razor 把 `CurrentPath="CurrentPath"`（**string 类型参数 + 裸标识符**）当作字面量字符串，而非表达式；组件不报错、功能静默失效 | 改为显式 `CurrentPath="@CurrentPath"`；已写入 `AGENT.md` 的代码约定与已知坑清单 |

### 12.3 运行时验证结论（实测，非推断）

| 验证项 | 结果 |
| --- | --- |
| `dotnet build LunchFate.slnx` | **0 警告 / 0 错误** |
| `dotnet test LunchFate.slnx --no-build` | **98 通过 / 0 失败** |
| WASM 合并包 `/global.css` 内部顺序 | bootstrap → blazorise → ABP 主题 → **本项目 tokens → base → layout → components → pages → 覆盖层**（覆盖层在最后，886 KB） |
| MVC 登录页 `<link>` 顺序 | 框架样式 1–14 → **本项目样式 15–18**（最后） |
| Blazor SSR 首屏 `<link>` 顺序 | 框架样式 1–10 → **本项目 7 个文件 11–17** → scoped CSS |
| 自建外壳功能能力 | `nb-shell` / `nb-topbar` / `nb-sidenav` / `nb-content` / `nb-footer` / `nb-breadcrumb` 全部渲染；顶栏含 **`admin` / 我的账户 / 注销 / 语言切换**（登出未丢失） |
| 菜单高亮与面包屑 | `/` → 首页｜`/shops` → 店铺管理（管理组自动展开）｜`/records/draws` → 抽取记录｜`/permissions` → 权限管理；面包屑分别为 首页｜管理 / 店铺管理｜抽取记录｜管理 / 权限管理 |
| 页面渲染 | `/shops`（DataGrid + 批量工具栏）、`/records/draws`（表格 + 分页）、`/permissions` 均正常，无异常提示 |

### 12.4 后续补充完成项

| 项 | 做法 | 验证 |
| --- | --- | --- |
| 根 `AGENTS.md` 指针 | 在「Layout / entry points」章节首行加入指向 `src/LunchFate.Blazor/AGENT.md` 的说明 | — |
| PWA 图标与 favicon 重做 | 新增可复现生成器 `etc/scripts/generate-icons.py`（Pillow），产出 `favicon.svg` / `favicon.ico`（16/32/48）/ `icon-192.png` / `icon-512.png`，并额外放置 `wwwroot/themes/basic/favicon.svg` 覆盖 ABP MVC 主题登录页图标 | 全部端点 **200** 且 content-type 正确；`/themes/basic/favicon.svg` 返回**本项目图标**，确认应用 wwwroot 优先于 ABP 嵌入资源；16/32/48/64px 逐档目视可辨识 |
| `App.razor` 图标接线 | `favicon.svg`（首选）+ `favicon.ico`（兜底）+ `apple-touch-icon` + `manifest` | 实测页头渲染出全部 4 个 `<link>` |
| `manifest.json` | 补 `description`，两个图标加 `purpose: "any maskable"` | 端点 200，`application/json` |

**图标设计**：明黄 `#FFD23F` 满幅底 + 四象限转盘（青 / 粉 / 绿 / 橙，对应 `--nb-accent-cyan` / `--nb-accent-pink` / `--nb-success` / `--nb-accent-orange`）+ 纯黑外圈 + 零模糊硬阴影 + 米白轴心。
比例全部参数化（转盘半径 0.36、外圈 0.055、轴心 0.10、阴影偏移 0.035），改设计只需改脚本常量并同步 `tokens.css` 色值。
转盘含描边的最大半径约 38.8% < 40%，满足 maskable 安全区。

### 12.5 尚未覆盖的验证项（残留风险）

1. **浏览器视觉走查未做**：本环境无法启动浏览器，`hover` 抬起 / `active` 按下位移、焦点环、`360 / 768 / 1024 / 1440` 四档视口、`prefers-reduced-motion` 均**未经人工目视确认**，建议按 §9.2 清单走查一遍。
2. **交互态样式一致性**：`/global.css` 的接管发生在页面加载约 3 秒后（ABP `AbpStyles` 行为），已确认其内部顺序正确，但**未在真实浏览器中对比"首屏"与"接管后"的观感差异**。
3. **ABP 内置页覆盖深度**：`/identity/users`、租户管理、设置管理页仅确认了能渲染与样式表加载，**未逐页目视检查**。
4. **移动端抽屉**：`< 992px` 的抽屉开合、遮罩点击关闭、`Esc` 关闭、焦点回归**未在浏览器中实测**（代码已实现）。

### 12.6 上线后用户反馈修复（4 项）

首次人工走查发现 4 个问题，均已定位到根因并修复；全部在运行时验证过。

| # | 反馈 | 根因 | 修复 | 验证方式 |
| --- | --- | --- | --- | --- |
| 1 | 右上角语言选择器显示异常，点击后按钮位置改变 | ① ABP 主题 `theme.css` 用 `.navbar-nav > .nav-item > .nav-link { color: #fff !important }` 把顶栏文字刷白，白底胶囊上完全不可见；② 实测语言选择器的标记是 `.nav-item > a + .dropdown-menu`（**没有 `.dropdown` 包裹**），菜单找不到定位父级，展开时相对顶栏定位，看起来像按钮被挤动 | ① `overrides/framework.css` 加同特异性 + `!important` 的黑字覆盖（限定 `.nb-topbar` 内）；② 给 `.nb-topbar .navbar-nav > .nav-item` 也加 `position: relative`，只做最小干预、不覆盖 Bootstrap/Blazorise 的定位规则 | 实测 SSR HTML 顶栏文本完整（`admin` / 我的账户 / 注销 / 语言）；覆盖规则确认下发 |
| 2 | 左侧导航下方像被切掉 | 侧栏用 `align-self: start` + `max-height`，高度等于**内容高度**而非整条视口高度 | 改为固定高度 `calc(100vh - var(--nb-topbar-h) - var(--nb-footer-h))`；页脚高度固定为 `--nb-footer-h` 使算式精确。短页面不顶出页脚，长页面靠 sticky 常驻 | 规则确认下发；算式闭合（顶栏 + 内容区 + 页脚 = 100vh） |
| 3 | 右上角用户按钮看不见 | 与 #1 同一根因（白字），用户菜单触发器实测是 `<button class="btn dropdown-toggle">` | 同上（覆盖规则同时命中 `a` / `button` / `.nav-link` / `.dropdown-toggle`） | 同上 |
| 4 | 转盘没有动效；扇区文字超出转盘 | ① **`Transition="_transition"` 缺少 `@`**，Razor 传字面量字符串，inline style 变成非法的 `transition:_transition`，过渡整条被浏览器丢弃；② 标签 `top: 12%` + `max-width: 34%` 超出几何上限（轴心半径 17% + 轮缘留白 3% → 可用径向长度仅约 30%），旋转后外端越过轮缘 | ① 改为 `Transition="@_transition"`；② 改为 `top: 15%` + `max-width: 28%` + `line-height: 1.2`，字号上限 13→12px，并给 `.nb-wheel` 加 `overflow: hidden` 兜底 | 实测 inline style 已是 `transition:transform 5s cubic-bezier(0.15, 0.6, 0.2, 1)`；20 个扇区标签正常渲染 |

**沉淀**：#1/#3 的 ABP 白字覆盖、#1 的顶栏下拉双标记形态、#4 的标签几何约束与 `Transition` 缺 `@`，
已全部写入 `src/LunchFate.Blazor/AGENT.md` 的已知坑清单（第 17、19、20、21、22 条）与转盘章节。
