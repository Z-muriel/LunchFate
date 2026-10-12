using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LunchFate.Records;
using LunchFate.Shops;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Modularity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Security.Claims;
using Xunit;

namespace LunchFate.EntityFrameworkCore.Applications;

/// <summary>提供记录测试的独立身份、快照种子及数据库读取辅助方法。</summary>
public abstract class RecordAppServiceTestBase<TStartupModule> : LunchFateApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    protected const string PermissionClaimType = "LunchFate.Test.Permission";
    protected readonly Guid UserId = Guid.NewGuid();
    protected IRecordAppService Records => GetRequiredService<IRecordAppService>();
    protected IShopAppService Shops => GetRequiredService<IShopAppService>();
    protected ICurrentTenant Tenant => GetRequiredService<ICurrentTenant>();

    /// <summary>只在测试执行期间切换带认证类型的用户和显式权限声明。</summary>
    protected IDisposable Login(Guid? userId = null, params string[] permissions)
    {
        var claims = new List<Claim>
        {
            new(AbpClaimTypes.UserId, (userId ?? UserId).ToString()),
            new(AbpClaimTypes.UserName, "记录测试用户")
        };
        claims.AddRange(permissions.Select(permission => new Claim(PermissionClaimType, permission)));
        return GetRequiredService<ICurrentPrincipalAccessor>()
            .Change(new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")));
    }

    /// <summary>建立没有用户标识且没有认证类型的匿名作用域。</summary>
    protected IDisposable Anonymous()
    {
        return GetRequiredService<ICurrentPrincipalAccessor>().Change(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    /// <summary>生成不触发地图请求的合法新增入参。</summary>
    protected static CreateUpdateShopDto NewShop(string name)
    {
        return new CreateUpdateShopDto
        {
            Name = name,
            Category = ShopCategory.Noodles,
            AveragePrice = 28m,
            IsEnabled = true
        };
    }

    /// <summary>生成合法临时兴趣点候选，不访问地图服务。</summary>
    protected static TemporaryShopDto TemporaryShop(int index = 0)
    {
        return new TemporaryShopDto
        {
            PoiId = $"test-poi-{index}",
            Name = $"临时候选{index}",
            Address = $"测试地址{index}",
            Longitude = 116.4,
            Latitude = 39.9
        };
    }

    /// <summary>在独立工作单元重新读取已保存抽取记录。</summary>
    protected Task<DrawRecord> ReadDrawAsync(Guid id)
    {
        return WithUnitOfWorkAsync(() => GetRequiredService<IRepository<DrawRecord, Guid>>().GetAsync(id));
    }

    /// <summary>在独立工作单元统计抽取记录，验证拒绝请求没有插入记录。</summary>
    protected Task<long> DrawCountAsync()
    {
        return WithUnitOfWorkAsync(() => GetRequiredService<IRepository<DrawRecord, Guid>>().GetCountAsync());
    }

    /// <summary>创建当前身份可查询的一对种子快照，不依赖门店或租户实体外键。</summary>
    protected async Task<(Guid DrawId, Guid CreationId)> SeedRecordPairAsync(Guid? tenantId, Guid? userId)
    {
        var draw = new DrawRecord(Guid.NewGuid(), tenantId, userId, "种子用户", Guid.NewGuid(),
            "种子店铺", "种子地址", ShopCategory.Other, 10m, new DateTime(2026, 10, 12), false, null);
        var creation = new ShopCreationRecord(Guid.NewGuid(), tenantId, userId, "种子用户", draw.ShopId,
            "种子店铺", "种子地址", ShopCategory.Other, 10m, draw.RecordTime);
        using (Tenant.Change(tenantId))
        {
            await WithUnitOfWorkAsync(async () =>
            {
                await GetRequiredService<IRepository<DrawRecord, Guid>>().InsertAsync(draw, autoSave: true);
                await GetRequiredService<IRepository<ShopCreationRecord, Guid>>().InsertAsync(creation, autoSave: true);
            });
        }
        return (draw.Id, creation.Id);
    }

    /// <summary>显式构造未经过参数校验拦截器的服务，直接验证生产服务自身的输入守卫。</summary>
    protected RecordAppService UninterceptedRecords()
    {
        return new RecordAppService(
            GetRequiredService<IRepository<DrawRecord, Guid>>(),
            GetRequiredService<IRepository<ShopCreationRecord, Guid>>(),
            GetRequiredService<IRepository<Shop, Guid>>())
        {
            LazyServiceProvider = GetRequiredService<IAbpLazyServiceProvider>()
        };
    }

    /// <summary>验证非法候选被服务自身拒绝且数据库记录数量保持不变。</summary>
    protected async Task AssertInvalidDrawAsync(CreateDrawDto input)
    {
        var count = await DrawCountAsync();
        await WithUnitOfWorkAsync(async () =>
            await Should.ThrowAsync<UserFriendlyException>(() => UninterceptedRecords().DrawAsync(input)));
        (await DrawCountAsync()).ShouldBe(count);
    }
}

/// <summary>在内存 SQLite 上验证记录持久化、快照、输入守卫与默认查询隔离。</summary>
[Collection(LunchFateTestConsts.CollectionDefinitionName)]
public class EfCoreRecordAppServiceTests : RecordAppServiceTestBase<LunchFateEntityFrameworkCoreTestModule>
{
    /// <summary>永久候选抽取只保存一条记录，并使用服务端身份、时间及完整快照。</summary>
    [Fact]
    public async Task Should_Persist_Permanent_Draw_Exactly_Once()
    {
        using var principal = Login();
        var input = NewShop("永久店铺");
        var shop = await Shops.CreateAsync(input);
        var before = await DrawCountAsync();
        var startedAt = GetRequiredService<Volo.Abp.Timing.IClock>().Now;
        var result = await Records.DrawAsync(new CreateDrawDto { ShopIds = new List<Guid> { shop.Id } });
        var finishedAt = GetRequiredService<Volo.Abp.Timing.IClock>().Now;
        var saved = await ReadDrawAsync(result.RecordId);

        result.RecordId.ShouldNotBe(Guid.Empty);
        result.WinnerIndex.ShouldBe(0);
        result.Shop.Id.ShouldBe(shop.Id);
        (await DrawCountAsync()).ShouldBe(before + 1);
        saved.TenantId.ShouldBe(Tenant.Id);
        saved.UserId.ShouldBe(UserId);
        saved.UserName.ShouldBe("记录测试用户");
        saved.ShopId.ShouldBe(shop.Id);
        saved.ShopName.ShouldBe(input.Name);
        saved.Address.ShouldBe(input.Address);
        saved.Category.ShouldBe(input.Category);
        saved.AveragePrice.ShouldBe(input.AveragePrice);
        saved.IsTemporary.ShouldBeFalse();
        saved.PoiId.ShouldBeNull();
        saved.RecordTime.ShouldBeGreaterThanOrEqualTo(startedAt);
        saved.RecordTime.ShouldBeLessThanOrEqualTo(finishedAt);
        (await Records.GetDrawListAsync(new RecordSearchInput())).Items.Single().Id.ShouldBe(saved.Id);
    }

    /// <summary>临时兴趣点抽取保存快照和兴趣点标识，但不新增永久店铺。</summary>
    [Fact]
    public async Task Should_Persist_Temporary_Draw_Without_Creating_Shop()
    {
        using var principal = Login();
        using var tenant = Tenant.Change(Guid.NewGuid());
        var candidate = TemporaryShop();
        var shopCount = await WithUnitOfWorkAsync(() => GetRequiredService<IRepository<Shop, Guid>>().GetCountAsync());
        var result = await Records.DrawAsync(new CreateDrawDto { TemporaryShops = new List<TemporaryShopDto> { candidate } });
        var saved = await ReadDrawAsync(result.RecordId);

        (await DrawCountAsync()).ShouldBe(1);
        saved.TenantId.ShouldBe(Tenant.Id);
        saved.UserId.ShouldBe(UserId);
        saved.UserName.ShouldBe("记录测试用户");
        saved.IsTemporary.ShouldBeTrue();
        saved.ShopId.ShouldBeNull();
        saved.PoiId.ShouldBe(candidate.PoiId);
        saved.ShopName.ShouldBe(candidate.Name);
        saved.Address.ShouldBe(candidate.Address);
        saved.Category.ShouldBe(ShopCategory.Other);
        saved.AveragePrice.ShouldBe(0m);
        result.Shop.Id.ShouldBe(Guid.Empty);
        result.Shop.Longitude.ShouldBe(candidate.Longitude);
        result.Shop.Latitude.ShouldBe(candidate.Latitude);
        (await WithUnitOfWorkAsync(() => GetRequiredService<IRepository<Shop, Guid>>().GetCountAsync())).ShouldBe(shopCount);
        candidate.Name = "客户端随后改名";
        candidate.PoiId = "客户端随后换标识";
        var queried = (await Records.GetDrawListAsync(new RecordSearchInput())).Items.Single();
        queried.ShopName.ShouldBe("临时候选0");
        queried.PoiId.ShouldBe("test-poi-0");
    }

    /// <summary>二十家永久候选均被接受，赢家索引必须对应输入顺序而非数据库顺序。</summary>
    [Fact]
    public async Task Should_Accept_Twenty_Permanent_Candidates_In_Input_Order()
    {
        using var principal = Login();
        var candidates = new List<ShopDto>();
        for (var i = 0; i < 20; i++)
        {
            candidates.Add(await Shops.CreateAsync(NewShop($"永久顺序{i:00}")));
        }
        candidates.Reverse();
        var result = await Records.DrawAsync(new CreateDrawDto { ShopIds = candidates.Select(x => x.Id).ToList() });
        result.WinnerIndex.ShouldBeInRange(0, 19);
        result.Shop.Id.ShouldBe(candidates[result.WinnerIndex].Id);
        result.Shop.Name.ShouldBe(candidates[result.WinnerIndex].Name);
        (await ReadDrawAsync(result.RecordId)).ShopId.ShouldBe(candidates[result.WinnerIndex].Id);
        (await DrawCountAsync()).ShouldBe(1);
    }

    /// <summary>二十家临时候选不被截断，赢家和兴趣点快照按输入索引对应。</summary>
    [Fact]
    public async Task Should_Accept_Twenty_Temporary_Candidates_In_Input_Order()
    {
        using var principal = Login();
        var candidates = Enumerable.Range(0, 20).Reverse().Select(TemporaryShop).ToList();
        var result = await Records.DrawAsync(new CreateDrawDto { TemporaryShops = candidates });
        result.WinnerIndex.ShouldBeInRange(0, 19);
        result.Shop.Name.ShouldBe(candidates[result.WinnerIndex].Name);
        (await ReadDrawAsync(result.RecordId)).PoiId.ShouldBe(candidates[result.WinnerIndex].PoiId);
        (await DrawCountAsync()).ShouldBe(1);
    }

    /// <summary>新增和抽取快照在店铺改名、软删除或物理删除后均保持原值。</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Should_Keep_Both_Snapshots_After_Rename_And_Delete(bool hardDelete)
    {
        using var principal = Login();
        var input = NewShop("原始快照店铺");
        var shop = await Shops.CreateAsync(input);
        await WithUnitOfWorkAsync(async () =>
        {
            var repository = GetRequiredService<IRepository<Shop, Guid>>();
            var storedShop = await repository.GetAsync(shop.Id);
            storedShop.Address = "抽取时地址";
            await repository.UpdateAsync(storedShop, autoSave: true);
        });
        var draw = await Records.DrawAsync(new CreateDrawDto { ShopIds = new List<Guid> { shop.Id } });
        var originalDraw = await ReadDrawAsync(draw.RecordId);
        var originalCreation = (await Records.GetShopCreationListAsync(new RecordSearchInput())).Items.Single();
        input.Name = "改名后的店铺";
        input.Category = ShopCategory.LightMeal;
        input.AveragePrice = 99m;
        await Shops.UpdateAsync(shop.Id, input);
        (await Records.GetDrawListAsync(new RecordSearchInput())).Items.Single().ShopName.ShouldBe("原始快照店铺");
        (await Records.GetShopCreationListAsync(new RecordSearchInput())).Items.Single().ShopName.ShouldBe("原始快照店铺");
        await Shops.DeleteAsync(shop.Id);
        if (hardDelete)
        {
            await WithUnitOfWorkAsync(async () =>
            {
                var db = await GetRequiredService<IDbContextProvider<LunchFateDbContext>>().GetDbContextAsync();
                (await db.Set<Shop>().IgnoreQueryFilters().Where(x => x.Id == shop.Id).ExecuteDeleteAsync()).ShouldBe(1);
                (await db.Set<Shop>().IgnoreQueryFilters().CountAsync(x => x.Id == shop.Id)).ShouldBe(0);
            });
        }
        var savedDraw = (await Records.GetDrawListAsync(new RecordSearchInput())).Items.Single();
        var savedCreation = (await Records.GetShopCreationListAsync(new RecordSearchInput())).Items.Single();
        savedDraw.Id.ShouldBe(originalDraw.Id);
        savedDraw.ShopId.ShouldBe(shop.Id);
        savedDraw.ShopName.ShouldBe("原始快照店铺");
        savedDraw.Address.ShouldBe("抽取时地址");
        savedDraw.Category.ShouldBe(ShopCategory.Noodles);
        savedDraw.AveragePrice.ShouldBe(28m);
        savedDraw.UserId.ShouldBe(UserId);
        savedDraw.RecordTime.ShouldBe(originalDraw.RecordTime);
        savedCreation.Id.ShouldBe(originalCreation.Id);
        savedCreation.ShopId.ShouldBe(shop.Id);
        savedCreation.ShopName.ShouldBe("原始快照店铺");
        savedCreation.Address.ShouldBe(originalCreation.Address);
        savedCreation.Category.ShouldBe(ShopCategory.Noodles);
        savedCreation.AveragePrice.ShouldBe(28m);
        savedCreation.UserId.ShouldBe(UserId);
        savedCreation.RecordTime.ShouldBe(originalCreation.RecordTime);
    }

    /// <summary>单条新增只生成一条当前操作者快照，重复名称拒绝不额外写记录。</summary>
    [Fact]
    public async Task Should_Create_One_Record_For_Single_Success_And_None_For_Duplicate()
    {
        using var principal = Login();
        var input = NewShop("单条新增");
        var shop = await Shops.CreateAsync(input);
        await Should.ThrowAsync<UserFriendlyException>(() => Shops.CreateAsync(input));
        var result = await Records.GetShopCreationListAsync(new RecordSearchInput());
        result.TotalCount.ShouldBe(1);
        var record = result.Items.Single();
        record.ShopId.ShouldBe(shop.Id);
        record.ShopName.ShouldBe(input.Name);
        record.UserId.ShouldBe(UserId);
        record.UserName.ShouldBe("记录测试用户");
        record.Category.ShouldBe(input.Category);
        record.AveragePrice.ShouldBe(input.AveragePrice);
        var saved = await WithUnitOfWorkAsync(() => GetRequiredService<IRepository<ShopCreationRecord, Guid>>().GetAsync(record.Id));
        saved.TenantId.ShouldBe(Tenant.Id);
    }

    /// <summary>批量成功项各写一条记录，同名跳过项不产生记录且不重复计数。</summary>
    [Fact]
    public async Task Should_Create_One_Record_Per_Batch_Success_And_None_Per_Skip()
    {
        using var principal = Login();
        var existing = await Shops.CreateAsync(NewShop("批量已存在"));
        var result = await Shops.BatchCreateAsync(new List<CreateUpdateShopDto>
        {
            NewShop("批量已存在"), NewShop("批量新增"), NewShop("批量新增")
        });
        result.CreatedCount.ShouldBe(1);
        result.SkippedCount.ShouldBe(2);
        result.Errors.ShouldBeEmpty();
        var records = await Records.GetShopCreationListAsync(new RecordSearchInput());
        records.TotalCount.ShouldBe(2);
        records.Items.Count(x => x.ShopId == existing.Id).ShouldBe(1);
        records.Items.Count(x => x.ShopName == "批量新增").ShouldBe(1);
        records.Items.All(x => x.UserId == UserId).ShouldBeTrue();
        var newShop = (await Shops.GetListAsync(new ShopSearchInput { Keyword = "批量新增" })).Items.Single();
        records.Items.Single(x => x.ShopName == "批量新增").ShopId.ShouldBe(newShop.Id);
    }

    /// <summary>空候选不能抽取或写入记录。</summary>
    [Fact]
    public async Task Should_Reject_Empty_Candidates()
    {
        using var principal = Login();
        await AssertInvalidDrawAsync(new CreateDrawDto());
    }

    /// <summary>重复永久标识不能改变抽取权重或生成记录。</summary>
    [Fact]
    public async Task Should_Reject_Duplicate_Permanent_Ids()
    {
        using var principal = Login();
        var shop = await Shops.CreateAsync(NewShop("重复候选"));
        await AssertInvalidDrawAsync(new CreateDrawDto { ShopIds = new List<Guid> { shop.Id, shop.Id } });
    }

    /// <summary>
    /// 门店库候选与附近候选可以合并抽取：候选顺序为门店库在前、附近在后，
    /// 返回的 WinnerIndex 直接对应这个并集顺序，记录来源与获胜者一致。
    /// </summary>
    [Fact]
    public async Task Should_Accept_Mixed_Candidates_And_Record_Winner_Source()
    {
        using var principal = Login();
        var shop = await Shops.CreateAsync(NewShop("混合候选门店"));
        var temporary = TemporaryShop(7);

        var mixed = new CreateDrawDto
        {
            ShopIds = new List<Guid> { shop.Id },
            TemporaryShops = new List<TemporaryShopDto> { temporary }
        };

        // 并集大小为 2：索引 0 是门店库候选、索引 1 是附近候选。
        // 等概率抽取无法指定获胜者，多抽几次直到两种来源都出现过（30 次全落同一边的概率约 2^-29）。
        var seenPermanent = false;
        var seenTemporary = false;
        for (var i = 0; i < 30 && !(seenPermanent && seenTemporary); i++)
        {
            var draw = await Records.DrawAsync(mixed);
            var saved = await ReadDrawAsync(draw.RecordId);

            draw.WinnerIndex.ShouldBeInRange(0, 1);
            if (draw.WinnerIndex == 0)
            {
                draw.Shop.Id.ShouldBe(shop.Id);
                draw.Shop.Name.ShouldBe(shop.Name);
                saved.IsTemporary.ShouldBeFalse();
                saved.ShopId.ShouldBe(shop.Id);
                saved.PoiId.ShouldBeNull();
                seenPermanent = true;
            }
            else
            {
                draw.Shop.Id.ShouldBe(Guid.Empty);
                draw.Shop.Name.ShouldBe(temporary.Name);
                saved.IsTemporary.ShouldBeTrue();
                saved.ShopId.ShouldBeNull();
                saved.PoiId.ShouldBe(temporary.PoiId);
                seenTemporary = true;
            }
        }

        seenPermanent.ShouldBeTrue();
        seenTemporary.ShouldBeTrue();
    }

    /// <summary>重复的 POI 标识会让同一家店在转盘上出现两次并扭曲概率，必须整体拒绝。</summary>
    [Fact]
    public async Task Should_Reject_Duplicate_Temporary_PoiIds()
    {
        using var principal = Login();
        var candidate = TemporaryShop(3);
        await AssertInvalidDrawAsync(new CreateDrawDto
        {
            TemporaryShops = new List<TemporaryShopDto> { candidate, TemporaryShop(3) }
        });
    }

    /// <summary>停用、已删除、不存在或空标识候选必须整体拒绝。</summary>
    [Theory]
    [InlineData("disabled")]
    [InlineData("deleted")]
    [InlineData("missing")]
    [InlineData("empty-id")]
    public async Task Should_Reject_Unavailable_Permanent_Candidate(string reason)
    {
        using var principal = Login();
        var valid = await Shops.CreateAsync(NewShop("有效候选"));
        var id = Guid.NewGuid();
        if (reason is "disabled" or "deleted")
        {
            var invalid = await Shops.CreateAsync(NewShop("无效候选"));
            id = invalid.Id;
            if (reason == "disabled")
            {
                var update = NewShop(invalid.Name);
                update.IsEnabled = false;
                await Shops.UpdateAsync(id, update);
            }
            else
            {
                await Shops.DeleteAsync(id);
            }
        }
        if (reason == "empty-id") id = Guid.Empty;
        await AssertInvalidDrawAsync(new CreateDrawDto { ShopIds = new List<Guid> { valid.Id, id } });
    }

    /// <summary>临时候选中的空项和缺失兴趣点标识必须由服务自身拒绝。</summary>
    [Theory]
    [InlineData("null-item")]
    [InlineData("empty-poi")]
    [InlineData("blank-poi")]
    public async Task Should_Reject_Invalid_Temporary_Identity(string reason)
    {
        using var principal = Login();
        var candidate = TemporaryShop();
        if (reason == "empty-poi") candidate.PoiId = string.Empty;
        if (reason == "blank-poi") candidate.PoiId = " ";
        await AssertInvalidDrawAsync(new CreateDrawDto
        {
            TemporaryShops = new List<TemporaryShopDto> { reason == "null-item" ? null! : candidate }
        });
    }

    /// <summary>越界及非有限经纬度必须由服务自身拒绝。</summary>
    [Theory]
    [InlineData(180.01, 0d)]
    [InlineData(-180.01, 0d)]
    [InlineData(0d, 90.01)]
    [InlineData(0d, -90.01)]
    [InlineData(double.NaN, 0d)]
    [InlineData(double.PositiveInfinity, 0d)]
    [InlineData(0d, double.NegativeInfinity)]
    [InlineData(0d, double.NaN)]
    public async Task Should_Reject_Invalid_Temporary_Coordinates(double longitude, double latitude)
    {
        using var principal = Login();
        var candidate = TemporaryShop();
        candidate.Longitude = longitude;
        candidate.Latitude = latitude;
        await AssertInvalidDrawAsync(new CreateDrawDto { TemporaryShops = new List<TemporaryShopDto> { candidate } });
    }

    /// <summary>空坐标和合法边界坐标允许临时兴趣点持久化。</summary>
    [Theory]
    [InlineData(null, null)]
    [InlineData(-180d, -90d)]
    [InlineData(180d, 90d)]
    public async Task Should_Accept_Null_Or_Boundary_Temporary_Coordinates(double? longitude, double? latitude)
    {
        using var principal = Login();
        var candidate = TemporaryShop();
        candidate.Longitude = longitude;
        candidate.Latitude = latitude;
        var result = await Records.DrawAsync(new CreateDrawDto { TemporaryShops = new List<TemporaryShopDto> { candidate } });
        result.Shop.Longitude.ShouldBe(longitude);
        result.Shop.Latitude.ShouldBe(latitude);
        (await ReadDrawAsync(result.RecordId)).PoiId.ShouldBe(candidate.PoiId);
    }

    /// <summary>即使默认测试授权器全部放行，匿名抽取、转盘及两种查询仍被显式登录守卫拒绝。</summary>
    [Theory]
    [InlineData("draw")]
    [InlineData("enabled")]
    [InlineData("draw-query")]
    [InlineData("creation-query")]
    [InlineData("create")]
    [InlineData("batch")]
    public async Task Should_Reject_Anonymous_With_Always_Allow_Authorization(string operation)
    {
        using var principal = Anonymous();
        GetRequiredService<Microsoft.AspNetCore.Authorization.IAuthorizationService>()
            .ShouldBeOfType<AlwaysAllowAuthorizationService>();
        Func<Task> action = operation switch
        {
            "draw" => () => Records.DrawAsync(new CreateDrawDto { TemporaryShops = new List<TemporaryShopDto> { TemporaryShop() } }),
            "enabled" => () => Shops.GetEnabledListAsync(),
            "draw-query" => () => Records.GetDrawListAsync(new RecordSearchInput()),
            "creation-query" => () => Records.GetShopCreationListAsync(new RecordSearchInput()),
            "create" => () => Shops.CreateAsync(NewShop("匿名新增")),
            "batch" => () => Shops.BatchCreateAsync(new List<CreateUpdateShopDto> { NewShop("匿名批量") }),
            _ => throw new InvalidOperationException("未知测试操作")
        };
        await Should.ThrowAsync<AbpAuthorizationException>(action);
        (await DrawCountAsync()).ShouldBe(0);
        (await WithUnitOfWorkAsync(() => GetRequiredService<IRepository<ShopCreationRecord, Guid>>().GetCountAsync())).ShouldBe(0);
    }

    /// <summary>具有用户标识但无认证类型，或已认证但缺少标识的身份均不能绕过登录守卫。</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Should_Reject_Partial_Authentication(bool hasAuthenticationType)
    {
        var claims = hasAuthenticationType ? Array.Empty<Claim>() : new[] { new Claim(AbpClaimTypes.UserId, UserId.ToString()) };
        using var principal = GetRequiredService<ICurrentPrincipalAccessor>().Change(
            new ClaimsPrincipal(new ClaimsIdentity(claims, hasAuthenticationType ? "Test" : null)));
        await Should.ThrowAsync<AbpAuthorizationException>(() => Records.DrawAsync(
            new CreateDrawDto { TemporaryShops = new List<TemporaryShopDto> { TemporaryShop() } }));
        await Should.ThrowAsync<AbpAuthorizationException>(() => Shops.GetEnabledListAsync());
        await Should.ThrowAsync<AbpAuthorizationException>(() => Records.GetDrawListAsync(new RecordSearchInput()));
        await Should.ThrowAsync<AbpAuthorizationException>(() => Records.GetShopCreationListAsync(new RecordSearchInput()));
    }

    /// <summary>默认查询仅本人，其他用户及历史空用户记录均不返回，过滤器关闭也不得跨租户。</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Should_Restrict_Default_Queries_To_Current_User_And_Exact_Tenant(bool host)
    {
        using var principal = Login();
        var currentTenant = host ? (Guid?)null : Guid.NewGuid();
        var own = await SeedRecordPairAsync(currentTenant, UserId);
        await SeedRecordPairAsync(currentTenant, Guid.NewGuid());
        await SeedRecordPairAsync(currentTenant, null);
        await SeedRecordPairAsync(Guid.NewGuid(), UserId);
        if (!host) await SeedRecordPairAsync(null, UserId);
        using var tenant = Tenant.Change(currentTenant);
        using var filter = GetRequiredService<IDataFilter<IMultiTenant>>().Disable();
        (await WithUnitOfWorkAsync(() => GetRequiredService<IRepository<DrawRecord, Guid>>().GetCountAsync()))
            .ShouldBe(host ? 4 : 5);
        (await WithUnitOfWorkAsync(() => GetRequiredService<IRepository<ShopCreationRecord, Guid>>().GetCountAsync()))
            .ShouldBe(host ? 4 : 5);
        var draws = await Records.GetDrawListAsync(new RecordSearchInput());
        var creations = await Records.GetShopCreationListAsync(new RecordSearchInput());
        draws.TotalCount.ShouldBe(1);
        draws.Items.Single().Id.ShouldBe(own.DrawId);
        draws.Items.Single().UserId.ShouldBe(UserId);
        creations.TotalCount.ShouldBe(1);
        creations.Items.Single().Id.ShouldBe(own.CreationId);
        creations.Items.Single().UserId.ShouldBe(UserId);
    }
}
