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
}
