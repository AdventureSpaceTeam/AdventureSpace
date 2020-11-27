﻿using System;
using System.Collections.Generic;
using Content.Shared.Network.NetMessages;
using Robust.Server.Interfaces.Player;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;

#nullable enable

namespace Content.Server.Eui
{
    public sealed class EuiManager : IPostInjectInit
    {
        [Dependency] private readonly IPlayerManager _players = default!;
        [Dependency] private readonly IServerNetManager _net = default!;

        private readonly Dictionary<IPlayerSession, PlayerEuiData> _playerData =
            new();

        private readonly Queue<(IPlayerSession player, uint id)> _stateUpdateQueue =
            new Queue<(IPlayerSession, uint id)>();

        private sealed class PlayerEuiData
        {
            public uint NextId = 1;
            public readonly Dictionary<uint, BaseEui> OpenUIs = new();
        }

        void IPostInjectInit.PostInject()
        {
            _players.PlayerStatusChanged += PlayerStatusChanged;
        }

        public void Initialize()
        {
            _net.RegisterNetMessage<MsgEuiCtl>(MsgEuiCtl.NAME);
            _net.RegisterNetMessage<MsgEuiState>(MsgEuiState.NAME);
            _net.RegisterNetMessage<MsgEuiMessage>(MsgEuiMessage.NAME, RxMsgMessage);
        }

        public void SendUpdates()
        {
            while (_stateUpdateQueue.TryDequeue(out var tuple))
            {
                var (player, id) = tuple;

                // Check that UI and player still exist.
                // COULD have been removed in the mean time.
                if (!_playerData.TryGetValue(player, out var plyDat) || !plyDat.OpenUIs.TryGetValue(id, out var ui))
                {
                    continue;
                }

                ui.DoStateUpdate();
            }
        }

        public void OpenEui(BaseEui eui, IPlayerSession player)
        {
            if (eui.Id != 0)
            {
                throw new ArgumentException("That EUI is already open!");
            }

            var data = _playerData[player];
            var newId = data.NextId++;
            eui.Initialize(this, player, newId);

            data.OpenUIs.Add(newId, eui);

            var msg = _net.CreateNetMessage<MsgEuiCtl>();
            msg.Id = newId;
            msg.Type = MsgEuiCtl.CtlType.Open;
            msg.OpenType = eui.GetType().Name;

            _net.ServerSendMessage(msg, player.ConnectedClient);
        }

        public void CloseEui(BaseEui eui)
        {
            eui.Shutdown();
            _playerData[eui.Player].OpenUIs.Remove(eui.Id);

            var msg = _net.CreateNetMessage<MsgEuiCtl>();
            msg.Id = eui.Id;
            msg.Type = MsgEuiCtl.CtlType.Close;
            _net.ServerSendMessage(msg, eui.Player.ConnectedClient);
        }

        private void RxMsgMessage(MsgEuiMessage message)
        {
            if (!_players.TryGetSessionByChannel(message.MsgChannel, out var ply))
            {
                return;
            }

            if (!_playerData.TryGetValue(ply, out var dat))
            {
                return;
            }

            if (!dat.OpenUIs.TryGetValue(message.Id, out var eui))
            {
                Logger.WarningS("eui", $"Got EUI message from player {ply} for non-existing UI {message.Id}");
                return;
            }

            eui.HandleMessage(message.Message);
        }

        private void PlayerStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            if (e.NewStatus == SessionStatus.Connected)
            {
                _playerData.Add(e.Session, new PlayerEuiData());
            }
            else if (e.NewStatus == SessionStatus.Disconnected)
            {
                if (_playerData.TryGetValue(e.Session, out var plyDat))
                {
                    // Gracefully close all open UIs.
                    foreach (var ui in plyDat.OpenUIs.Values)
                    {
                        ui.Closed();
                    }

                    _playerData.Remove(e.Session);
                }
            }
        }

        public void QueueStateUpdate(BaseEui eui)
        {
            DebugTools.Assert(eui.Id != 0, "EUI has not been opened yet.");
            DebugTools.Assert(!eui.IsShutDown, "EUI has been closed.");

            _stateUpdateQueue.Enqueue((eui.Player, eui.Id));
        }
    }
}
