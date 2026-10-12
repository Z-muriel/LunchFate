using System.Collections.Generic;

namespace LunchFate.Shops;

/// <summary>
/// 批量新增店铺结果。
/// </summary>
public class BatchCreateShopsResult
{
    /// <summary>成功新增数量。</summary>
    public int CreatedCount { get; set; }

    /// <summary>因同名已存在而跳过的数量。</summary>
    public int SkippedCount { get; set; }

    /// <summary>未成功处理的名称及原因。</summary>
    public List<string> Errors { get; set; } = [];
}
