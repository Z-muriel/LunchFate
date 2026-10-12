namespace LunchFate.Shops;

/// <summary>
/// 批量启用/停用店铺结果。
/// 三类计数之和等于去重后提交的门店数，便于前端如实回显"有多少没生效"。
/// </summary>
public class BatchSetShopsEnabledResult
{
    /// <summary>实际发生状态变更的门店数。</summary>
    public int UpdatedCount { get; set; }

    /// <summary>已经是目标状态、无需变更的门店数（重复提交时为幂等命中）。</summary>
    public int UnchangedCount { get; set; }

    /// <summary>未找到的门店数（已被删除、不属于当前租户或标识不存在）。</summary>
    public int NotFoundCount { get; set; }
}
