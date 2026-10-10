using LunchFate.Shops;
using Xunit;

namespace LunchFate.EntityFrameworkCore.Applications.Shops;

/// <summary>
/// 在内存 SQLite 上运行的店铺应用服务测试（具体驱动类）。
/// </summary>
[Collection(LunchFateTestConsts.CollectionDefinitionName)]
public class EfCoreShopAppServiceTests : ShopAppServiceTests<LunchFateEntityFrameworkCoreTestModule>
{
}
