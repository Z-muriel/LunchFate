using LunchFate.Localization;
using Volo.Abp.AspNetCore.Components;

namespace LunchFate.Blazor;

public abstract class LunchFateComponentBase : AbpComponentBase
{
    protected LunchFateComponentBase()
    {
        LocalizationResource = typeof(LunchFateResource);
    }
}
