using LunchFate.Shops;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace LunchFate;

/// <summary>
/// 店铺对象映射。
/// 说明：<see cref="CreateUpdateShopDto"/> → <see cref="Shop"/> 的映射在
/// <c>ShopAppService</c> 中手工完成（避免为审计字段生成无意义的映射代码）。
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class ShopToShopDtoMapper : MapperBase<Shop, ShopDto>
{
    public override partial ShopDto Map(Shop source);

    public override partial void Map(Shop source, ShopDto destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
/// <summary>
/// 店铺 → 大转盘轻量 DTO 的映射。
/// </summary>
public partial class ShopToEnabledShopDtoMapper : MapperBase<Shop, EnabledShopDto>
{
    public override partial EnabledShopDto Map(Shop source);

    public override partial void Map(Shop source, EnabledShopDto destination);
}
