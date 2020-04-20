﻿using System;
using Content.Client.UserInterface;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Players;

namespace Content.Client.GameObjects.Components.Mobs
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedCombatModeComponent))]
    public sealed class CombatModeComponent : SharedCombatModeComponent
    {
#pragma warning disable 649
        [Dependency] private readonly IPlayerManager _playerManager;
        [Dependency] private readonly IGameHud _gameHud;
#pragma warning restore 649

        public override bool IsInCombatMode
        {
            get => base.IsInCombatMode;
            set
            {
                base.IsInCombatMode = value;
                UpdateHud();
            }
        }

        public override TargetingZone ActiveZone
        {
            get => base.ActiveZone;
            set
            {
                base.ActiveZone = value;
                UpdateHud();
            }
        }

        public override void HandleMessage(ComponentMessage message, IComponent component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case PlayerAttachedMsg _:
                    _gameHud.CombatPanelVisible = true;
                    UpdateHud();
                    break;

                case PlayerDetachedMsg _:
                    _gameHud.CombatPanelVisible = false;
                    break;
            }
        }

        private void UpdateHud()
        {
            if (Owner != _playerManager.LocalPlayer.ControlledEntity)
            {
                return;
            }

            _gameHud.CombatModeActive = IsInCombatMode;
            _gameHud.TargetingZone = ActiveZone;
        }
    }
}
