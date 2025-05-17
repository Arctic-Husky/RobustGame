using Content.Shared.Localization;

namespace Content.Shared.IoC;

internal static class SharedContentIoC
{
    public static void Register()
    {
        IoCManager.Register<ContentLocalizationManager, ContentLocalizationManager>();
    }
    
}