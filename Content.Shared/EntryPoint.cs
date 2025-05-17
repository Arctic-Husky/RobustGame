using System.Globalization;
using Content.Shared.IoC;
using Content.Shared.Localization;
using JetBrains.Annotations;
using Robust.Shared.ContentPack;

// DEVNOTE: Games that want to be on the hub can change their namespace prefix in the "manifest.yml" file.
namespace Content.Shared;

[UsedImplicitly]
public sealed class EntryPoint : GameShared
{
    // IoC services shared between the client and the server go here...

    public override void PreInit()
    {
        SharedContentIoC.Register();
        
        IoCManager.InjectDependencies(this);

        // TODO: Document what else you might want to put here
    }

    public override void Init()
    {
        // TODO: Document what you put here
    }

    public override void PostInit()
    {
        base.PostInit();
        // DEVNOTE: You might want to put special init handlers for, say, tiles here.
        // TODO: Document what else you might want to put here
    }
}