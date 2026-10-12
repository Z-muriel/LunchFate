using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LunchFate.Records;

/// <summary>
/// 服务端抽取入参；门店库候选与附近临时候选可同时提供，转盘取两者并集。
/// 候选顺序约定：先按 <see cref="ShopIds"/> 顺序，再按 <see cref="TemporaryShops"/> 顺序，
/// 客户端扇区必须按同一顺序排列，返回的 WinnerIndex 才能直接对应到扇区。
/// </summary>
public class CreateDrawDto
{
    /// <summary>按抽取展示顺序提供的永久店铺标识，不允许重复或空标识；可为空集合。</summary>
    [Required]
    public List<Guid> ShopIds { get; set; } = new();

    /// <summary>按抽取展示顺序声明的临时地图候选，不写入门店库；可为空集合，POI 标识不得重复。</summary>
    [Required]
    public List<TemporaryShopDto> TemporaryShops { get; set; } = new();
}
