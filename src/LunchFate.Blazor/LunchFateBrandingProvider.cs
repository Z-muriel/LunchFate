using Microsoft.Extensions.Localization;
using LunchFate.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace LunchFate.Blazor;

[Dependency(ReplaceServices = true)]
public class LunchFateBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<LunchFateResource> _localizer;

    public LunchFateBrandingProvider(IStringLocalizer<LunchFateResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
