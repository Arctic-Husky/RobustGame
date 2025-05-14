using Content.Client.MainMenu.UI;
using Robust.Client.UserInterface;

namespace Content.Client.MainMenu;

public sealed class MainMenu : Robust.Client.State.State
{
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    
    private ISawmill _sawmill = default!;
    private MainMenuScreen _mainMenuScreen = default!;
    protected override void Startup()
    {
        _sawmill = _logManager.GetSawmill("mainmenu");
        _sawmill.Debug("Startup");
        
        _mainMenuScreen = new MainMenuScreen();
        
        _userInterfaceManager.StateRoot.AddChild(_mainMenuScreen);
    }

    protected override void Shutdown()
    {
        _sawmill.Debug("Shutdown");
        
        _mainMenuScreen.RemoveAllChildren();
    }
}