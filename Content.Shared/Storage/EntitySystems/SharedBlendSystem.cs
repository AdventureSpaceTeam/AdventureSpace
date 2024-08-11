namespace Content.Shared.Storage;
using Content.Shared.Interaction.Events;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Audio.Systems;
using static Content.Shared.Storage.BlendComponent;
using Robust.Shared.Timing;

public sealed class SharedBlendSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BlendComponent, UseInHandEvent>(OnUseInHand);
        //SubscribeLocalEvent<BlendComponent, >
    }

    private void OnUseInHand(EntityUid uid, BlendComponent component, UseInHandEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!TryComp<StorageComponent>(uid, out var storage))
            return;

        _audio.PlayPvs(component.Sound, uid);

        List<EntityUid> dumpQueue = _containerSystem.EmptyContainer(storage.Container);

        for (int i = 0; i < dumpQueue.Count; i++)
        {
            int randomIndex = _random.Next(i, dumpQueue.Count);
            var temp = dumpQueue[i];
            dumpQueue[i] = dumpQueue[randomIndex];
            dumpQueue[randomIndex] = temp;
        }

        foreach (var ent in dumpQueue)
        {
            _containerSystem.Insert(ent, storage.Container);
        }

    }
}

