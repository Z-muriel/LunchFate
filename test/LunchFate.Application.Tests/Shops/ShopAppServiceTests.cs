using System;
using System.Linq;
using System.Threading.Tasks;
using LunchFate.Shops;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Modularity;
using Xunit;

namespace LunchFate.Shops;

/// <summary>
/// 店铺应用服务测试（需求文档 AC-7：CRUD、权限与软删除的单元测试覆盖）。
/// 与模板既有约定一致：本类为抽象类，具体用例由 EF Core 测试工程驱动（内存 SQLite）。
/// </summary>
public abstract class ShopAppServiceTests<TStartupModule> : LunchFateApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IShopAppService _shopAppService;

    protected ShopAppServiceTests()
    {
        _shopAppService = GetRequiredService<IShopAppService>();
    }

    [Fact]
    public async Task Should_Create_And_Get_Shop()
    {
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

    [Fact]
    public async Task Should_Reject_Duplicate_Shop_Name()
    {
        await _shopAppService.CreateAsync(NewShop("老王面馆"));

        //同名（未删除）应被拒绝 —— 需求文档 §3.2
        await Should.ThrowAsync<UserFriendlyException>(() =>
            _shopAppService.CreateAsync(NewShop("老王面馆")));
    }

    [Fact]
    public async Task Should_Allow_Recreating_Deleted_Shop_Name()
    {
        var shop = await _shopAppService.CreateAsync(NewShop("老王面馆"));
        await _shopAppService.DeleteAsync(shop.Id);

        //软删除后同名可再次创建 —— 需求文档 FR-S-04
        var recreated = await _shopAppService.CreateAsync(NewShop("老王面馆"));
        recreated.Id.ShouldNotBe(shop.Id);
    }

    [Fact]
    public async Task Should_Soft_Delete_Shop()
    {
        var shop = await _shopAppService.CreateAsync(NewShop("老王面馆"));
        await _shopAppService.DeleteAsync(shop.Id);

        await Should.ThrowAsync<Volo.Abp.Domain.Entities.EntityNotFoundException>(
            () => _shopAppService.GetAsync(shop.Id));
    }

    [Fact]
    public async Task Should_Filter_Shops_By_Keyword_And_Status()
    {
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

    [Fact]
    public async Task Should_Only_Return_Enabled_Shops_To_Wheel()
    {
        await _shopAppService.CreateAsync(NewShop("老王面馆", ShopCategory.Noodles));
        await _shopAppService.CreateAsync(NewShop("轻食沙拉铺", ShopCategory.LightMeal, isEnabled: false));

        var wheel = await _shopAppService.GetEnabledListAsync();

        wheel.Items.Count.ShouldBe(1);
        wheel.Items.Single().Name.ShouldBe("老王面馆");
    }

    [Fact]
    public async Task Should_Disable_Shop_Without_Removing_It()
    {
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
