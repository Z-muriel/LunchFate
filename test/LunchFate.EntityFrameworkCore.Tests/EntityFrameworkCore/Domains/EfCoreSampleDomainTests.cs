using LunchFate.Samples;
using Xunit;

namespace LunchFate.EntityFrameworkCore.Domains;

[Collection(LunchFateTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<LunchFateEntityFrameworkCoreTestModule>
{

}
