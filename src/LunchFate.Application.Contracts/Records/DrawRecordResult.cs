using System;
using LunchFate.Shops;
using Volo.Abp.Application.Dtos;

namespace LunchFate.Records;

/// <summary>
/// 抽取记录只读结果，展示记录时的操作者与店铺快照。
/// </summary>
public class DrawRecordResult : EntityDto<Guid>
{
    /// <summary>操作者标识，历史记录可空。</summary>
    public Guid? UserId { get; set; }

    /// <summary>操作者用户名快照。</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>永久店铺标识，临时候选为空。</summary>
    public Guid? ShopId { get; set; }

    /// <summary>店铺名称快照。</summary>
    public string ShopName { get; set; } = string.Empty;

    /// <summary>地址快照，可空。</summary>
    public string? Address { get; set; }

    /// <summary>店铺分类快照。</summary>
    public ShopCategory Category { get; set; }

    /// <summary>人均价格快照。</summary>
    public decimal AveragePrice { get; set; }

    /// <summary>服务端记录时间。</summary>
    public DateTime RecordTime { get; set; }

    /// <summary>是否为声明的临时地图候选。</summary>
    public bool IsTemporary { get; set; }

    /// <summary>地图兴趣点标识，可空。</summary>
    public string? PoiId { get; set; }
}
