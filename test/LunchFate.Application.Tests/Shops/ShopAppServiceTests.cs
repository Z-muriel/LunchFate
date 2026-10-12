using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Security.Claims;
using Xunit;

namespace LunchFate.Shops;

/// <summary>
/// 店铺应用服务测试，由 EF Core 测试工程驱动，每例独立设置已认证身份。
/// </summary>
public abstract class ShopAppServiceTests<TStartupModule> : LunchFateApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IShopAppService _shopAppService;

    /// <summary>
    /// 在测试应用初始化及种子完成后解析服务，不修改种子使用的身份。
    /// </summary>
    protected ShopAppServiceTests()
    {
        _shopAppService = GetRequiredService<IShopAppService>();
    }

    /// <summary>验证已登录用户可以新增并读取店铺。</summary>
    [Fact]
    public async Task Should_Create_And_Get_Shop()
    {
        using var principal = Login();
        var created = await _shopAppService.CreateAsync(NewShop("老王面馆"));

        created.Id.ShouldNotBe(Guid.Empty);
        created.Name.ShouldBe("老王面馆");
        created.Category.ShouldBe(ShopCategory.Noodles);
        created.IsEnabled.ShouldBeTrue();

        var fetched = await _shopAppService.GetAsync(created.Id);
        fetched.Name.ShouldBe("老王面馆");
        fetched.Address.ShouldBe("人民路 12 号");
        fetched.Rating.ShouldBe(4.5m);
    }

    /// <summary>验证未删除店铺不允许重复名称。</summary>
    [Fact]
    public async Task Should_Reject_Duplicate_Shop_Name()
    {
        using var principal = Login();
        await _shopAppService.CreateAsync(NewShop("老王面馆"));
        await Should.ThrowAsync<UserFriendlyException>(() =>
            _shopAppService.CreateAsync(NewShop("老王面馆")));
    }

    /// <summary>验证软删除后可以重新使用原名称。</summary>
    [Fact]
    public async Task Should_Allow_Recreating_Deleted_Shop_Name()
    {
        using var principal = Login();
        var shop = await _shopAppService.CreateAsync(NewShop("老王面馆"));
        await _shopAppService.DeleteAsync(shop.Id);

        var recreated = await _shopAppService.CreateAsync(NewShop("老王面馆"));
        recreated.Id.ShouldNotBe(shop.Id);
    }

    /// <summary>验证已软删除的店铺不能通过详情接口读取。</summary>
    [Fact]
    public async Task Should_Soft_Delete_Shop()
    {
        using var principal = Login();
        var shop = await _shopAppService.CreateAsync(NewShop("老王面馆"));
        await _shopAppService.DeleteAsync(shop.Id);

        await Should.ThrowAsync<Volo.Abp.Domain.Entities.EntityNotFoundException>(
            () => _shopAppService.GetAsync(shop.Id));
    }

    /// <summary>验证关键字、分类及启停状态筛选。</summary>
    [Fact]
    public async Task Should_Filter_Shops_By_Keyword_And_Status()
    {
        using var principal = Login();
        await _shopAppService.CreateAsync(NewShop("老王面馆", ShopCategory.Noodles));
        await _shopAppService.CreateAsync(NewShop("轻食沙拉铺", ShopCategory.LightMeal, isEnabled: false));

        var byKeyword = await _shopAppService.GetListAsync(new ShopSearchInput
        {
            Keyword = "面馆",
            MaxResultCount = 100
        });
        byKeyword.TotalCount.ShouldBe(1);
        byKeyword.Items.Single().Name.ShouldBe("老王面馆");

        var byCategory = await _shopAppService.GetListAsync(new ShopSearchInput
        {
            Category = ShopCategory.LightMeal,
            MaxResultCount = 100
        });
        byCategory.TotalCount.ShouldBe(1);
        byCategory.Items.Single().Name.ShouldBe("轻食沙拉铺");

        var disabledOnly = await _shopAppService.GetListAsync(new ShopSearchInput
        {
            IsEnabled = false,
            MaxResultCount = 100
        });
        disabledOnly.TotalCount.ShouldBe(1);
        disabledOnly.Items.Single().Name.ShouldBe("轻食沙拉铺");
    }

    /// <summary>验证转盘候选排除停用店铺。</summary>
    [Fact]
    public async Task Should_Only_Return_Enabled_Shops_To_Wheel()
    {
        using var principal = Login();
        await _shopAppService.CreateAsync(NewShop("老王面馆", ShopCategory.Noodles));
        await _shopAppService.CreateAsync(NewShop("轻食沙拉铺", ShopCategory.LightMeal, isEnabled: false));

        var wheel = await _shopAppService.GetEnabledListAsync();
        wheel.Items.Count.ShouldBe(1);
        wheel.Items.Single().Name.ShouldBe("老王面馆");
    }

    /// <summary>验证停用操作不删除店铺但会从转盘移除。</summary>
    [Fact]
    public async Task Should_Disable_Shop_Without_Removing_It()
    {
        using var principal = Login();
        var shop = await _shopAppService.CreateAsync(NewShop("老王面馆"));

        await _shopAppService.UpdateAsync(shop.Id, new CreateUpdateShopDto
        {
            Name = shop.Name,
            Category = shop.Category,
            IsEnabled = false
        });

        var list = await _shopAppService.GetListAsync(new ShopSearchInput { MaxResultCount = 100 });
        list.Items.Single(x => x.Id == shop.Id).IsEnabled.ShouldBeFalse();
        (await _shopAppService.GetEnabledListAsync()).Items.ShouldBeEmpty();
    }

    /// <summary>验证非法排序回退及白名单内的降序排序。</summary>
    [Fact]
    public async Task Should_Fallback_To_Name_Asc_When_Sorting_Not_Whitelisted()
    {
        using var principal = Login();
        await _shopAppService.CreateAsync(NewShop("B 店"));
        await _shopAppService.CreateAsync(NewShop("A 店"));

        var fallback = await _shopAppService.GetListAsync(new ShopSearchInput
        {
            Sorting = "Name; DROP TABLE AppShops --",
            MaxResultCount = 100
        });
        fallback.Items.Select(x => x.Name).ToArray().ShouldBe(new[] { "A 店", "B 店" });

        var descending = await _shopAppService.GetListAsync(new ShopSearchInput
        {
            Sorting = "Name desc",
            MaxResultCount = 100
        });
        descending.Items.Select(x => x.Name).ToArray().ShouldBe(new[] { "B 店", "A 店" });
    }

    /// <summary>验证取消十二家上限后，二十家启用店铺全部返回且不重复。</summary>
    [Fact]
    public async Task Should_Return_All_Twenty_Enabled_Shops_To_Wheel()
    {
        using var principal = Login();
        for (var i = 0; i < 20; i++)
        {
            await _shopAppService.CreateAsync(NewShop($"转盘店铺{i}"));
        }

        var wheel = await _shopAppService.GetEnabledListAsync();
        wheel.Items.Count.ShouldBe(20);
        wheel.Items.Select(x => x.Id).Distinct().Count().ShouldBe(20);
        wheel.Items.Select(x => x.Name).OrderBy(x => x).ToArray()
            .ShouldBe(Enumerable.Range(0, 20).Select(i => $"转盘店铺{i}").OrderBy(x => x).ToArray());
    }

    /// <summary>为当前测试建立可释放的认证身份，避免污染其他测试及种子初始化。</summary>
    private IDisposable Login()
    {
        return GetRequiredService<ICurrentPrincipalAccessor>().Change(new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(AbpClaimTypes.UserId, Guid.NewGuid().ToString()),
            new Claim(AbpClaimTypes.UserName, "门店测试用户")
        }, "Test")));
    }

    /// <summary>创建独立测试所需的合法店铺入参。</summary>
    private static CreateUpdateShopDto NewShop(
        string name,
        ShopCategory category = ShopCategory.Noodles,
        bool isEnabled = true)
    {
        return new CreateUpdateShopDto
        {
            Name = name,
            Category = category,
            Address = "人民路 12 号",
            Phone = "13800000000",
            AveragePrice = 25.5m,
            Rating = 4.5m,
            Tags = "近,辣",
            IsEnabled = isEnabled,
            Remark = "测试店铺"
        };
    }
}
