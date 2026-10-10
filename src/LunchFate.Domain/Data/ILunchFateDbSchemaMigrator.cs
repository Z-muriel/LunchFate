using System.Threading.Tasks;

namespace LunchFate.Data;

public interface ILunchFateDbSchemaMigrator
{
    Task MigrateAsync();
}
