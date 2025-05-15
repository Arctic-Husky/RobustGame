using System.Text.RegularExpressions;
using Content.Client.MainMenu.UI;
using Robust.Client;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared;
using Robust.Shared.AuthLib;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Client.MainMenu;

public sealed class MainMenu : Robust.Client.State.State
{
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    
    // Utilizado para manipular configurações salvas em máquina
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    
    [Dependency] private readonly IBaseClient _client = default!;
    [Dependency] private readonly IClientNetManager _netManager = default!;
    
    private ISawmill _sawmill = default!;
    private MainMenuScreen _mainMenuScreen = default!;
    
    private bool _isConnecting;
    
    private static readonly Regex Ipv6Regex = new(@"\[(.*:.*:.*)](?::(\d+))?");
    
    protected override void Startup()
    {
        _sawmill = _logManager.GetSawmill("mainmenu");
        _sawmill.Debug("Startup");
        
        _mainMenuScreen = new MainMenuScreen(configMan: _configurationManager);
        
        _userInterfaceManager.StateRoot.AddChild(_mainMenuScreen);
        
        _mainMenuScreen.DirectConnectButton.OnPressed += DirectConnectButtonPressed;
        _mainMenuScreen.AddressBox.OnTextEntered += AddressBoxEntered;
        
        _client.RunLevelChanged += RunLevelChanged;
    }
    
    protected override void Shutdown()
    {
        _sawmill.Debug("Shutdown");
        
        _client.RunLevelChanged -= RunLevelChanged;
        _netManager.ConnectFailed -= _onConnectFailed;
        
        _mainMenuScreen.RemoveAllChildren();
    }
    
    private void AddressBoxEntered(LineEdit.LineEditEventArgs args)
    {
        if (_isConnecting)
        {
            return;
        }
        
        TryConnect(args.Text);
    }

    private void DirectConnectButtonPressed(BaseButton.ButtonEventArgs args)
    {
        var input = _mainMenuScreen.AddressBox.Text;
        TryConnect(input);
    }
    
    private void TryConnect(string address)
    {
        var inputName = _mainMenuScreen.UsernameBox.Text.Trim();
        if (!UsernameHelpers.IsNameValid(inputName, out var reason))
        {
            var invalidReason = Loc.GetString(reason.ToText());
            _userInterfaceManager.Popup(
                Loc.GetString("main-menu-invalid-username-with-reason", ("invalidReason", invalidReason)),
                Loc.GetString("main-menu-invalid-username"));
            return;
        }

        var configName = _configurationManager.GetCVar(CVars.PlayerName);
        if (_mainMenuScreen.UsernameBox.Text != configName)
        {
            _configurationManager.SetCVar(CVars.PlayerName, inputName);
            _configurationManager.SaveToFile();
        }

        _setConnectingState(true);
        _netManager.ConnectFailed += _onConnectFailed;
        try
        {
            ParseAddress(address, out var ip, out var port);
            _client.ConnectToServer(ip, port);
        }
        catch (ArgumentException e)
        {
            _userInterfaceManager.Popup($"Unable to connect: {e.Message}", "Connection error.");
            _sawmill.Warning(e.ToString());
            _netManager.ConnectFailed -= _onConnectFailed;
            _setConnectingState(false);
        }
    }
    
    private void RunLevelChanged(object? obj, RunLevelChangedEventArgs args)
    {
        switch (args.NewLevel)
        {
            case ClientRunLevel.Connecting:
                _setConnectingState(true);
                break;
            case ClientRunLevel.Initialize:
                _setConnectingState(false);
                _netManager.ConnectFailed -= _onConnectFailed;
                break;
        }
    }
    
    private void _setConnectingState(bool state)
    {
        _isConnecting = state;
        _mainMenuScreen.DirectConnectButton.Disabled = state;
    }
    
    private void _onConnectFailed(object? _, NetConnectFailArgs args)
    {
        _userInterfaceManager.Popup(Loc.GetString("main-menu-failed-to-connect",("reason", args.Reason)));
        _netManager.ConnectFailed -= _onConnectFailed;
        _setConnectingState(false);
    }
    
    private void ParseAddress(string address, out string ip, out ushort port)
    {
        var match6 = Ipv6Regex.Match(address);
        if (match6 != Match.Empty)
        {
            ip = match6.Groups[1].Value;
            if (!match6.Groups[2].Success)
            {
                port = _client.DefaultPort;
            }
            else if (!ushort.TryParse(match6.Groups[2].Value, out port))
            {
                throw new ArgumentException("Not a valid port.");
            }

            return;
        }

        // See if the IP includes a port.
        var split = address.Split(':');
        ip = address;
        port = _client.DefaultPort;
        if (split.Length > 2)
        {
            throw new ArgumentException("Not a valid Address.");
        }

        // IP:port format.
        if (split.Length == 2)
        {
            ip = split[0];
            if (!ushort.TryParse(split[1], out port))
            {
                throw new ArgumentException("Not a valid port.");
            }
        }
    }
}