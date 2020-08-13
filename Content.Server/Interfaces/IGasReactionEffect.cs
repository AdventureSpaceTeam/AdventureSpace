﻿#nullable enable
using Content.Server.Atmos;
using Content.Server.Atmos.Reactions;
using Robust.Shared.Interfaces.Serialization;

namespace Content.Server.Interfaces
{
    public interface IGasReactionEffect : IExposeData
    {
        ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder);
    }
}
