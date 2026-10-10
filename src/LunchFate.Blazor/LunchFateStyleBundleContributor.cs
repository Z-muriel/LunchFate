using Volo.Abp.AspNetCore.Mvc.UI.Bundling;

namespace LunchFate.Blazor;

public class LunchFateStyleBundleContributor : BundleContributor
{
    public override void ConfigureBundle(BundleConfigurationContext context)
    {
        context.Files.Add(new BundleFile("main.css", true));
    }
}
