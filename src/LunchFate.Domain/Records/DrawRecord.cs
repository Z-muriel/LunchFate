using System;
using LunchFate.Shops;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.MultiTenancy;

namespace LunchFate.Records;

/// <summary>
/// 抽取记录聚合根，保存抽取时的操作者与店铺快照，不关联其他聚合的生命周期。
/// </summary>
public class DrawRecord : AggregateRoot<Guid>, IMultiTenant
{
    /// <summary>所属租户，宿主记录为 null。</summary>
    public Guid? TenantId { get; protected set; }

    /// <summary>操作者标识，历史无操作者记录可空。</summary>
    public Guid? UserId { get; protected set; }

    /// <summary>操作者用户名快照，最大长度为 256。</summary>
    public string UserName { get; protected set; } = string.Empty;

    /// <summary>永久店铺标识，临时候选为空；不建立跨聚合外键。</summary>
    public Guid? ShopId { get; protected set; }

    /// <summary>店铺名称快照，最大长度为 256。</summary>
    public string ShopName { get; protected set; } = string.Empty;

    /// <summary>地址快照，最大长度为 256，可空。</summary>
    public string? Address { get; protected set; }

    /// <summary>店铺分类快照。</summary>
    public ShopCategory Category { get; protected set; }

    /// <summary>人均价格快照。</summary>
    public decimal AveragePrice { get; protected set; }

    /// <summary>服务端记录时间。</summary>
    public DateTime RecordTime { get; protected set; }

    /// <summary>是否为声明的临时地图候选。</summary>
    public bool IsTemporary { get; protected set; }

    /// <summary>临时候选的地图兴趣点标识，最大长度为 128，可空。</summary>
    public string? PoiId { get; protected set; }

    /// <summary>
    /// 供持久化框架还原抽取记录。
    /// </summary>
    protected DrawRecord()
    {
    }

    /// <summary>
    /// 使用服务端生成的标识、身份、时间及已验证的候选快照创建不可变抽取记录。
    /// </summary>
    /// <param name="id">服务端记录标识。</param>
    /// <param name="tenantId">当前租户，宿主为 null。</param>
    /// <param name="userId">操作者标识。</param>
    /// <param name="userName">操作者用户名快照。</param>
    /// <param name="shopId">永久门店标识，临时门店为空。</param>
    /// <param name="shopName">门店名称快照。</param>
    /// <param name="address">地址快照。</param>
    /// <param name="category">分类快照。</param>
    /// <param name="averagePrice">人均价格快照。</param>
    /// <param name="recordTime">服务端抽取时间。</param>
    /// <param name="isTemporary">是否为临时附近门店。</param>
    /// <param name="poiId">临时兴趣点原始标识。</param>
    public DrawRecord(
        Guid id,
        Guid? tenantId,
        Guid? userId,
        string userName,
        Guid? shopId,
        string shopName,
        string? address,
        ShopCategory category,
        decimal averagePrice,
        DateTime recordTime,
        bool isTemporary,
        string? poiId)
        : base(id)
    {
        TenantId = tenantId;
        UserId = userId;
        Check.NotNull(userName, nameof(userName));
        Check.Length(userName, nameof(userName), 256);
        UserName = userName;
        ShopId = shopId;
        Check.NotNullOrWhiteSpace(shopName, nameof(shopName));
        Check.Length(shopName, nameof(shopName), 256);
        ShopName = shopName;
        Address = Check.Length(address, nameof(address), 256);
        Category = category;
        AveragePrice = averagePrice;
        RecordTime = recordTime;
        IsTemporary = isTemporary;
        PoiId = Check.Length(poiId, nameof(poiId), 128);
    }
}
