﻿using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;
using Content.Shared.Examine;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Systems;

public sealed class ArtifactExamineTriggerSystem : EntitySystem
{
    [Dependency] private readonly ArtifactSystem _artifact = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ArtifactExamineTriggerComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(EntityUid uid, ArtifactExamineTriggerComponent component, ExaminedEvent args)
    {
        _artifact.TryActivateArtifact(uid);
    }
}
