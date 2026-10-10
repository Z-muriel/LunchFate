using Volo.Abp.Settings;

namespace LunchFate.Settings;

public class LunchFateSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(LunchFateSettings.MySetting1));
    }
}
