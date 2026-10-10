using LunchFate.Samples;
using Xunit;

namespace LunchFate.EntityFrameworkCore.Applications;

[Collection(LunchFateTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<LunchFateEntityFrameworkCoreTestModule>
{

}
