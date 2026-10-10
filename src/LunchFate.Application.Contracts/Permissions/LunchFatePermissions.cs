namespace LunchFate.Permissions;

/// <summary>
/// 本项目持有的权限名称常量（需求文档 §7）。
/// </summary>
public static class LunchFatePermissions
{
    /// <summary>权限组名，同时作为顶级权限前缀。</summary>
    public const string GroupName = "LunchFate";

    /// <summary>店铺管理（需求文档 §7）。</summary>
    public static class Shops
    {
        /// <summary>店铺管理查看权限（菜单与列表的入口权限）。</summary>
        public const string Default = GroupName + ".Shops";

        /// <summary>新增店铺。</summary>
        public const string Create = Default + ".Create";

        /// <summary>编辑店铺（含启停切换）。</summary>
        public const string Edit = Default + ".Edit";

        /// <summary>删除店铺。</summary>
        public const string Delete = Default + ".Delete";
    }
}
