namespace LunchFate.Blazor.Client.Navigation;

/// <summary>
/// 本项目的菜单标识常量，供菜单贡献者与授权判断复用。
/// </summary>
public class LunchFateMenus
{
    /// <summary>菜单标识前缀，用于区分不同来源的菜单项。</summary>
    private const string Prefix = "LunchFate";

    /// <summary>首页（大转盘）菜单。</summary>
    public const string Home = Prefix + ".Home";

    /// <summary>店铺管理菜单。</summary>
    public const string Shops = Prefix + ".Shops";

    /// <summary>用户权限管理入口。</summary>
    public const string Permissions = Prefix + ".Permissions";

    /// <summary>抽取记录菜单。</summary>
    public const string DrawRecords = Prefix + ".DrawRecords";

    /// <summary>门店添加记录菜单。</summary>
    public const string ShopCreationRecords = Prefix + ".ShopCreationRecords";
}
