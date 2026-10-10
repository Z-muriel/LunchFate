using System;
using System.Linq;
using System.Threading.Tasks;
using LunchFate.Localization;
using Microsoft.Extensions.Localization;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;

namespace LunchFate.Shops;

/// <summary>
/// 店铺领域服务：承载名称唯一性等领域不变量（需求文档 §3.2、FR-S-02）。
/// </summary>
public class ShopManager : DomainService
{
    private readonly IRepository<Shop, Guid> _shopRepository;
    private readonly IStringLocalizer<LunchFateResource> _localizer;

    public ShopManager(
        IRepository<Shop, Guid> shopRepository,
        IStringLocalizer<LunchFateResource> localizer)
    {
        _shopRepository = shopRepository;
        _localizer = localizer;
    }

    /// <summary>
    /// 校验店铺名称在未删除数据中唯一（更新场景可排除自身），并通过 Trim 规范化名称，
    /// 避免首尾空格导致同名校验被绕过。
    /// </summary>
    /// <param name="name">待校验的店铺名称</param>
    /// <param name="excludingId">更新场景下排除的店铺 Id</param>
    /// <returns>规范化（Trim）后的店铺名称</returns>
    /// <exception cref="UserFriendlyException">同名（未删除）店铺已存在时抛出</exception>
    public virtual async Task<string> EnsureNameIsUniqueAsync(string name, Guid? excludingId = null)
    {
        // 1. 规范化名称
        var normalizedName = name?.Trim() ?? string.Empty;

        // 2. 查询同名且未删除的店铺（软删除过滤由仓储全局过滤器自动应用）
        var query = await _shopRepository.GetQueryableAsync();
        query = query.Where(s => s.Name == normalizedName);

        if (excludingId.HasValue)
        {
            var id = excludingId.Value;
            query = query.Where(s => s.Id != id);
        }

        // 3. 已存在则抛出用户可读的业务异常
        if (await AsyncExecuter.AnyAsync(query))
        {
            throw new UserFriendlyException(_localizer["ShopNameDuplicate", normalizedName].Value);
        }

        return normalizedName;
    }
}
