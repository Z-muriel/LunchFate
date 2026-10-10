using LunchFate.Shops;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace LunchFate;

/// <summary>
/// 店铺 → 店铺 DTO 的对象映射。
/// 说明：<see cref="CreateUpdateShopDto"/> → <see cref="Shop"/> 的映射在
/// <c>ShopAppService</c> 中手工完成（避免为审计字段生成无意义的映射代码）；
/// 大转盘的 <c>EnabledShopDto</c> 在查询中以数据库端 <c>Select</c> 投影，无需映射类。
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class ShopToShopDtoMapper : MapperBase<Shop, ShopDto>
{
    public override partial ShopDto Map(Shop source);

    public override partial void Map(Shop source, ShopDto destination);
}
