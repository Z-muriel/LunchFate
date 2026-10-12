using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LunchFate.Records;

/// <summary>
/// 登录用户的服务端抽取与记录查询契约，不提供记录修改或删除端点。
/// </summary>
public interface IRecordAppService : IApplicationService
{
    /// <summary>
    /// 校验候选（门店库 + 附近临时门店的并集）后等概率抽取一次，
    /// 记录成功保存后返回获胜快照及候选并集中的索引。
    /// </summary>
    Task<DrawResult> DrawAsync(CreateDrawDto input);

    /// <summary>
    /// 分页查询当前租户的抽取记录，默认仅本人，查看全部需要抽取记录查看权限。
    /// </summary>
    Task<PagedResultDto<DrawRecordResult>> GetDrawListAsync(RecordSearchInput input);

    /// <summary>
    /// 分页查询当前租户的店铺新增记录，默认仅本人，查看全部需要新增记录查看权限。
    /// </summary>
    Task<PagedResultDto<ShopCreationRecordResult>> GetShopCreationListAsync(RecordSearchInput input);
}
