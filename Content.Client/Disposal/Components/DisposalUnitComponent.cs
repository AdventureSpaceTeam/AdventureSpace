﻿using Content.Shared.Disposal.Components;
using Content.Shared.DragDrop;
using Robust.Shared.GameObjects;

namespace Content.Client.Disposal.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedDisposalUnitComponent))]
    public class DisposalUnitComponent : SharedDisposalUnitComponent
    {
        public DisposalUnitBoundUserInterfaceState? UiState;

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);
            if (curState is not DisposalUnitComponentState state) return;

            RecentlyEjected = state.RecentlyEjected;
        }

        public override bool DragDropOn(DragDropEvent eventArgs)
        {
            return false;
        }
    }
}
