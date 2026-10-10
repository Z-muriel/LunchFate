using Volo.Abp.Ui.Branding;
using Volo.Abp.DependencyInjection;
using Microsoft.Extensions.Localization;
using LunchFate.Localization;

namespace LunchFate.Blazor.Client;

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
