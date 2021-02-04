﻿#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Botany;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.ComponentDependencies;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.GameObjects.Components.Botany
{
    [RegisterComponent]
    public class SeedExtractorComponent : Component, IInteractUsing
    {
        [ComponentDependency] private readonly PowerReceiverComponent? _powerReceiver = default!;

        [Dependency] private readonly IRobustRandom _random = default!;

        public override string Name => "SeedExtractor";

        // TODO: Upgradeable machines.
        private int _minSeeds = 1;
        private int _maxSeeds = 4;

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!_powerReceiver?.Powered ?? false)
                return false;

            if (eventArgs.Using.TryGetComponent(out ProduceComponent? produce) && produce.Seed != null)
            {
                eventArgs.User.PopupMessageCursor(Loc.GetString("You extract some seeds from the {0}.", eventArgs.Using.Name));

                eventArgs.Using.Delete();

                var random = _random.Next(_minSeeds, _maxSeeds);

                for (var i = 0; i < random; i++)
                {
                    produce.Seed.SpawnSeedPacket(Owner.Transform.Coordinates, Owner.EntityManager);
                }

                return true;
            }

            return false;
        }
    }
}
