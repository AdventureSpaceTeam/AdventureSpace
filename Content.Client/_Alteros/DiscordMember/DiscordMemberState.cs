using System.Threading;
using Content.Client._Alteros.DiscordMember;
using Content.Shared.DiscordMember;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Shared.Network;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Client.DiscordMember;

public sealed class DiscordMemberState : State
{
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly IClientNetManager _netManager = default!;

    private DiscordMemberGui? _gui;
    private readonly CancellationTokenSource _checkTimerCancel = new();

    protected override void Startup()
    {
        _gui = new DiscordMemberGui();
        _userInterfaceManager.StateRoot.AddChild(_gui);

        Timer.SpawnRepeating(TimeSpan.FromSeconds(5), () =>
        {
            _netManager.ClientSendMessage(new MsgDiscordMemberCheck());
        }, _checkTimerCancel.Token);
    }

    protected override void Shutdown()
    {
        _checkTimerCancel.Cancel();
        _gui!.Dispose();
    }
}
