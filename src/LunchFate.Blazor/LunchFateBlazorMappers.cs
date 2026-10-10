using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;
using LunchFate.Authors;
using LunchFate.Books;
namespace LunchFate.Blazor;
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class LunchFateBlazorMappers : MapperBase<BookDto, CreateUpdateBookDto>
{
    public override partial CreateUpdateBookDto Map(BookDto source);
    public override partial void Map(BookDto source, CreateUpdateBookDto destination);
}
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class LunchFateAuthorDtoToCreateUpdateAuthorDtoMapper : MapperBase<AuthorDto, CreateUpdateAuthorDto>
{
    public override partial CreateUpdateAuthorDto Map(AuthorDto source);
    public override partial void Map(AuthorDto source, CreateUpdateAuthorDto destination);
}
