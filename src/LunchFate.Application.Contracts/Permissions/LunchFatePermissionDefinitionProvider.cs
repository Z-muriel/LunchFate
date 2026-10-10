using LunchFate.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Volo.Abp.MultiTenancy;

namespace LunchFate.Permissions;

public class LunchFatePermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(LunchFatePermissions.GroupName);

        var booksPermission = myGroup.AddPermission(LunchFatePermissions.Books.Default, L("Permission:Books"));
        booksPermission.AddChild(LunchFatePermissions.Books.Create, L("Permission:Books.Create"));
        booksPermission.AddChild(LunchFatePermissions.Books.Edit, L("Permission:Books.Edit"));
        booksPermission.AddChild(LunchFatePermissions.Books.Delete, L("Permission:Books.Delete"));

        var authorsPermission = myGroup.AddPermission(LunchFatePermissions.Authors.Default, L("Permission:Authors"));
        authorsPermission.AddChild(LunchFatePermissions.Authors.Create, L("Permission:Authors.Create"));
        authorsPermission.AddChild(LunchFatePermissions.Authors.Edit, L("Permission:Authors.Edit"));
        authorsPermission.AddChild(LunchFatePermissions.Authors.Delete, L("Permission:Authors.Delete"));
        //Define your own permissions here. Example:
        //myGroup.AddPermission(LunchFatePermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<LunchFateResource>(name);
    }
}
