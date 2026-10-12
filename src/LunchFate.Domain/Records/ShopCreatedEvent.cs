using System;
using LunchFate.Shops;

namespace LunchFate.Records;

/// <summary>
/// 店铺成功插入后立即发布的本地事件，携带服务端身份、时间与店铺快照。
/// </summary>
public class ShopCreatedEvent
{
    /// <summary>操作者所属租户，宿主为 null。</summary>
    public Guid? TenantId { get; }

    /// <summary>服务端获取的操作者标识，可空。</summary>
    public Guid? UserId { get; }

    /// <summary>服务端获取的操作者用户名。</summary>
    public string UserName { get; }

    /// <summary>已保存店铺的标识。</summary>
    public Guid ShopId { get; }

    /// <summary>已保存店铺的名称快照。</summary>
    public string Name { get; }

    /// <summary>已保存店铺的地址快照。</summary>
    public string? Address { get; }

    /// <summary>已保存店铺的分类快照。</summary>
    public ShopCategory Category { get; }

    /// <summary>已保存店铺的人均价格快照。</summary>
    public decimal AveragePrice { get; }

    /// <summary>服务端新增时间。</summary>
    public DateTime RecordTime { get; }

    /// <summary>
    /// 根据当前服务端操作者和已插入店铺创建本地新增事件。
    /// </summary>
    /// <param name="tenantId">当前租户，宿主为 null。</param>
    /// <param name="userId">添加人标识。</param>
    /// <param name="userName">添加人用户名。</param>
    /// <param name="shopId">已入库门店标识。</param>
    /// <param name="name">门店名称快照。</param>
    /// <param name="address">地址快照。</param>
    /// <param name="category">分类快照。</param>
    /// <param name="averagePrice">人均价格快照。</param>
    /// <param name="recordTime">服务端新增时间。</param>
    public ShopCreatedEvent(
        Guid? tenantId,
        Guid? userId,
        string userName,
        Guid shopId,
        string name,
        string? address,
        ShopCategory category,
        decimal averagePrice,
        DateTime recordTime)
    {
        TenantId = tenantId;
        UserId = userId;
        UserName = userName;
        ShopId = shopId;
        Name = name;
        Address = address;
        Category = category;
        AveragePrice = averagePrice;
        RecordTime = recordTime;
    }
}
