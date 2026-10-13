# AGENT.md — 前端开发引导规范（LunchFate.Blazor / LunchFate.Blazor.Client）

> 本文件是**前端子系统的开发规范与引导文档**，适用于 `src/LunchFate.Blazor/` 与 `src/LunchFate.Blazor.Client/` 两个项目下的全部 UI 代码。
> 仓库级规范见根目录 `AGENTS.md`（分层、命名、DDD、异常/授权、协作流程）。两者冲突时，**先看本文档的样式与组件约定，其余以根 `AGENTS.md` 为准**。
> 视觉基线与完整令牌表见 `docs/前端重构方案-Neubrutalism.md`。

---

## 1. 权威顺序（遇到分歧时按此顺序裁决）

1. **运行中的代码行为** —— 现有实现即事实，不要凭规范文字推翻已验证的行为。
2. **根 `AGENTS.md`** —— 分层、命名、注释、异常、授权、协作流程等硬性约定。
3. **本文档** —— 前端目录结构、样式体系、组件规范、开发流程。
4. **`docs/前端重构方案-Neubrutalism.md`** —— 视觉令牌全表、布局设计、批次计划。
5. **`docs/需求文档.md`** —— 功能需求编号（`FR-*`），代码注释必须引用。

**风格定位**：新粗野主义（Neubrutalism）。三个不可妥协的特征：**粗黑描边（3px 起）**、**零模糊硬阴影**、**高饱和纯色块**。

---

## 2. 技术栈与不可变约束

| 项 | 现状 | 约定 |
| --- | --- | --- |
| 框架 | ABP 10.6.1 + Blazor Web App（`InteractiveAuto`，Server + WASM 双端） | 任何 UI 改动必须同时兼容 SSR 首屏与 WASM 交互态 |
| UI 组件库 | Blazorise（Bootstrap 5 provider） | **保留**，只做 CSS 覆盖层，不替换、不重写其标记 |
| 主题 | ABP BasicTheme（`Volo.Abp.AspNetCore.Components.Web.BasicTheme`，NuGet 内编译组件） | 外壳已由 `Layout/MainLayout.razor` 自建替换；ABP 内置页不可改标记 |
| 样式技术 | 原生 CSS + CSS 自定义属性 | **禁止**引入 SASS / Less / Tailwind / PostCSS |
| 前端构建 | 无（`abp install-libs` 只装第三方 libs） | **禁止**新增 npm 构建步骤；`dotnet build` 是唯一构建入口 |
| JS | `wwwroot/global-scripts.js` 为 **ES 模块**，经 `IJSRuntime.import` 按需加载 | **禁止**把它加回 MVC 经典脚本包（`export` 会语法报错） |
| 字体/图标 | 系统字体栈 + FontAwesome（本地 `wwwroot/libs`） | **禁止**引用外部 CDN（部署环境可能无外网） |
| 本地化 | `zh-Hans.json` / `en.json` 同步维护 | **禁止**硬编码用户可见文案 |

---

## 3. 目录结构与文件放置

```
src/LunchFate.Blazor/                 ← 宿主项目（SSR、MVC、bundle 注册、品牌）
├── AGENT.md                          ← 本文件
├── Components/App.razor              ← HTML 外壳、bundle 挂载点、theme-color
├── LunchFateBlazorModule.cs          ← 服务注册 + ConfigureBundles（MVC / Blazor SSR 两处）
├── LunchFateStyleBundleContributor.cs← 样式文件清单与顺序（WASM 侧 + 供上面两处复用）
├── LunchFateComponentBase.cs         ← 服务端组件基类
└── wwwroot/
    ├── favicon.svg                   ← 应用图标（矢量，App.razor 首选引用）
    ├── favicon.ico                   ← 应用图标（16/32/48 兜底）
    ├── themes/basic/favicon.svg      ← 覆盖 ABP MVC 主题登录页引用的图标
    └── global-scripts.js             ← 高德地图 ES 模块（勿动契约）

src/LunchFate.Blazor.Client/          ← 客户端项目（页面、组件、外壳、样式）
├── Layout/                           ← 应用外壳（无 scoped CSS，样式全部在 layout.css）
│   ├── MainLayout.razor              ← 外壳骨架 + ABP 功能组件挂载点
│   ├── TopBar.razor                  ← 品牌 + 汉堡 + 复用 ABP NavToolbar
│   ├── SideNav.razor                 ← IMenuManager 驱动的侧边栏（移动端抽屉 + Esc 关闭）
│   ├── SideNavItem.razor             ← 菜单项，递归渲染支持任意层级
│   ├── Breadcrumbs.razor             ← 自建面包屑（路由 → 菜单路径推导）
│   ├── AppFooter.razor
│   └── MenuLink.cs                   ← 菜单地址归一化 / 绝对化（三个组件共用）
├── Components/
│   ├── Ui/                           ← 自有 UI 组件（Nb*）
│   │   ├── NbTypes.cs                ← NbColor / NbSize 枚举
│   │   ├── NbCss.cs                  ← 类名拼装工具
│   │   ├── NbButton.razor  NbCard.razor  NbBadge.razor  NbAlert.razor
│   │   └── NbEmptyState.razor  NbSpinner.razor  NbPageHeader.razor  NbToolbar.razor
│   ├── Wheel/
│   │   ├── LunchWheel.razor          ← 转盘本体（几何计算在此，禁止改动）
│   │   └── WheelResultCard.razor     ← 结果卡片（可导航 / 降级两态）
│   ├── NearbyShopSearch.razor        ← 附近搜索
│   ├── NearbyPoiItem.razor           ← 搜索结果行
│   ├── RecordList.razor              ← 两类记录共用
│   ├── ShopMap.razor                 ← 地图容器（常驻渲染树）
│   └── WeatherWidget.razor
├── Pages/                            ← 路由页面（Home / Shops / DrawRecords / ShopCreationRecords / Permissions）
├── Navigation/                       ← 菜单贡献者（数据层，不含样式）
├── Routes.razor                      ← 路由 + DefaultLayout 指定（用别名 NbLayout 消除歧义）
├── LunchFateComponentBase.cs         ← 客户端组件基类
└── wwwroot/
    ├── styles/                       ← **全部样式，唯一真源**
    │   ├── tokens.css                ← 设计令牌（只允许 :root 变量声明）
    │   ├── base.css                  ← reset / 排版 / 链接 / 滚动条 / error UI / 启动加载态
    │   ├── layout.css                ← 外壳布局（顶栏 / 侧栏 / 面包屑 / 内容区 / 页脚）
    │   ├── components.css            ← .nb-* 组件类 + .nb-filter-grid 等通用容器
    │   ├── pages.css                 ← 页面专属（转盘 / 天气 / 店铺 / 记录 / 地图 / 权限）
    │   └── overrides/
    │       ├── blazorise.css         ← Blazorise + Bootstrap 5 全量覆盖
    │       └── framework.css         ← ABP MVC 外壳 + 登录页 + 框架内置页
    ├── icon-192.png                  ← PWA 图标（manifest.json 引用）
    ├── icon-512.png                  ← PWA 图标（manifest.json 引用）
    └── manifest.json
```

### 图标资产

图标**由脚本生成，不要手工编辑二进制**：

```bash
python etc/scripts/generate-icons.py    # 需要 Pillow；仅本机生成时用，应用运行时不依赖
```

脚本参数化了绘制比例（转盘半径 0.36、外圈描边 0.055、轴心 0.10、硬阴影偏移 0.035），
改动设计请改脚本里的常量与 `SVG_TEMPLATE`，并同步 `tokens.css` 中的色值。
产物与引用位置：

| 文件 | 引用方 |
| --- | --- |
| `src/LunchFate.Blazor/wwwroot/favicon.svg` / `.ico` | `App.razor`（Blazor 页头） |
| `src/LunchFate.Blazor/wwwroot/themes/basic/favicon.svg` | ABP MVC 主题登录页（应用 wwwroot 优先于 ABP 嵌入资源，实测覆盖生效） |
| `src/LunchFate.Blazor.Client/wwwroot/icon-192.png` / `icon-512.png` | `manifest.json`（`purpose: any maskable`） |

设计已满足 maskable 安全区：转盘含描边的最大半径约 38.8% < 40%。

> 已退役：`src/LunchFate.Blazor/wwwroot/global-styles.css`、`src/LunchFate.Blazor.Client/wwwroot/main.css`
> 与 `src/LunchFate.Blazor.Client/Pages/Home.razor.css`（内容已分别并入 `base.css` 与 `pages.css`）。

**放置规则**

| 要新增的东西 | 放哪里 |
| --- | --- |
| 新的路由页面 | `Pages/Xxx.razor`（`@page` + `@attribute [Authorize(...)]` + `@inherits LunchFateComponentBase`） |
| 可复用的纯展示组件 | `Components/Ui/NbXxx.razor`（`Nb` 前缀 = 本项目自有设计系统） |
| 业务组件（含服务调用） | `Components/Xxx.razor` |
| 应用外壳相关 | `Layout/` |
| 颜色/间距/阴影等视觉值 | **只能**写进 `styles/tokens.css` |
| 某个页面独有的样式 | `styles/pages.css`（**不是** `Xxx.razor.css`） |
| 组件视觉样式 | `styles/components.css`（**不是** `Xxx.razor.css`） |
| 组件结构样式（尺寸/定位/栅格） | 可以放 `Xxx.razor.css` |

---

## 4. 样式体系

### 4.1 加载链路（改动样式前必须理解）

```
MVC 侧（登录页 / 错误页）
  BasicThemeBundles.Styles.Global
    └─ tokens.css → base.css → overrides/blazorise.css → overrides/framework.css

Blazor SSR 侧（首屏）
  BlazorBasicThemeBundles.Styles.Global
    └─ tokens.css → base.css → layout.css → components.css → pages.css
       → overrides/blazorise.css → overrides/framework.css

Blazor WASM 侧（交互态）
  BlazorWebAssemblyStandardBundles.Styles.Global（经 LunchFateStyleBundleContributor）
    └─ 与 Blazor SSR 侧完全相同的 7 个文件、相同顺序

说明：App.razor 里 WebAssemblyStyleFiles="GlobalStyles"（值为 "global.css"）
指的是 WASM 样式包的**包名**，由 ABP 打包后以 /global.css 提供，不是磁盘上的文件。
因此新增样式只需改 LunchFateStyleBundleContributor 与 ConfigureBundles，不要动 App.razor。
```

**两条硬规则**

1. **同一份样式必须同时被 SSR 与 WASM 加载**。只加一条链路会出现"首次访问没样式、刷新后才有"——这正是高德 Key 踩过的坑（见根 `AGENTS.md`）。
2. **文件在 bundle 中的顺序 = 层叠顺序**。新增样式文件时，必须同步更新 `LunchFateStyleBundleContributor`（WASM）与 `LunchFateBlazorModule.ConfigureBundles`（MVC / SSR），并放在正确的层。**不要用 `@import`**（会产生串行额外请求）。

### 4.2 层叠与特异性策略

> ⚠️ **不要用 `@layer` 包自己的样式。** Bootstrap / Blazorise / ABP `theme.css` 都是**未分层**样式，未分层样式优先级高于任何分层样式——把 `.nb-btn` 放进 `@layer` 会让它彻底输给 `.btn`。

按以下顺序解决覆盖问题：

1. **靠文件顺序**：自己的样式排在框架样式之后（实测 SSR 侧确实如此：框架 CSS 先加载，项目样式在后）。
2. **靠双类选择器**：`.btn.btn`、`.badge.badge`、`.btn.btn-primary` —— 特异性 0-2-0，比框架的单类规则高一级。
   `overrides/blazorise.css` 已全面采用这一写法，因此**即使 WASM 合并包 /global.css 的内部顺序与预期不同，覆盖依然生效**。
3. **`!important` 是最后手段**：仅在覆盖 Blazorise 生成的行内样式或框架自带 `!important` 时使用，**必须同行注释说明原因**：

   ```css
   /* !important：Blazorise Modal 用行内 style 设置 padding，必须强制覆盖 */
   .nb-modal .modal-body { padding: var(--nb-space-5) !important; }
   ```

4. **禁止**整文件 `!important`，禁止"图省事"加 `!important`。

**元素级规则要加 `html` 前缀**：`base.css` 里的 `body` / `h1..h6` / `a` / `hr` 写成 `html body`、`html h1` 等（特异性 0-0-2），
以便在 WASM 合并包里无论与 Bootstrap 谁先谁后都能生效。新增元素级规则请沿用这个写法。

### 4.3 scoped CSS（`Xxx.razor.css`）使用边界

`*.razor.css` 编译进 `LunchFate.Blazor.Client.styles.css`，加载顺序在 bundle 之外、不可控。因此：

- ✅ 允许写：`width` / `height` / `position` / `inset` / `display` / `grid-*` / `flex-*` / `transform` / `z-index` / 媒体查询
- ❌ 禁止写：`color` / `background` / `border` / `border-radius` / `box-shadow` / `font-*`

**理由**：避免"改了令牌但组件不跟着变"，以及 scoped 与全局样式互相打架时无法定位。

### 4.4 命名约定

| 类型 | 约定 | 示例 |
| --- | --- | --- |
| 自有组件类 | `nb-` 前缀 + kebab-case | `.nb-card`、`.nb-btn`、`.nb-empty` |
| 组件元素 | BEM-lite：`__` 连接 | `.nb-card__header`、`.nb-card__body` |
| 变体 / 状态 | `--` 连接 | `.nb-btn--primary`、`.nb-card--flat`、`.nb-table--compact` |
| 布局块 | `nb-` + 区域名 | `.nb-topbar`、`.nb-sidenav`、`.nb-breadcrumb` |
| 转盘 | `nb-wheel-` 前缀 | `.nb-wheel`、`.nb-wheel__label`、`.nb-wheel__hub` |
| 工具类 | 尽量少；仅 `nb-u-` 前缀 | `.nb-u-tabular`（数字等宽） |

**禁止**用 `col-*`/`row`/`btn`/`card` 等 Bootstrap 类名去定义自己的组件（会与框架冲突）。需要 Bootstrap 栅格时直接用，不要重定义。

---

## 5. 设计令牌速查

> 完整说明见 `docs/前端重构方案-Neubrutalism.md` §4。**所有视觉值只能引用令牌，禁止写字面量。**

```css
:root {
  /* ── 色彩 ── */
  --nb-ink: #000;            --nb-ink-soft: #1a1a1a;   --nb-ink-mute: #4a4a4a;
  --nb-paper: #fffdf5;       --nb-surface: #fff;        --nb-surface-alt: #fff8e1;
  --nb-rule: rgba(0,0,0,.35);/* 仅表格/列表内部分隔线 */

  --nb-primary: #ffd23f;     --nb-primary-hover: #ffe066; --nb-primary-active: #f5c400;
  --nb-success: #3ddc97;     --nb-warning: #ffb703;
  --nb-danger: #ff4d4d;      --nb-info: #4cc9f0;

  --nb-accent-cyan: #4cc9f0; --nb-accent-pink: #ff6b9d; --nb-accent-orange: #ff8a3d;
  --nb-accent-purple: #b388ff; --nb-accent-lime: #c4f000;

  /* ── 字体 ── */
  --nb-font-sans: system-ui, -apple-system, "Segoe UI", "PingFang SC",
                  "Microsoft YaHei", "Noto Sans SC", sans-serif;
  --nb-font-display: "Archivo Black", var(--nb-font-sans);
  --nb-font-mono: ui-monospace, "JetBrains Mono", Consolas, monospace;

  --nb-fs-xs: 12px; --nb-fs-sm: 14px; --nb-fs-base: 16px; --nb-fs-lg: 18px;
  --nb-fs-xl: 22px; --nb-fs-2xl: 28px; --nb-fs-3xl: 36px; --nb-fs-4xl: 48px;
  --nb-lh-tight: 1.15; --nb-lh-base: 1.6; --nb-tracking-tight: -.02em;
  --nb-weight-body: 500; --nb-weight-bold: 700; --nb-weight-black: 900;

  /* ── 边框 / 圆角 ── */
  --nb-border-w-sm: 2px; --nb-border-w: 3px; --nb-border-w-lg: 4px;
  --nb-border-color: var(--nb-ink);
  --nb-radius-sm: 2px; --nb-radius: 4px;
  --nb-radius-pill: 999px; --nb-radius-circle: 50%;

  /* ── 硬阴影（模糊半径恒为 0） ── */
  --nb-shadow-xs: 2px 2px 0 var(--nb-ink);
  --nb-shadow-sm: 3px 3px 0 var(--nb-ink);
  --nb-shadow:    4px 4px 0 var(--nb-ink);
  --nb-shadow-lg: 6px 6px 0 var(--nb-ink);
  --nb-shadow-xl: 8px 8px 0 var(--nb-ink);

  /* ── 间距（4px 基准） ── */
  --nb-space-1: 4px;  --nb-space-2: 8px;  --nb-space-3: 12px; --nb-space-4: 16px;
  --nb-space-5: 24px; --nb-space-6: 32px; --nb-space-7: 48px; --nb-space-8: 64px;

  /* ── 动效 ── */
  --nb-dur-fast: 120ms; --nb-dur-base: 180ms; --nb-dur-slow: 320ms;
  --nb-ease: cubic-bezier(.2,.8,.2,1);

  /* ── 布局 ── */
  --nb-topbar-h: 64px; --nb-sidenav-w: 248px;
  --nb-content-max: 1200px; --nb-page-pad: clamp(16px, 4vw, 32px);
}
```

**统一交互位移**（所有可点击元素一致）：

```css
/* 静息 */                      box-shadow: var(--nb-shadow);
/* hover  抬起 */               transform: translate(-2px, -2px); box-shadow: var(--nb-shadow-lg);
/* active 按下 */               transform: translate(2px, 2px);   box-shadow: none;
/* focus-visible */             outline: 3px solid var(--nb-ink); outline-offset: 2px;
```

---

## 6. 组件规范

### 6.1 自有组件（`Components/Ui/Nb*.razor`）

```razor
@* NbBadge.razor —— 状态徽章（.nb-badge） *@
@inherits LunchFateComponentBase

<span class="@CssClass">@ChildContent</span>

@code {
    /// <summary>徽章语义色；决定背景色，禁止跨语义借用。</summary>
    [Parameter] public NbColor Color { get; set; } = NbColor.Neutral;

    /// <summary>徽章内容。</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>附加 CSS 类，供调用方微调间距等。</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>拼装最终类名：基础类 + 变体类 + 调用方附加类。</summary>
    private string CssClass => string.Join(' ',
        "nb-badge", $"nb-badge--{Color.ToString().ToLowerInvariant()}", Class).TrimEnd();
}
```

**硬性要求**

1. **继承 `LunchFateComponentBase`**（拿到 `L` 本地化与 `ToUserMessage`）。
2. **每个 `[Parameter]` 与每个 `private` / `protected override` 方法必须有 XML `<summary>`**（沿用根 `AGENTS.md` 的注释规则）。
3. **类名拼装集中在一个表达式属性里**（如上面的 `CssClass`），不要在标记里散落三元表达式。
4. **组件不得直接读 `IConfiguration` 或发 HTTP 请求**（那是业务组件的事）。
5. **业务组件（`Components/*.razor`）调用服务后必须 try/catch，并用 `ToUserMessage(ex)` 转成用户提示**，不要 `throw ex`。
6. **状态语义固定**：`Success` = 启用/成功，`Warning` = 停用/注意，`Danger` = 删除/错误。不得为了好看跨语义借用颜色。
7. **`NbCard` 同时使用 `<Actions>` / `<Footer>` 与主体内容时，主体必须显式包在 `<ChildContent>` 内**，
   否则 Razor 报 `RZ9996: Unrecognized child content`。只用主体（无命名片段）时可以省略 `<ChildContent>`：

   ```razor
   <NbCard Title="@L["Shops"]">
       <Actions>
           <NbButton Color="NbColor.Primary" Clicked="OpenCreateModal">@L["NewShop"]</NbButton>
       </Actions>
       <ChildContent>
           <div class="nb-filter-grid">…</div>
       </ChildContent>
   </NbCard>
   ```

8. **`NbButton` 只能渲染 `<button>`**。需要"看起来像按钮的链接"时，直接在 `<a>` 上用 `.nb-*` 类：
   `<a class="nb-btn nb-btn--neutral" href="…">`。这是唯一允许绕过 Nb 组件的场景。

### 6.2 使用 Blazorise 组件的规则

- 继续使用 `Card` / `Modal` / `DataGrid` / `EditForm` 等，但**外观一律由 `.nb-*` 覆盖层接管**。
- **不得**修改 Blazorise 组件的结构参数（列定义、`ReadData`、`PageSize`、`ShowPager` 等），只调整视觉相关参数。
- 弹窗统一结构：`ModalHeader`（黄底 + 3px 下边框）→ `ModalBody` → `ModalFooter`（右对齐按钮组，主操作在最后）。
- `DataGrid` 覆盖点集中在 `overrides/blazorise.css`，不要散落到页面样式里。

### 6.3 转盘组件（`Components/Wheel/`）—— 高危区域

**禁止改动**以下内容（改错会导致"指针指向的扇区 ≠ 弹窗结果"）：

- `WheelBackground` 的 `conic-gradient` 角度计算
- `SectorAngle(index)` 的中心角计算
- `_rotation` / `_turns` 的旋转数学
- `SpinSeconds = 5` 与 `_transition` 的缓动曲线（`FR-W-02`）
- 候选顺序：**门店库启用门店在前、附近门店在后**（必须与服务端 `ShopIds` + `TemporaryShops` 拼接顺序一致，否则 `WinnerIndex` 对不上扇区）
- 附近候选的**追加 + 按 `PoiId` 去重**语义（不是整体替换）

**允许改动**：扇区配色（取自 §4.1 色环）、扇区文字颜色（统一纯黑，去掉 `text-shadow`）、外圈边框、指针形状与硬阴影、中心按钮样式、标签字号与 `title` 提示。

**扇区标签的几何约束（改字号/长度前必读）**：标签旋转 90° 后沿半径方向排列，可用径向长度 = 转盘半径 50% − 轴心半径 17% − 轮缘留白 3% ≈ **30%**。
因此 `.nb-wheel__label` 用 `top: 15%` + `max-width: 28%`（均为百分比，各尺寸下一致），并靠 `.nb-wheel` 的 `overflow: hidden` 兜底。
**调大 `max-width` 或上移 `top` 会让文字压到外圈描边甚至出界** —— 这正是重构后首次上线出现的问题。

### 6.4 地图组件（`ShopMap.razor`）—— 高危区域

- 地图容器**必须常驻渲染树**，**禁止**用 `@if` 包裹（高德会往容器内注入 DOM，条件渲染会在重渲染时把地图清掉）。降级文案是容器的**兄弟节点**。
- 容器样式只允许改 `border` / `border-radius` / `overflow`；**禁止**改 `display` / `position`。
- **禁止**把 `global-scripts.js` 加回 MVC 经典脚本包。

---

## 7. 代码约定

### 7.1 Razor / C#

- 每个组件文件顶部加中文注释说明用途与关联需求编号（如 `@* 店铺管理（需求文档 FR-S-01 ~ FR-S-05） *@`）。
- **`string` 类型的组件参数必须写显式 `@`**：`CurrentPath="@CurrentPath"`。写成 `CurrentPath="CurrentPath"` 时 Razor 会把
  裸标识符当成**字面量字符串**，参数收到 `"CurrentPath"`，组件不报错但功能静默失效（`bool` / 枚举 / 对象类型的参数不受影响）。
- 私有字段 `_camelCase`；参数 PascalCase；事件回调用 `EventCallback<T>` / `EventCallback`。
- 异步方法 `Async` 结尾；查询方法 `Get` / `Search` 开头。
- 列表渲染必须带 `@key`（如 `@key="row.Id"`），避免重渲染错位。
- 表单字段必须有 `label`（或 `aria-label`）+ `ValidationMessage`。
- 破坏性操作必须二次确认弹窗。
- **禁止**在 Razor 里写内联 `style="color: #..."`；需要动态值时用 CSS 自定义属性：`style="--nb-sector-color: @color"`。

### 7.2 JavaScript

- `global-scripts.js` 保持 ES 模块形态，只导出函数，**不注册全局变量**。
- 新增 JS 能力时：优先用 `IJSRuntime` 调用已有导出；确需新增则追加 `export function`，并更新调用组件的 `IJSObjectReference` 缓存逻辑。
- **禁止**引入 jQuery 新用法（现有 libs 里的 jQuery 只服务 MVC 侧）。

### 7.3 本地化

- 所有用户可见文案走 `L["Key"]`；新增键必须**同时**补 `zh-Hans.json` 与 `en.json`。
- 页面/组件里的错误提示统一用 `ToUserMessage(ex)`，不要自造文案。

---

## 8. 新增页面 / 组件的标准流程

### 新增一个页面

1. 确认需求编号（`docs/需求文档.md` 的 `FR-*`），写进文件头注释。
2. `Pages/Xxx.razor`：`@page` + `@attribute [Authorize(...)]` + `@inherits LunchFateComponentBase`。
3. 页面结构用现成组件拼装：`NbPageHeader` → `NbCard` → `NbToolbar` → 表格/列表 → `NbAlert` / `NbEmptyState`。
4. 需要的视觉值：**只引用令牌**，不写字面量。
5. 页面专属样式加进 `styles/pages.css`（**不要**新建 `Xxx.razor.css` 写颜色）。
6. 若新增菜单入口，改 `Navigation/LunchFateMenuContributor.cs`（只改数据，不写样式）。
7. 跑 §9 的验证命令。

### 新增一个组件

1. 判断归属：纯展示 → `Components/Ui/NbXxx.razor`；含服务调用 → `Components/Xxx.razor`。
2. 继承 `LunchFateComponentBase`；每个 `[Parameter]` 写 XML 注释。
3. 类名拼装集中到 `CssClass` 表达式属性；变体用 `--` 修饰类。
4. 组件视觉样式加进 `styles/components.css`；结构样式（尺寸/定位）可放 `Xxx.razor.css`。
5. 覆盖 Blazorise 的规则放 `styles/overrides/blazorise.css`。
6. 移动端（360px）与桌面（1440px）各验证一次。

### 提交前 checklist

- [ ] 所有颜色/边框/圆角/阴影/字号/间距都来自令牌
- [ ] 无 1px 组件边框、无模糊阴影、无渐变背景（转盘 `conic-gradient` 除外）
- [ ] hover 抬起 / active 按下行为一致
- [ ] `:focus-visible` 焦点环可见
- [ ] 文案走本地化键，`zh-Hans.json` 与 `en.json` 同步
- [ ] 新增/修改的 `private` 与 `protected override` 方法有 XML `<summary>`
- [ ] 样式文件已在 `LunchFateStyleBundleContributor` 与 `ConfigureBundles` 两处注册（如新增文件）
- [ ] `dotnet build LunchFate.slnx` 0 警告 0 错误

---

## 9. 构建与验证

```bash
# 构建（必须 0 警告 / 0 错误）
dotnet build LunchFate.slnx

# 测试（必须 92 个全绿；前端改动不应影响测试结果）
dotnet test LunchFate.slnx --no-build

# 运行（https://localhost:44352，admin / abc123!）
dotnet run --project src/LunchFate.Blazor

# 首次 clone 后必须先装客户端依赖（wwwroot/libs 是 gitignored）
abp install-libs
```

**视觉/功能自检清单**

- [ ] 硬刷新首屏（SSR）与导航后交互态（WASM）样式一致
- [ ] 视口 `360 / 768 / 1024 / 1440` 无横向滚动条
- [ ] `prefers-reduced-motion: reduce` 下无位移/旋转动画
- [ ] 登录页、`/identity/users`、租户管理、设置管理页与主风格不冲突
- [ ] 控制台无 CSS 404、无 JS 报错
- [ ] 移动端弹窗为底部抽屉（`≤768px`）；侧栏抽屉可开合、`Esc` 可关、焦点可回归
- [ ] 需求文档 §9 的 12 条验收标准逐条通过

---

## 10. 已知坑（前端专项）

| # | 坑 | 表现 | 正确做法 |
| --- | --- | --- | --- |
| 1 | `InteractiveAuto` 双端渲染 | 首屏没样式，刷新后才有 | 样式必须同时挂 SSR 与 WASM 两条 bundle |
| 2 | 样式加载顺序 | `.nb-*` 覆盖不了 `.btn` | 先查 DevTools 的 `<link>` 顺序；顺序不行就用双类选择器 `.nb-btn.btn` |
| 3 | `@layer` 陷阱 | 自己的样式完全失效 | **不要**用 `@layer` 包自己的样式（框架样式未分层，优先级更高） |
| 4 | scoped CSS 写颜色 | 改了令牌组件不变 | `Xxx.razor.css` 只写结构样式 |
| 5 | 转盘几何被误改 | 弹窗结果与指针扇区不一致 | 只改颜色/边框/阴影/字体，数学一行不动 |
| 6 | 候选顺序被重排 | `WinnerIndex` 对不上扇区 | 保持"门店库在前、附近在后"；附近是追加 + 去重 |
| 7 | 地图容器被条件渲染 | 地图渲染一次后消失 | 容器常驻渲染树，降级文案放兄弟节点 |
| 8 | `global-scripts.js` 进经典脚本包 | 全站 JS 语法报错 | 它是 ES 模块，只能 `import` |
| 9 | 高德 Key 只配一份 | 首次访问没地图、刷新后才有 | 服务端 `appsettings.json` + 客户端 `wwwroot/appsettings.json` 都要配 `JsApiKey` |
| 10 | Blazorise 行内样式 | 覆盖层不生效 | 用 `!important` + 同行注释说明原因 |
| 11 | `wwwroot/libs` 是 gitignored | 新环境页面样式全崩 | 先跑 `abp install-libs` |
| 12 | 中文标题过粗/过细 | 观感不一致 | `font-weight: 900` + 系统字体栈；不引入网络中文字体 |
| 13 | `NbCard` 混用命名片段与无名内容 | `RZ9996: Unrecognized child content` | 主体显式包进 `<ChildContent>` |
| 14 | 误以为 `global.css` / `global.js` 是磁盘文件 | 找不到文件、试图新建 | 它们是 ABP 的**样式/脚本包名**，由打包产出；新增样式只改注册代码 |
| 15 | 在 `Xxx.razor.css` 里写颜色 | 改了令牌组件不变，且与全局样式打架 | scoped CSS 只写结构样式（尺寸/定位/栅格） |
| 16 | 用 `href=""` 表示首页菜单链接 | 点击首页跳到当前页 | 用 `MenuLink.Resolve(url, BaseUri)` 生成绝对地址 |
| 17 | **string 类型参数写成裸标识符** | 参数收到字面量字符串，功能静默失效（曾导致侧栏高亮与面包屑全废） | string 参数一律写 `CurrentPath="@CurrentPath"`，不要写 `CurrentPath="CurrentPath"` |
| 18 | ABP 菜单 URL 可能是绝对地址 | 拿 `MenuItem.Url` 直接与当前路由比较必然不等 | 用 `MenuLink.Normalize`（会剥掉协议与主机）后再比较 |
| 19 | **ABP 主题把顶栏文字刷成白色** | 用户菜单与语言选择器在白底胶囊上完全不可见，只剩空胶囊 | ABP 的 `theme.css` 用 `.navbar-nav > .nav-item > .nav-link { color: #fff !important }`；覆盖必须同特异性 + `!important`（见 `overrides/framework.css` 的顶栏段落） |
| 20 | **顶栏下拉有两种标记形态** | 语言选择器的菜单找不到定位父级，展开时像"按钮被挤动" | 用户菜单是 `.nav-item > .dropdown > button + .dropdown-menu`；语言选择器是 `.nav-item > a + .dropdown-menu`（**无 `.dropdown` 包裹**）→ 给 `.nav-item` 也加 `position: relative` |
| 21 | 转盘标签跑出轮缘 | 文字压到外圈描边甚至出界 | 几何约束：轴心半径 17% + 轮缘留白 3% → 可用径向长度上限 30%；用 `top: 15%` + `max-width: 28%`，并靠 `.nb-wheel` 的 `overflow: hidden` 兜底 |
| 22 | 侧栏高度写死 `100vh` | 短页面把页脚顶出屏幕，长页面侧栏被内容截断 | 用 `calc(100vh - var(--nb-topbar-h) - var(--nb-footer-h))`；页脚高度固定为 `--nb-footer-h` 以保证算式精确 |

---

## 11. 协作流程（硬性，摘录自根 `AGENTS.md`）

- 全程中文协作；注释、说明也用中文。
- 只读分析可直接执行；**创建/修改/删除文件前必须先给方案**（目标、涉及文件、影响范围、风险、验证计划），获明确批准后才动手。
- 严格按批准范围做最小改动；不猜需求或业务意图；无法验证时说明残留风险。
- 按批次推进（见 `docs/前端重构方案-Neubrutalism.md` §6.2），每批独立可验收、可回滚，不跨批次混合改动。
