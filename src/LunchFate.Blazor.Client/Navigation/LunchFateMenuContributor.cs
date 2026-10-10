using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using LunchFate.Localization;
using LunchFate.MultiTenancy;
using LunchFate.Permissions;
using Volo.Abp.Account.Localization;
using Volo.Abp.UI.Navigation;
using Localization.Resources.AbpUi;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.SettingManagement.Blazor.Menus;
using Volo.Abp.TenantManagement.Blazor.Navigation;
using Volo.Abp.Identity.Blazor;

namespace LunchFate.Blazor.Client.Navigation;

/// <summary>
/// 组装侧边栏主菜单与用户菜单（首页、店铺管理、管理分组）。
/// </summary>
public class LunchFateMenuContributor : IMenuContributor
{
    private readonly IConfiguration _configuration;

    public LunchFateMenuContributor(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task ConfigureMenuAsync(MenuConfigurationContext context)
    {
        if (context.Menu.Name == StandardMenus.Main)
        {
            await ConfigureMainMenuAsync(context);
        }
        else if (context.Menu.Name == StandardMenus.User)
        {
            await ConfigureUserMenuAsync(context);
        }
    }

    /// <summary>
    /// 构建主菜单：首页、管理分组排序，以及按权限追加的店铺管理入口。
    /// </summary>
    /// <param name="context">菜单配置上下文</param>
    private static async Task ConfigureMainMenuAsync(MenuConfigurationContext context)
    {
        var l = context.GetLocalizer<LunchFateResource>();

        //Administration
        var administration = context.Menu.GetAdministration();
        administration.Order = 6;
        context.Menu.AddItem(new ApplicationMenuItem(
            LunchFateMenus.Home,
            l["Menu:Home"],
            "/",
            icon: "fas fa-home",
            order: 1
        ));

        //MultiTenancyConsts.IsEnabled 是编译期常量，此处按其取值直接排序。
        if (MultiTenancyConsts.IsEnabled)
        {
            administration.SetSubItemOrder(TenantManagementMenuNames.GroupName, 1);
        }

        administration.SetSubItemOrder(IdentityMenuNames.GroupName, 2);
        administration.SetSubItemOrder(SettingManagementMenus.GroupName, 3);

        //店铺管理（需求文档 §4：菜单"管理 → 店铺管理"）
        if (await context.IsGrantedAsync(LunchFatePermissions.Shops.Default))
        {
            administration.AddItem(new ApplicationMenuItem(
                LunchFateMenus.Shops,
                l["Menu:Shops"],
                url: "/shops",
                icon: "fas fa-store"
            ));
        }
    }

    /// <summary>
    /// 构建用户菜单：WASM 环境跳转授权服务器的"我的账号"。
    /// </summary>
    /// <param name="context">菜单配置上下文</param>
    private async Task ConfigureUserMenuAsync(MenuConfigurationContext context)
    {
        if (OperatingSystem.IsBrowser())
        {
            //Blazor wasm menu items
            var authServerUrl = _configuration["AuthServer:Authority"] ?? "";
            var accountResource = context.GetLocalizer<AccountResource>();
            context.Menu.AddItem(new ApplicationMenuItem("Account.Manage", accountResource["MyAccount"], $"{authServerUrl.EnsureEndsWith('/')}Account/Manage", icon: "fa fa-cog", order: 900,  target: "_blank").RequireAuthenticated());

        }
        else
        {
            //Blazor server menu items
        }
        await Task.CompletedTask;
    }
}
