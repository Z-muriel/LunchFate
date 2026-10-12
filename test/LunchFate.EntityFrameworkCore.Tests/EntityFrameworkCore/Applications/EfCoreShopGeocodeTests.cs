using System;
using System.Security.Claims;
using System.Threading.Tasks;
using LunchFate.Integration.Gaode;
using LunchFate.Shops;
using Shouldly;
using Volo.Abp.Security.Claims;
using Xunit;

namespace LunchFate.EntityFrameworkCore.Applications;

/// <summary>
/// 在内存 SQLite 上验证地址地理编码的触发时机（FR-M-01）：
/// 新增按地址解析、编辑改地址必须重新解析、未改地址不得重复解析、显式坐标不得被覆盖。
/// </summary>
[Collection(LunchFateTestConsts.CollectionDefinitionName)]
public class EfCoreShopGeocodeTests : LunchFateApplicationTestBase<LunchFateShopGeocodeTestModule>
{
    private const string FirstAddress = "广东省广州市海珠区新港西路 135 号";
    private const string SecondAddress = "广东省广州市天河区体育西路 1 号";

    private IShopAppService Shops => GetRequiredService<IShopAppService>();

    /// <summary>测试模块把 IGaodeMapService 指向同一个记录型单例，因此可直接读取调用记录。</summary>
    private RecordingGaodeMapService Gaode =>
        (RecordingGaodeMapService)GetRequiredService<IGaodeMapService>();

    /// <summary>新增时按地址解析出坐标，且只调用一次地理编码。</summary>
    [Fact]
    public async Task Should_Geocode_Once_When_Creating_With_Address()
    {
        using var principal = Login();

        var created = await Shops.CreateAsync(NewShop("新增解析", FirstAddress));

        Gaode.GeocodeRequests.ShouldBe(new[] { FirstAddress });
        created.Longitude.ShouldBe(100d + FirstAddress.Length);
        created.Latitude.ShouldBe(20d + FirstAddress.Length);
    }

    /// <summary>
    /// 回归：编辑变更地址必须重新地理编码。
    /// 原实现先 Apply 再比较地址，导致比较恒为相等、编辑场景永远不重新解析。
    /// </summary>
    [Fact]
    public async Task Should_Regeocode_When_Address_Changes_On_Update()
    {
        using var principal = Login();
        var created = await Shops.CreateAsync(NewShop("编辑重解析", FirstAddress));

        var updated = await Shops.UpdateAsync(created.Id, NewShop("编辑重解析", SecondAddress));

        Gaode.GeocodeRequests.ShouldBe(new[] { FirstAddress, SecondAddress });
        updated.Address.ShouldBe(SecondAddress);
        updated.Longitude.ShouldBe(100d + SecondAddress.Length);
        updated.Latitude.ShouldBe(20d + SecondAddress.Length);
        updated.Longitude.ShouldNotBe(created.Longitude);
    }

    /// <summary>地址未变化时不得重复调用地理编码，坐标保持原值。</summary>
    [Fact]
    public async Task Should_Not_Regeocode_When_Address_Unchanged_On_Update()
    {
        using var principal = Login();
        var created = await Shops.CreateAsync(NewShop("原名称", FirstAddress));

        var updated = await Shops.UpdateAsync(created.Id, NewShop("改后名称", FirstAddress));

        Gaode.GeocodeRequests.Count.ShouldBe(1);
        updated.Name.ShouldBe("改后名称");
        updated.Longitude.ShouldBe(created.Longitude);
        updated.Latitude.ShouldBe(created.Latitude);
    }

    /// <summary>地址被清空时坐标一并清空，避免导航指向旧地址。</summary>
    [Fact]
    public async Task Should_Clear_Coordinates_When_Address_Removed_On_Update()
    {
        using var principal = Login();
        var created = await Shops.CreateAsync(NewShop("清空地址", FirstAddress));

        var updated = await Shops.UpdateAsync(created.Id, NewShop("清空地址", null));

        updated.Address.ShouldBeNull();
        updated.Longitude.ShouldBeNull();
        updated.Latitude.ShouldBeNull();
    }

    /// <summary>客户端显式提供 POI 坐标时直接采信，不再调用地理编码（FR-M-04）。</summary>
    [Fact]
    public async Task Should_Keep_Explicit_Coordinates_Without_Geocoding()
    {
        using var principal = Login();

        var input = NewShop("显式坐标", FirstAddress);
        input.Longitude = 113.293333;
        input.Latitude = 23.092112;

        var created = await Shops.CreateAsync(input);

        Gaode.GeocodeRequests.ShouldBeEmpty();
        created.Longitude.ShouldBe(113.293333);
        created.Latitude.ShouldBe(23.092112);
    }

    /// <summary>只提供经度视为无效坐标，仍按地址解析，避免写出半截坐标。</summary>
    [Fact]
    public async Task Should_Fallback_To_Geocoding_When_Only_One_Coordinate_Provided()
    {
        using var principal = Login();

        var input = NewShop("半截坐标", FirstAddress);
        input.Longitude = 113.293333;

        var created = await Shops.CreateAsync(input);

        Gaode.GeocodeRequests.ShouldBe(new[] { FirstAddress });
        created.Longitude.ShouldBe(100d + FirstAddress.Length);
        created.Latitude.ShouldBe(20d + FirstAddress.Length);
    }

    /// <summary>建立可释放的已认证身份，满足服务端的显式登录守卫。</summary>
    private IDisposable Login()
    {
        return GetRequiredService<ICurrentPrincipalAccessor>().Change(new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(AbpClaimTypes.UserId, Guid.NewGuid().ToString()),
            new Claim(AbpClaimTypes.UserName, "地理编码测试用户")
        }, "Test")));
    }

    /// <summary>构造合法的门店新增入参。</summary>
    private static CreateUpdateShopDto NewShop(string name, string? address)
    {
        return new CreateUpdateShopDto
        {
            Name = name,
            Category = ShopCategory.Noodles,
            Address = address,
            AveragePrice = 28m,
            IsEnabled = true
        };
    }
}
