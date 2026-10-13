using System;
using System.Security.Claims;
using System.Threading.Tasks;
using LunchFate.Integration.Gaode;
using LunchFate.Maps;
using Shouldly;
using Volo.Abp.Security.Claims;
using Xunit;

namespace LunchFate.EntityFrameworkCore.Applications;

/// <summary>
/// 在内存 SQLite 上验证"搜索附近"的类型召回契约：
/// 关键字可为空（改由类型筛选召回附近同类店铺），类型筛选项原样透传到高德集成层。
/// </summary>
[Collection(LunchFateTestConsts.CollectionDefinitionName)]
public class EfCoreMapNearbySearchTests : LunchFateApplicationTestBase<LunchFateShopGeocodeTestModule>
{
    private const string FoodType = "050000";
    private const double Longitude = 113.293333;
    private const double Latitude = 23.092112;

    private IMapAppService Maps => GetRequiredService<IMapAppService>();

    /// <summary>测试模块把 IGaodeMapService 指向同一个记录型单例，因此可直接读取调用记录。</summary>
    private RecordingGaodeMapService Gaode =>
        (RecordingGaodeMapService)GetRequiredService<IGaodeMapService>();

    /// <summary>关键字留空不再被校验拒绝，类型筛选（默认餐饮服务）原样传给高德。</summary>
    [Fact]
    public async Task Should_Search_By_Type_When_Keywords_Empty()
    {
        using var principal = Login();

        var result = await Maps.SearchNearbyAsync(NewInput(keywords: string.Empty, types: FoodType));

        result.Items.ShouldBeEmpty();
        var request = Gaode.SearchRequests.ShouldHaveSingleItem();
        request.Keywords.ShouldBe(string.Empty);
        request.Types.ShouldBe(FoodType);
        request.PageNum.ShouldBe(1);
    }

    /// <summary>类型也留空时传空串，由高德按文档回退为餐饮/生活服务/商务住宅三类。</summary>
    [Fact]
    public async Task Should_Pass_Empty_Types_When_Not_Selected()
    {
        using var principal = Login();

        await Maps.SearchNearbyAsync(NewInput(keywords: "面馆", types: string.Empty));

        var request = Gaode.SearchRequests.ShouldHaveSingleItem();
        request.Keywords.ShouldBe("面馆");
        request.Types.ShouldBe(string.Empty);
    }

    /// <summary>SkipCount 换算为高德的 1 基页码，翻页时页码递增。</summary>
    [Fact]
    public async Task Should_Convert_SkipCount_To_Gaode_Page_Number()
    {
        using var principal = Login();

        await Maps.SearchNearbyAsync(NewInput(keywords: string.Empty, types: FoodType, skipCount: 40));

        Gaode.SearchRequests.ShouldHaveSingleItem().PageNum.ShouldBe(3);
    }

    /// <summary>建立可释放的已认证身份，满足服务端的显式登录守卫。</summary>
    private IDisposable Login()
    {
        return GetRequiredService<ICurrentPrincipalAccessor>().Change(new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(AbpClaimTypes.UserId, Guid.NewGuid().ToString()),
            new Claim(AbpClaimTypes.UserName, "附近搜索测试用户")
        }, "Test")));
    }

    /// <summary>构造合法的附近搜索入参（每页 20 条，与界面默认一致）。</summary>
    private static NearbySearchInput NewInput(string keywords, string types, int skipCount = 0)
    {
        return new NearbySearchInput
        {
            Keywords = keywords,
            Types = types,
            Longitude = Longitude,
            Latitude = Latitude,
            Radius = 3000,
            SkipCount = skipCount,
            MaxResultCount = 20
        };
    }
}
