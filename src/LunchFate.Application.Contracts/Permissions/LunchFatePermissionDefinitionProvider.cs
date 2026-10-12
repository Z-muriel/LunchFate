using LunchFate.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace LunchFate.Permissions;

/// <summary>
/// 定义本项目持有的权限组与权限项（需求文档 §7）。
/// </summary>
public class LunchFatePermissionDefinitionProvider : PermissionDefinitionProvider
{
    /// <summary>
    /// 注册权限组 <c>LunchFate</c>、店铺操作权限及两类当前租户全部记录查看权限。
    /// </summary>
    /// <param name="context">ABP 权限定义上下文</param>
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(LunchFatePermissions.GroupName);

        var shopsPermission = myGroup.AddPermission(LunchFatePermissions.Shops.Default, L("Permission:Shops"));
        shopsPermission.AddChild(LunchFatePermissions.Shops.Create, L("Permission:Shops.Create"));
        shopsPermission.AddChild(LunchFatePermissions.Shops.Edit, L("Permission:Shops.Edit"));
        shopsPermission.AddChild(LunchFatePermissions.Shops.Delete, L("Permission:Shops.Delete"));

        // 本人记录无需额外授权；仅跨用户查看当前租户记录时要求明确授权。
        myGroup.AddPermission(LunchFatePermissions.Records.DrawsViewAll, L("Permission:Records.DrawsViewAll"));
        myGroup.AddPermission(LunchFatePermissions.Records.ShopCreationsViewAll, L("Permission:Records.ShopCreationsViewAll"));
    }

    /// <summary>
    /// 创建指向本项目本地化资源的可本地化字符串。
    /// </summary>
    /// <param name="name">本地化键名</param>
    /// <returns>可本地化字符串</returns>
    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<LunchFateResource>(name);
    }
}
