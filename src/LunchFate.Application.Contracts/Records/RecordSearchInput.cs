using System;
using Volo.Abp.Application.Dtos;

namespace LunchFate.Records;

/// <summary>
/// 记录分页查询条件；默认仅查询当前用户，查看租户内全部记录需要相应权限。
/// </summary>
public class RecordSearchInput : PagedAndSortedResultRequestDto
{
    /// <summary>匹配店铺名称、用户名或地址的关键字。</summary>
    public string? Keyword { get; set; }

    /// <summary>记录时间下界，包含边界。</summary>
    public DateTime? FromTime { get; set; }

    /// <summary>记录时间上界，包含边界。</summary>
    public DateTime? ToTime { get; set; }

    /// <summary>是否查询当前租户内全部用户的记录，必须验证对应查看权限。</summary>
    public bool IncludeAll { get; set; }
}
