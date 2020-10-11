﻿using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Shared.GameObjects.Components;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    public sealed class ComputerComponent : SharedComputerComponent, IMapInit
    {
        [ViewVariables]
        private string _boardPrototype;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _boardPrototype, "board", string.Empty);
        }

        public override void Initialize()
        {
            base.Initialize();

            if (Owner.TryGetComponent(out PowerReceiverComponent powerReceiver))
            {
                powerReceiver.OnPowerStateChanged += PowerReceiverOnOnPowerStateChanged;

                if (Owner.TryGetComponent(out AppearanceComponent appearance))
                {
                    appearance.SetData(ComputerVisuals.Powered, powerReceiver.Powered);
                }
            }

            CreateComputerBoard();
        }

        public override void OnRemove()
        {
            if (Owner.TryGetComponent(out PowerReceiverComponent powerReceiver))
            {
                powerReceiver.OnPowerStateChanged -= PowerReceiverOnOnPowerStateChanged;
            }

            base.OnRemove();
        }

        private void PowerReceiverOnOnPowerStateChanged(object sender, PowerStateEventArgs e)
        {
            if (Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(ComputerVisuals.Powered, e.Powered);
            }
        }

        /// <summary>
        ///     Creates the corresponding computer board on the computer.
        ///     This exists so when you deconstruct computers that were serialized with the map,
        ///     you can retrieve the computer board.
        /// </summary>
        private void CreateComputerBoard()
        {
            // We don't do anything if this is null or empty.
            if (string.IsNullOrEmpty(_boardPrototype))
                return;

            var container = ContainerManagerComponent.Ensure<Container>("board", Owner, out var existed);

            if (existed)
            {
                // We already contain a board. Note: We don't check if it's the right one!
                if (container.ContainedEntities.Count != 0)
                    return;
            }

            var board = Owner.EntityManager.SpawnEntity(_boardPrototype, Owner.Transform.Coordinates);

            if(!container.Insert(board))
                Logger.Warning($"Couldn't insert board {board} to computer {Owner}!");
        }

        public void MapInit()
        {
            CreateComputerBoard();
        }
    }
}
