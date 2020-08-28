﻿using System.Collections.Generic;
using Content.Server.Sandbox;
using Robust.Server.Interfaces.Player;
using Robust.Shared.IoC;

namespace Content.Server.GameTicking.GamePresets
{
    public sealed class PresetSandbox : GamePreset
    {
        [Dependency] private readonly ISandboxManager _sandboxManager = default!;

        public override bool Start(IReadOnlyList<IPlayerSession> readyPlayers, bool force = false)
        {
            _sandboxManager.IsSandboxEnabled = true;
            return true;
        }

        public override string ModeTitle => "Sandbox";
        public override string Description => "No stress, build something!";
    }
}
