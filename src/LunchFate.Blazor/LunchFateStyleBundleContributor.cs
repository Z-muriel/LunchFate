using Volo.Abp.AspNetCore.Mvc.UI.Bundling;

namespace LunchFate.Blazor;

/// <summary>
/// 把项目样式按固定顺序加入 Blazor WASM 标准样式包。
/// 加入顺序即层叠顺序：令牌 → 基础 → 布局 → 组件 → 页面 → 覆盖层，
/// 覆盖层必须排在最后才能压过 Bootstrap / Blazorise / ABP 主题样式。
/// 新增或改名样式文件时，必须同步更新本文件与
/// <see cref="LunchFateBlazorModule.ConfigureBundles"/> 两处注册。
/// </summary>
public class LunchFateStyleBundleContributor : BundleContributor
{
    /// <summary>
    /// Blazor 侧（含 WASM 交互态）加载的全部样式文件，顺序即层叠顺序。
    /// 路径不带前导斜杠，与原有的 main.css 注册方式保持一致。
    /// </summary>
    public static readonly string[] BlazorStyleFiles =
    [
        "styles/tokens.css",
        "styles/base.css",
        "styles/layout.css",
        "styles/components.css",
        "styles/pages.css",
        "styles/overrides/blazorise.css",
        "styles/overrides/framework.css"
    ];

    /// <summary>
    /// MVC 侧（登录页 / 错误页）加载的样式文件：只需要令牌、基础与覆盖层，
    /// 不含仅 Blazor 使用的布局与页面样式。
    /// </summary>
    public static readonly string[] MvcStyleFiles =
    [
        "/styles/tokens.css",
        "/styles/base.css",
        "/styles/overrides/blazorise.css",
        "/styles/overrides/framework.css"
    ];

    /// <summary>
    /// 按 <see cref="BlazorStyleFiles"/> 的顺序把样式文件加入当前样式包。
    /// </summary>
    /// <param name="context">样式包配置上下文</param>
    public override void ConfigureBundle(BundleConfigurationContext context)
    {
        foreach (var file in BlazorStyleFiles)
        {
            context.Files.Add(new BundleFile(file, true));
        }
    }
}
