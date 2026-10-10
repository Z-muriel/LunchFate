using LunchFate.Localization;
using Volo.Abp.Application.Services;

namespace LunchFate;

/* Inherit your application services from this class.
 */
public abstract class LunchFateAppService : ApplicationService
{
    protected LunchFateAppService()
    {
        LocalizationResource = typeof(LunchFateResource);
    }
}
