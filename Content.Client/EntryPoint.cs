using Content.Client.IoC;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.State;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.Localization;

// DEVNOTE: Games that want to be on the hub can change their namespace prefix in the "manifest.yml" file.
namespace Content.Client;

[UsedImplicitly]
public sealed class EntryPoint : GameClient
{
    [Dependency] private readonly ContentLocalizationManager _contentLoc = default!;
    public override void Init()
    {
        ClientContentIoC.Register();
        
        var factory = IoCManager.Resolve<IComponentFactory>();
        var prototypes = IoCManager.Resolve<IPrototypeManager>();

        factory.DoAutoRegistrations();

        foreach (var ignoreName in IgnoredComponents.List)
        {
            factory.RegisterIgnore(ignoreName);
        }

        foreach (var ignoreName in IgnoredPrototypes.List)
        {
            prototypes.RegisterIgnore(ignoreName);
        }

        IoCManager.BuildGraph();
        IoCManager.InjectDependencies(this);

        factory.GenerateNetIds();

        // DEVNOTE: This is generally where you'll be setting up the IoCManager further.
        
        _contentLoc.Initialize();
    }

    public override void PostInit()
    {
        base.PostInit();
            
        // DEVNOTE: The line below will disable lighting, so you can see in-game sprites without the need for lights
        IoCManager.Resolve<ILightManager>().Enabled = false;

        var stateManager = IoCManager.Resolve<IStateManager>();

        // DEVNOTE: It's recommended to look at how this works! It's for debug purposes and you probably want something prettier for the final game.
        // Additionally, state manager is the primary way you'll be changing between UIScreen instances.
        // stateManager.RequestStateChange<DebugBuiltinConnectionScreenState>();
        stateManager.RequestStateChange<MainMenu.MainMenu>();

        // DEVNOTE: Further setup...
        //var client = IoCManager.Resolve<IBaseClient>();

        // Optionally, singleplayer also works!
        // client.StartSinglePlayer();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
            
        // DEVNOTE: You might want to do a proper shutdown here.
    }

    public override void Update(ModUpdateLevel level, FrameEventArgs frameEventArgs)
    {
        base.Update(level, frameEventArgs);
        // DEVNOTE: Game update loop goes here. Usually you'll want some independent GameTicker.
    }
}