﻿using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Server.Preferences.Managers;
using Content.Shared;
using Content.Shared.CCVar;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Network;


namespace Content.Server.Connection
{
    public interface IConnectionManager
    {
        void Initialize();
    }

    /// <summary>
    ///     Handles various duties like guest username assignment, bans, connection logs, etc...
    /// </summary>
    public sealed class ConnectionManager : IConnectionManager
    {
        [Dependency] private readonly IServerDbManager _dbManager = default!;
        [Dependency] private readonly IPlayerManager _plyMgr = default!;
        [Dependency] private readonly IServerNetManager _netMgr = default!;
        [Dependency] private readonly IServerDbManager _db = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        public void Initialize()
        {
            _netMgr.Connecting += NetMgrOnConnecting;
            _netMgr.AssignUserIdCallback = AssignUserIdCallback;
            // Approval-based IP bans disabled because they don't play well with Happy Eyeballs.
            // _netMgr.HandleApprovalCallback = HandleApproval;
        }

        /*
        private async Task<NetApproval> HandleApproval(NetApprovalEventArgs eventArgs)
        {
            var ban = await _db.GetServerBanByIpAsync(eventArgs.Connection.RemoteEndPoint.Address);
            if (ban != null)
            {
                var expires = "This is a permanent ban.";
                if (ban.ExpirationTime is { } expireTime)
                {
                    var duration = expireTime - ban.BanTime;
                    var utc = expireTime.ToUniversalTime();
                    expires = $"This ban is for {duration.TotalMinutes} minutes and will expire at {utc:f} UTC.";
                }
                var reason = $@"You, or another user of this computer or connection is banned from playing here.
The ban reason is: ""{ban.Reason}""
{expires}";
                return NetApproval.Deny(reason);
            }

            return NetApproval.Allow();
        }
        */

        private async Task NetMgrOnConnecting(NetConnectingArgs e)
        {
            // Check if banned.
            var addr = e.IP.Address;
            var userId = e.UserId;
            ImmutableArray<byte>? hwId = e.UserData.HWId;
            if (hwId.Value.Length == 0 || !_cfg.GetCVar(CCVars.BanHardwareIds))
            {
                // HWId not available for user's platform, don't look it up.
                // Or hardware ID checks disabled.
                hwId = null;
            }

            var adminData = await _dbManager.GetAdminDataForAsync(e.UserId);
            if (_plyMgr.PlayerCount >= _cfg.GetCVar(CCVars.SoftMaxPlayers) && adminData is null)
            {
                e.Deny("The server is full!");
                return;
            }

            var ban = await _db.GetServerBanAsync(addr, userId, hwId);
            if (ban != null)
            {
                e.Deny(ban.DisconnectMessage);
                return;
            }

            if (_cfg.GetCVar(CCVars.WhitelistEnabled)
                && await _db.GetWhitelistStatusAsync(userId) == false
                && adminData is null)
            {
                e.Deny(Loc.GetString("whitelist-not-whitelisted"));
                return;
            }

            if (!ServerPreferencesManager.ShouldStorePrefs(e.AuthType))
            {
                return;
            }

            await _db.UpdatePlayerRecordAsync(userId, e.UserName, addr, e.UserData.HWId);
            await _db.AddConnectionLogAsync(userId, e.UserName, addr, e.UserData.HWId);
        }

        private async Task<NetUserId?> AssignUserIdCallback(string name)
        {
            if (!_cfg.GetCVar(CCVars.GamePersistGuests))
            {
                return null;
            }

            var userId = await _db.GetAssignedUserIdAsync(name);
            if (userId != null)
            {
                return userId;
            }

            var assigned = new NetUserId(Guid.NewGuid());
            await _db.AssignUserIdAsync(name, assigned);
            return assigned;
        }
    }
}
