using System;
using LunchFate.Shops;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.MultiTenancy;

namespace LunchFate.Records;

/// <summary>
/// 店铺新增记录聚合根，独立保存新增时的快照，不因店铺删除或改名而改变。
/// </summary>
public class ShopCreationRecord : AggregateRoot<Guid>, IMultiTenant
{
    /// <summary>所属租户，宿主记录为 null。</summary>
    public Guid? TenantId { get; protected set; }

    /// <summary>操作者标识，历史无操作者记录可空。</summary>
    public Guid? UserId { get; protected set; }

    /// <summary>操作者用户名快照，最大长度为 256。</summary>
    public string UserName { get; protected set; } = string.Empty;

    /// <summary>店铺标识，可空；不建立跨聚合外键。</summary>
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

    /// <summary>
    /// 供持久化框架还原店铺新增记录。
    /// </summary>
    protected ShopCreationRecord()
    {
    }

    /// <summary>
    /// 根据服务端新增事件的身份、时间及店铺快照创建不可变记录。
    /// </summary>
    /// <param name="id">服务端记录标识。</param>
    /// <param name="tenantId">当前租户，宿主为 null。</param>
    /// <param name="userId">添加人标识。</param>
    /// <param name="userName">添加人用户名快照。</param>
    /// <param name="shopId">门店标识，不建立外键。</param>
    /// <param name="shopName">门店名称快照。</param>
    /// <param name="address">地址快照。</param>
    /// <param name="category">分类快照。</param>
    /// <param name="averagePrice">人均价格快照。</param>
    /// <param name="recordTime">服务端添加时间。</param>
    public ShopCreationRecord(
        Guid id,
        Guid? tenantId,
        Guid? userId,
        string userName,
        Guid? shopId,
        string shopName,
        string? address,
        ShopCategory category,
        decimal averagePrice,
        DateTime recordTime)
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
    }
}
