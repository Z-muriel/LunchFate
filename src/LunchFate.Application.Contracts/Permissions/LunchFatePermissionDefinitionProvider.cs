using LunchFate.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace LunchFate.Permissions;

public class LunchFatePermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(LunchFatePermissions.GroupName);

        //Define your own permissions here. Example:
        //myGroup.AddPermission(LunchFatePermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<LunchFateResource>(name);
    }
}
