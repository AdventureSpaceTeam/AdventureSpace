using System.Threading;
using Content.Shared.DiscordAuth;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Shared.Network;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Client.DiscordAuth;

public sealed class DiscordAuthState : State
{
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly IClientNetManager _netManager = default!;

    private Alteros.DiscordAuth.DiscordAuthGui? _gui;
    private readonly CancellationTokenSource _checkTimerCancel = new();

    protected override void Startup()
    {
        _gui = new Alteros.DiscordAuth.DiscordAuthGui();
        _userInterfaceManager.StateRoot.AddChild(_gui);

        Timer.SpawnRepeating(TimeSpan.FromSeconds(5), () =>
        {
            _netManager.ClientSendMessage(new MsgDiscordAuthCheck());
        }, _checkTimerCancel.Token);
    }

    protected override void Shutdown()
    {
        _checkTimerCancel.Cancel();
        _gui!.Dispose();
    }
}
