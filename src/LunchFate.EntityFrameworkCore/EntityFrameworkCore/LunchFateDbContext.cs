using Microsoft.EntityFrameworkCore;
using LunchFate.Records;
using LunchFate.Shops;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.BlobStoring.Database.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.TenantManagement;
using Volo.Abp.TenantManagement.EntityFrameworkCore;

namespace LunchFate.EntityFrameworkCore;

/// <summary>
/// 午餐应用数据库上下文，统一配置模块实体、店铺及独立记录快照表。
/// </summary>
[ReplaceDbContext(typeof(IIdentityDbContext))]
[ReplaceDbContext(typeof(ITenantManagementDbContext))]
[ConnectionStringName("Default")]
public class LunchFateDbContext :
    AbpDbContext<LunchFateDbContext>,
    ITenantManagementDbContext,
    IIdentityDbContext
{
    /// <summary>可维护的店铺聚合。</summary>
    public DbSet<Shop> Shops { get; set; }

    /// <summary>独立的抽取记录快照。</summary>
    public DbSet<DrawRecord> DrawRecords { get; set; }

    /// <summary>独立的店铺新增记录快照。</summary>
    public DbSet<ShopCreationRecord> ShopCreationRecords { get; set; }

    #region 模块实体

    // 替换身份与租户模块的上下文，使模块仓储共用本上下文及工作单元。

    // 身份管理
    public DbSet<IdentityUser> Users { get; set; }
    public DbSet<IdentityRole> Roles { get; set; }
    public DbSet<IdentityClaimType> ClaimTypes { get; set; }
    public DbSet<OrganizationUnit> OrganizationUnits { get; set; }
    public DbSet<IdentitySecurityLog> SecurityLogs { get; set; }
    public DbSet<IdentityLinkUser> LinkUsers { get; set; }
    public DbSet<IdentityUserDelegation> UserDelegations { get; set; }
    public DbSet<IdentitySession> Sessions { get; set; }

    // 租户管理
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantConnectionString> TenantConnectionStrings { get; set; }

    #endregion

    /// <summary>
    /// 使用依赖注入提供的选项初始化应用数据库上下文。
    /// </summary>
    public LunchFateDbContext(DbContextOptions<LunchFateDbContext> options)
        : base(options)
    {

    }

    /// <summary>
    /// 配置模块模型及业务表，记录按独立聚合映射，不建立门店或用户外键。
    /// </summary>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // 配置依赖模块的实体模型。

        builder.ConfigurePermissionManagement();
        builder.ConfigureSettingManagement();
        builder.ConfigureBackgroundJobs();
        builder.ConfigureAuditLogging();
        builder.ConfigureFeatureManagement();
        builder.ConfigureIdentity();
        builder.ConfigureOpenIddict();
        builder.ConfigureTenantManagement();
        builder.ConfigureBlobStoring();

        // 配置店铺及独立记录快照表。

        builder.Entity<Shop>(b =>
        {
            b.ToTable(LunchFateConsts.DbTablePrefix + "Shops", LunchFateConsts.DbSchema);
            b.ConfigureByConvention(); // 按 ABP 约定配置基类属性。
            b.Property(x => x.Name).IsRequired().HasMaxLength(ShopConsts.MaxNameLength);
            b.Property(x => x.Address).HasMaxLength(ShopConsts.MaxAddressLength);
            b.Property(x => x.Phone).HasMaxLength(ShopConsts.MaxPhoneLength);
            b.Property(x => x.AveragePrice).HasPrecision(18, 2);
            b.Property(x => x.Rating).HasPrecision(3, 1);
            b.Property(x => x.Tags).HasMaxLength(ShopConsts.MaxTagsLength);
            b.Property(x => x.Remark).HasMaxLength(ShopConsts.MaxRemarkLength);
            // FR-M-01：经度/纬度可空，由高德地理编码服务自动填充
            b.Property(x => x.Longitude).HasPrecision(10, 6);
            b.Property(x => x.Latitude).HasPrecision(10, 6);
            //需求文档 §3.2：同名（未删除）唯一；用过滤唯一索引在库级兜底，
            //过滤条件允许软删除数据保留同名历史（FR-S-04）
            b.HasIndex(x => x.Name).IsUnique().HasFilter("IsDeleted = 0");
        });

        builder.Entity<DrawRecord>(b =>
        {
            b.ToTable(LunchFateConsts.DbTablePrefix + "DrawRecords", LunchFateConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.UserName).IsRequired().HasMaxLength(256);
            b.Property(x => x.ShopName).IsRequired().HasMaxLength(256);
            b.Property(x => x.Address).HasMaxLength(256);
            b.Property(x => x.AveragePrice).HasPrecision(18, 2);
            b.Property(x => x.RecordTime).IsRequired();
            b.Property(x => x.PoiId).HasMaxLength(128);
            b.HasIndex(x => new { x.TenantId, x.UserId, x.RecordTime });
            b.HasIndex(x => new { x.TenantId, x.RecordTime });
        });

        builder.Entity<ShopCreationRecord>(b =>
        {
            b.ToTable(LunchFateConsts.DbTablePrefix + "ShopCreationRecords", LunchFateConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.UserName).IsRequired().HasMaxLength(256);
            b.Property(x => x.ShopName).IsRequired().HasMaxLength(256);
            b.Property(x => x.Address).HasMaxLength(256);
            b.Property(x => x.AveragePrice).HasPrecision(18, 2);
            b.Property(x => x.RecordTime).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.UserId, x.RecordTime });
            b.HasIndex(x => new { x.TenantId, x.RecordTime });
        });
    }
}
