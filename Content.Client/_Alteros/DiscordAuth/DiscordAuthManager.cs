using System.IO;
using Content.Client.DiscordMember;
using Content.Shared.DiscordAuth;
using Content.Shared.DiscordMember;
using Robust.Client.Graphics;
using Robust.Client.State;
using Robust.Shared.Network;

namespace Content.Client.DiscordAuth;

public sealed class DiscordAuthManager : Content.Corvax.Interfaces.Client.IClientDiscordAuthManager
{
    [Dependency] private readonly IClientNetManager _netManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;

    public string AuthUrl { get; private set; } = string.Empty;
    public Texture? Qrcode { get; private set; }
    public string DiscordUrl { get; private set; } = string.Empty;
    public Texture? DiscordQrcode { get; private set; }
    public string DiscordUsername { get; private set; } = string.Empty;

    public void Initialize()
    {
        _netManager.RegisterNetMessage<MsgDiscordAuthCheck>();
        _netManager.RegisterNetMessage<MsgDiscordAuthRequired>(OnDiscordAuthRequired);
        _netManager.RegisterNetMessage<MsgDiscordMemberCheck>();
        _netManager.RegisterNetMessage<MsgDiscordMemberRequired>(OnDiscordMemberRequired);
    }

    private void OnDiscordAuthRequired(MsgDiscordAuthRequired message)
    {
        if (_stateManager.CurrentState is not DiscordAuthState)
        {
            AuthUrl = message.AuthUrl;
            if (message.QrCode.Length > 0)
            {
                using var ms = new MemoryStream(message.QrCode);
                Qrcode = Texture.LoadFromPNGStream(ms);
            }

            _stateManager.RequestStateChange<DiscordAuthState>();
        }
    }

    private void OnDiscordMemberRequired(MsgDiscordMemberRequired message)
    {
        if (_stateManager.CurrentState is not DiscordMemberState)
        {
            DiscordUrl = message.AuthUrl;
            DiscordUsername = message.DiscordUsername;
            if (message.QrCode.Length > 0)
            {
                using var ms = new MemoryStream(message.QrCode);
                DiscordQrcode = Texture.LoadFromPNGStream(ms);
            }

            _stateManager.RequestStateChange<DiscordMemberState>();
        }
    }
}
