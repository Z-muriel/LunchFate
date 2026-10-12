using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LunchFate.Permissions;
using LunchFate.Records;
using LunchFate.Shops;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Shouldly;
using Volo.Abp.Authorization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace LunchFate.EntityFrameworkCore.Applications;

/// <summary>使用独立真实授权链验证记录范围权限、登录策略与店铺新增权限组合。</summary>
[Collection(LunchFateTestConsts.CollectionDefinitionName)]
public class EfCoreRecordAuthorizationTests : RecordAppServiceTestBase<LunchFateRecordAuthorizationTestModule>
{
    /// <summary>确认授权模块已消除始终允许服务，实际权限定义可被真实检查器检查。</summary>
    [Fact]
    public async Task Should_Use_Real_Authorization_And_Register_Both_Record_Permissions()
    {
        GetRequiredService<IAuthorizationService>().ShouldBeOfType<AbpAuthorizationService>();
        GetRequiredService<IAbpAuthorizationService>().ShouldBeOfType<AbpAuthorizationService>();
        GetRequiredService<IMethodInvocationAuthorizationService>().ShouldBeOfType<MethodInvocationAuthorizationService>();
        var checker = GetRequiredService<IPermissionChecker>();
        checker.ShouldBeOfType<PermissionChecker>();
        var providers = GetRequiredService<IOptions<AbpPermissionOptions>>().Value.ValueProviders;
        providers.ToArray().ShouldBe(new[] { typeof(TestUserPermissionValueProvider) });
        var manager = GetRequiredService<IPermissionDefinitionManager>();
        (await manager.GetAsync(LunchFatePermissions.Records.DrawsViewAll)).Name
            .ShouldBe("LunchFate.Records.DrawsViewAll");
        (await manager.GetAsync(LunchFatePermissions.Records.ShopCreationsViewAll)).Name
            .ShouldBe("LunchFate.Records.ShopCreationsViewAll");
        using (Login())
        {
            (await checker.IsGrantedAsync(LunchFatePermissions.Records.DrawsViewAll)).ShouldBeFalse();
            (await checker.IsGrantedAsync(LunchFatePermissions.Records.ShopCreationsViewAll)).ShouldBeFalse();
        }
        using (Login(permissions: new[] { LunchFatePermissions.Records.DrawsViewAll }))
        {
            (await checker.IsGrantedAsync(LunchFatePermissions.Records.DrawsViewAll)).ShouldBeTrue();
            (await checker.IsGrantedAsync(LunchFatePermissions.Records.ShopCreationsViewAll)).ShouldBeFalse();
            var grants = await checker.IsGrantedAsync(new[]
            {
                LunchFatePermissions.Records.DrawsViewAll, LunchFatePermissions.Records.ShopCreationsViewAll
            });
            grants.Result[LunchFatePermissions.Records.DrawsViewAll].ShouldBe(PermissionGrantResult.Granted);
            grants.Result[LunchFatePermissions.Records.ShopCreationsViewAll].ShouldBe(PermissionGrantResult.Prohibited);
        }
    }

    /// <summary>缺少对应权限时查看全部被拒，包括只授予另一种记录权限及店铺管理权限的用户。</summary>
    [Theory]
    [InlineData(true, 0)]
    [InlineData(false, 0)]
    [InlineData(true, 1)]
    [InlineData(false, 1)]
    [InlineData(true, 2)]
    [InlineData(false, 2)]
    public async Task Should_Reject_IncludeAll_Without_Exact_Record_Permission(bool draws, int grantedSet)
    {
        var permissions = grantedSet switch
        {
            0 => Array.Empty<string>(),
            1 => new[] { draws ? LunchFatePermissions.Records.ShopCreationsViewAll : LunchFatePermissions.Records.DrawsViewAll },
            2 => new[] { LunchFatePermissions.Shops.Default, LunchFatePermissions.Shops.Create },
            _ => throw new InvalidOperationException("未知权限组合")
        };
        using var principal = Login(permissions: permissions);
        var own = await SeedRecordPairAsync(Tenant.Id, UserId);
        var input = new RecordSearchInput { IncludeAll = true };
        if (draws)
        {
            await Should.ThrowAsync<AbpAuthorizationException>(() => Records.GetDrawListAsync(input));
            (await Records.GetDrawListAsync(new RecordSearchInput())).Items.Single().Id.ShouldBe(own.DrawId);
        }
        else
        {
            await Should.ThrowAsync<AbpAuthorizationException>(() => Records.GetShopCreationListAsync(input));
            (await Records.GetShopCreationListAsync(new RecordSearchInput())).Items.Single().Id.ShouldBe(own.CreationId);
        }
    }

    /// <summary>获指定查看全部权限仅放开当前租户内用户范围，关闭多租户过滤器仍不跨租户或宿主边界。</summary>
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public async Task Should_Allow_IncludeAll_Only_Inside_Exact_Current_Tenant(bool draws, bool host)
    {
        var permission = draws ? LunchFatePermissions.Records.DrawsViewAll : LunchFatePermissions.Records.ShopCreationsViewAll;
        using var principal = Login(permissions: new[] { permission });
        var currentTenant = host ? (Guid?)null : Guid.NewGuid();
        var own = await SeedRecordPairAsync(currentTenant, UserId);
        var other = await SeedRecordPairAsync(currentTenant, Guid.NewGuid());
        var historical = await SeedRecordPairAsync(currentTenant, null);
        await SeedRecordPairAsync(Guid.NewGuid(), UserId);
        await SeedRecordPairAsync(Guid.NewGuid(), Guid.NewGuid());
        if (!host) await SeedRecordPairAsync(null, UserId);
        using var tenant = Tenant.Change(currentTenant);
        using var filter = GetRequiredService<IDataFilter<IMultiTenant>>().Disable();
        (await WithUnitOfWorkAsync(() => GetRequiredService<IRepository<DrawRecord, Guid>>().GetCountAsync()))
            .ShouldBe(host ? 5 : 6);
        (await WithUnitOfWorkAsync(() => GetRequiredService<IRepository<ShopCreationRecord, Guid>>().GetCountAsync()))
            .ShouldBe(host ? 5 : 6);
        if (draws)
        {
            var all = await Records.GetDrawListAsync(new RecordSearchInput { IncludeAll = true });
            all.TotalCount.ShouldBe(3);
            all.Items.Select(x => x.Id).OrderBy(x => x).ToArray()
                .ShouldBe(new[] { own.DrawId, other.DrawId, historical.DrawId }.OrderBy(x => x).ToArray());
            (await Records.GetDrawListAsync(new RecordSearchInput())).Items.Single().Id.ShouldBe(own.DrawId);
        }
        else
        {
            var all = await Records.GetShopCreationListAsync(new RecordSearchInput { IncludeAll = true });
            all.TotalCount.ShouldBe(3);
            all.Items.Select(x => x.Id).OrderBy(x => x).ToArray()
                .ShouldBe(new[] { own.CreationId, other.CreationId, historical.CreationId }.OrderBy(x => x).ToArray());
            (await Records.GetShopCreationListAsync(new RecordSearchInput())).Items.Single().Id.ShouldBe(own.CreationId);
        }
    }

    /// <summary>单条和批量新增必须同时具有店铺查看及新增权限，任一缺失都不得写门店或记录。</summary>
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    public async Task Should_Require_Default_And_Create_For_Single_And_Batch(bool batch, bool grantDefault, bool grantCreate)
    {
        var permissions = new List<string>();
        if (grantDefault) permissions.Add(LunchFatePermissions.Shops.Default);
        if (grantCreate) permissions.Add(LunchFatePermissions.Shops.Create);
        using var principal = Login(permissions: permissions.ToArray());
        Func<Task> action = batch
            ? () => Shops.BatchCreateAsync(new List<CreateUpdateShopDto> { NewShop("未授权批量") })
            : () => Shops.CreateAsync(NewShop("未授权单条"));
        await Should.ThrowAsync<AbpAuthorizationException>(action);
        (await WithUnitOfWorkAsync(() => GetRequiredService<IRepository<Shop, Guid>>().GetCountAsync())).ShouldBe(0);
        (await Records.GetShopCreationListAsync(new RecordSearchInput())).TotalCount.ShouldBe(0);
    }

    /// <summary>两项权限同时授予后，单条和批量新增各生成一条本人记录。</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Should_Allow_Single_And_Batch_With_Both_Shop_Permissions(bool batch)
    {
        using var principal = Login(permissions: new[] { LunchFatePermissions.Shops.Default, LunchFatePermissions.Shops.Create });
        if (batch)
        {
            var result = await Shops.BatchCreateAsync(new List<CreateUpdateShopDto> { NewShop("授权批量") });
            result.CreatedCount.ShouldBe(1);
            result.SkippedCount.ShouldBe(0);
            result.Errors.ShouldBeEmpty();
        }
        else
        {
            (await Shops.CreateAsync(NewShop("授权单条"))).Id.ShouldNotBe(Guid.Empty);
        }
        (await Records.GetShopCreationListAsync(new RecordSearchInput())).Items.Single().UserId.ShouldBe(UserId);
    }

    /// <summary>仅登录且没有店铺管理或记录查看全部权限，也能抽取并查询本人两种记录。</summary>
    [Fact]
    public async Task Should_Allow_Authenticated_User_Without_Management_Permissions()
    {
        using var principal = Login();
        var own = await SeedRecordPairAsync(Tenant.Id, UserId);
        await SeedRecordPairAsync(Tenant.Id, Guid.NewGuid());
        (await Shops.GetEnabledListAsync()).Items.ShouldBeEmpty();
        var draw = await Records.DrawAsync(new CreateDrawDto { TemporaryShops = new List<TemporaryShopDto> { TemporaryShop() } });
        (await ReadDrawAsync(draw.RecordId)).UserId.ShouldBe(UserId);
        (await Records.GetDrawListAsync(new RecordSearchInput())).TotalCount.ShouldBe(2);
        (await Records.GetShopCreationListAsync(new RecordSearchInput())).Items.Single().Id.ShouldBe(own.CreationId);
        await Should.ThrowAsync<AbpAuthorizationException>(() => Shops.GetListAsync(new ShopSearchInput()));
    }

    /// <summary>真实授权链拒绝匿名用户调用抽取、候选及记录查询接口。</summary>
    [Theory]
    [InlineData("draw")]
    [InlineData("enabled")]
    [InlineData("draw-query")]
    [InlineData("creation-query")]
    public async Task Should_Reject_Anonymous_Through_Real_Authorization(string operation)
    {
        using var principal = Anonymous();
        Func<Task> action = operation switch
        {
            "draw" => () => Records.DrawAsync(new CreateDrawDto { TemporaryShops = new List<TemporaryShopDto> { TemporaryShop() } }),
            "enabled" => () => Shops.GetEnabledListAsync(),
            "draw-query" => () => Records.GetDrawListAsync(new RecordSearchInput()),
            "creation-query" => () => Records.GetShopCreationListAsync(new RecordSearchInput()),
            _ => throw new InvalidOperationException("未知测试操作")
        };
        await Should.ThrowAsync<AbpAuthorizationException>(action);
    }

    /// <summary>反射验证服务类要求登录且无匿名豁免，新增方法显式包含两项店铺权限。</summary>
    [Fact]
    public void Should_Declare_Required_Class_And_Create_Authorization_Attributes()
    {
        foreach (var type in new[] { typeof(RecordAppService), typeof(ShopAppService) })
        {
            type.GetCustomAttributes<AuthorizeAttribute>(true).Any(x => string.IsNullOrEmpty(x.Policy)).ShouldBeTrue();
            type.GetCustomAttributes<AllowAnonymousAttribute>(true).ShouldBeEmpty();
        }
        foreach (var name in new[] { nameof(ShopAppService.CreateAsync), nameof(ShopAppService.BatchCreateAsync) })
        {
            var method = typeof(ShopAppService).GetMethod(name)!;
            method.GetCustomAttributes<AuthorizeAttribute>(true).Select(x => x.Policy).OrderBy(x => x).ToArray()
                .ShouldBe(new[] { LunchFatePermissions.Shops.Default, LunchFatePermissions.Shops.Create }.OrderBy(x => x).ToArray());
            method.GetCustomAttributes<AllowAnonymousAttribute>(true).ShouldBeEmpty();
        }
        foreach (var method in new[]
        {
            typeof(RecordAppService).GetMethod(nameof(RecordAppService.DrawAsync))!,
            typeof(RecordAppService).GetMethod(nameof(RecordAppService.GetDrawListAsync))!,
            typeof(RecordAppService).GetMethod(nameof(RecordAppService.GetShopCreationListAsync))!,
            typeof(ShopAppService).GetMethod(nameof(ShopAppService.GetEnabledListAsync))!
        })
        {
            method.GetCustomAttributes<AllowAnonymousAttribute>(true).ShouldBeEmpty();
        }
    }
}
