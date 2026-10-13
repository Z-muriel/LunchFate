using System;

namespace LunchFate.Blazor.Client.Layout;

/// <summary>
/// 菜单地址解析工具：把 ABP 菜单项地址统一归一化为可比较的相对路径与可点击的地址。
/// 供 SideNav / SideNavItem / Breadcrumbs 共用，避免多处实现产生分歧。
///
/// 注意：ABP 的 IMenuManager 会把菜单地址解析为**绝对地址**
/// （例如 "/shops" 变成 "https://host/shops"），因此归一化必须先剥掉协议与主机，
/// 否则激活态判断与面包屑匹配会全部失效。
/// </summary>
internal static class MenuLink
{
    /// <summary>
    /// 把菜单地址归一化为可比较的相对路径：
    /// 先剥掉协议与主机，再去掉查询串、锚点与首尾斜杠，最后转小写。
    /// </summary>
    /// <param name="url">菜单项地址，形如 "/shops"、"~/Identity/Users" 或 "https://host/shops"</param>
    /// <returns>归一化后的相对路径；地址为空时返回空串</returns>
    public static string Normalize(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        var value = StripOrigin(url.Trim());

        var cut = value.IndexOfAny(['?', '#']);
        if (cut >= 0)
        {
            value = value[..cut];
        }

        return value.TrimStart('~', '/').TrimEnd('/').ToLowerInvariant();
    }

    /// <summary>
    /// 把菜单地址解析为可直接用于 href 的地址。
    /// - 同源绝对地址会转回站内相对地址，避免 App:SelfUrl 配置与当前访问地址不一致时跳出站点；
    /// - 外部地址（如授权服务器的"我的账号"）原样返回；
    /// - 相对地址按应用基址补全，避免出现 href="" 导致首页链接跳到当前页。
    /// </summary>
    /// <param name="url">菜单项地址</param>
    /// <param name="baseUri">应用基址，取自 NavigationManager.BaseUri</param>
    /// <returns>可点击的地址；地址为空时返回 "#"</returns>
    public static string Resolve(string? url, string baseUri)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return "#";
        }

        var value = url.Trim();

        if (IsHttpUrl(value))
        {
            var baseUriObject = new Uri(baseUri);
            if (Uri.TryCreate(value, UriKind.Absolute, out var absolute) &&
                string.Equals(absolute.Host, baseUriObject.Host, StringComparison.OrdinalIgnoreCase) &&
                absolute.Port == baseUriObject.Port &&
                string.Equals(absolute.Scheme, baseUriObject.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                return new Uri(baseUriObject, StripOrigin(value).TrimStart('/')).ToString();
            }

            // 非同源：视为外部地址（例如授权服务器的 Account/Manage）
            return value;
        }

        // 协议相对地址（//host/path）无法判断同源，按外部地址处理
        if (value.StartsWith("//", StringComparison.Ordinal))
        {
            return value;
        }

        return new Uri(new Uri(baseUri), value.TrimStart('~', '/')).ToString();
    }

    /// <summary>
    /// 判断地址是否为带 http/https 协议的绝对地址。
    /// </summary>
    /// <param name="url">待判断的地址</param>
    /// <returns>是 http/https 绝对地址时为 true</returns>
    private static bool IsHttpUrl(string url)
        => url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
           url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 把绝对地址压成"只有路径与查询串"的形式；相对地址原样返回。
    /// 用字符串切分而不是 Uri 解析，避免相对路径被误判为 file:// 绝对地址。
    /// </summary>
    /// <param name="url">已 Trim 的地址</param>
    /// <returns>剥掉协议与主机后的地址</returns>
    private static string StripOrigin(string url)
    {
        if (IsHttpUrl(url))
        {
            var schemeEnd = url.IndexOf("://", StringComparison.Ordinal) + 3;
            var pathStart = url.IndexOf('/', schemeEnd);
            return pathStart >= 0 ? url[pathStart..] : "/";
        }

        if (url.StartsWith("//", StringComparison.Ordinal))
        {
            var pathStart = url.IndexOf('/', 2);
            return pathStart >= 0 ? url[pathStart..] : "/";
        }

        return url;
    }
}
