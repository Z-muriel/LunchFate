using Xunit;

namespace LunchFate.EntityFrameworkCore;

[CollectionDefinition(LunchFateTestConsts.CollectionDefinitionName)]
public class LunchFateEntityFrameworkCoreCollection : ICollectionFixture<LunchFateEntityFrameworkCoreFixture>
{

}
