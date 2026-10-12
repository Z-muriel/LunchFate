using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LunchFate.Shops;

/// <summary>
/// 批量启用/停用店铺入参（需求文档 FR-S-06）。
/// 门店标识按跨页累计提交，服务端只处理确实存在（未删除且属于当前租户）的门店。
/// </summary>
public class BatchSetShopsEnabledInput
{
    /// <summary>待操作的门店标识集合；不允许为空集合、含空标识或超过单批上限。</summary>
    [Required]
    public List<Guid> Ids { get; set; } = new();

    /// <summary>目标启用状态：true 为启用，false 为停用。</summary>
    public bool IsEnabled { get; set; }
}
