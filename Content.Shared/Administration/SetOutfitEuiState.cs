using Content.Shared.Eui;
using Robust.Shared.Serialization;
using System;
using Robust.Shared.GameObjects;

namespace Content.Shared.Administration
{
    [Serializable, NetSerializable]
    public class SetOutfitEuiState : EuiStateBase
    {
        public EntityUid TargetEntityId;
    }
}
