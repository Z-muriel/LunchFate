using System;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus;
using Volo.Abp.Guids;
using Volo.Abp.Uow;

namespace LunchFate.Records;

/// <summary>
/// 在门店新增的同一工作单元内写入独立记录，不直接操作门店聚合。
/// </summary>
public class ShopCreatedEventHandler : ILocalEventHandler<ShopCreatedEvent>, ITransientDependency
{
    /// <summary>本聚合的添加记录仓储。</summary>
    private readonly IRepository<ShopCreationRecord, Guid> _repository;
    /// <summary>服务端生成记录标识。</summary>
    private readonly IGuidGenerator _guidGenerator;

    /// <summary>
    /// 初始化门店新增记录处理器及服务端标识生成器。
    /// </summary>
    public ShopCreatedEventHandler(
        IRepository<ShopCreationRecord, Guid> repository,
        IGuidGenerator guidGenerator)
    {
        _repository = repository;
        _guidGenerator = guidGenerator;
    }

    /// <summary>
    /// 将事件中的操作者、租户、店铺快照和时间原样写入记录，保存失败向发布者传播。
    /// </summary>
    [UnitOfWork]
    public virtual async Task HandleEventAsync(ShopCreatedEvent eventData)
    {
        var record = new ShopCreationRecord(
            _guidGenerator.Create(),
            eventData.TenantId,
            eventData.UserId,
            eventData.UserName,
            eventData.ShopId,
            eventData.Name,
            eventData.Address,
            eventData.Category,
            eventData.AveragePrice,
            eventData.RecordTime);

        await _repository.InsertAsync(record, autoSave: true);
    }
}
