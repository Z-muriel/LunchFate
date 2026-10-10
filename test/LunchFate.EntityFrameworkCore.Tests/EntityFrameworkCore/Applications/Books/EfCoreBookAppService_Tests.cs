using LunchFate.Books;
using Xunit;

namespace LunchFate.EntityFrameworkCore.Applications.Books;

[Collection(LunchFateTestConsts.CollectionDefinitionName)]
public class EfCoreBookAppService_Tests : BookAppService_Tests<LunchFateEntityFrameworkCoreTestModule>
{

}