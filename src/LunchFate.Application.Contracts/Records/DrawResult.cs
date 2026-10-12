using System;
using LunchFate.Shops;

namespace LunchFate.Records;

/// <summary>
/// 服务端已成功保存记录的抽取结果，获胜索引对应原始候选输入顺序。
/// </summary>
public class DrawResult
{
    /// <summary>已保存的抽取记录标识。</summary>
    public Guid RecordId { get; set; }

    /// <summary>获胜候选在原始输入中的零起始索引。</summary>
    public int WinnerIndex { get; set; }

    /// <summary>获胜店铺快照，临时候选的标识为 Guid.Empty。</summary>
    public EnabledShopDto Shop { get; set; } = default!;
}
