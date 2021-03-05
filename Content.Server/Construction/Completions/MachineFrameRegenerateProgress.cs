﻿#nullable enable
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Construction;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public class MachineFrameRegenerateProgress : IGraphAction
    {
        public async Task PerformAction(IEntity entity, IEntity? user)
        {
            if (entity.Deleted)
                return;

            if (entity.TryGetComponent<MachineFrameComponent>(out var machineFrame))
            {
                machineFrame.RegenerateProgress();
            }
        }
    }
}
