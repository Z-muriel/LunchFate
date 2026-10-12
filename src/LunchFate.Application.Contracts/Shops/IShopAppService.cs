using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LunchFate.Shops;

/// <summary>
/// 店铺应用服务契约：增删改查、筛选查询与大转盘数据源（需求文档 §4）。
/// </summary>
public interface IShopAppService :
    ICrudAppService<
        ShopDto,
        Guid,
        ShopSearchInput,
        CreateUpdateShopDto>
{
    /// <summary>
    /// 大转盘数据源：仅返回已启用的店铺（需求文档 FR-W-01）。
    /// </summary>
    Task<ListResultDto<EnabledShopDto>> GetEnabledListAsync();

    /// <summary>
    /// 批量新增店铺；重复名称跳过，其余项目继续处理。
    /// 空集合或超过单批上限时抛出 <c>UserFriendlyException</c>，不静默截断入参。
    /// </summary>
    /// <param name="inputs">店铺新增入参</param>
    /// <returns>批量处理统计</returns>
    Task<BatchCreateShopsResult> BatchCreateAsync(List<CreateUpdateShopDto> inputs);

    /// <summary>
    /// 批量启用/停用店铺（需求文档 FR-S-06）。
    /// 只更新状态确实需要变更的门店，整体单事务；重复提交同一批门店为幂等操作。
    /// 空集合、含空标识或超过单批上限时抛出 <c>UserFriendlyException</c>，不静默截断入参。
    /// </summary>
    /// <param name="input">门店标识集合与目标启用状态</param>
    /// <returns>批量处理统计</returns>
    Task<BatchSetShopsEnabledResult> BatchSetEnabledAsync(BatchSetShopsEnabledInput input);
}
