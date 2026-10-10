using LunchFate.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace LunchFate.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class LunchFateController : AbpControllerBase
{
    protected LunchFateController()
    {
        LocalizationResource = typeof(LunchFateResource);
    }
}
