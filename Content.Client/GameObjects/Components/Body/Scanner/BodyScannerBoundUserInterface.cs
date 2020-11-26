﻿using System;
using Content.Shared.GameObjects.Components.Body.Scanner;
using JetBrains.Annotations;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components.Body.Scanner
{
    [UsedImplicitly]
    public class BodyScannerBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private BodyScannerDisplay _display;

        [ViewVariables]
        private IEntity _entity;

        public BodyScannerBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey) { }

        protected override void Open()
        {
            base.Open();
            _display = new BodyScannerDisplay(this);
            _display.OnClose += Close;
            _display.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not BodyScannerUIState scannerState)
            {
                return;
            }

            if (!Owner.Owner.EntityManager.TryGetEntity(scannerState.Uid, out _entity))
            {
                throw new ArgumentException($"Received an invalid entity with id {scannerState.Uid} for body scanner with id {Owner.Owner.Uid} at {Owner.Owner.Transform.MapPosition}");
            }

            _display.UpdateDisplay(_entity);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _display?.Dispose();
            }
        }
    }
}
