using LunchFate.Localization;
using Volo.Abp.AspNetCore.Components;

namespace LunchFate.Blazor.Client;

public abstract class LunchFateComponentBase : AbpComponentBase
{
    protected LunchFateComponentBase()
    {
        LocalizationResource = typeof(LunchFateResource);
    }
}
