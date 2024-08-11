using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Tools;
using Content.Shared.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._c4llv07e.DonateUplink;

public sealed class DonateUplinkSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DonateUplinkComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<DonateUplinkComponent, DonateUplinkScrewingFinishedEvent>(OnDeconstructed);
        SubscribeLocalEvent<DonateUplinkComponent, ActivatableUIOpenAttemptEvent>(OnUIOpenAttempt);
        SubscribeLocalEvent<DonateUplinkComponent, ComponentGetState>(GetCompState);
        SubscribeLocalEvent<DonateUplinkComponent, ComponentHandleState>(HandleCompState);
    }

    private void GetCompState(Entity<DonateUplinkComponent> ent, ref ComponentGetState args)
    {
        args.State = new DonateUplinkComponentState
        {
            Open = ent.Comp.Opened,
        };
    }

    private void HandleCompState(Entity<DonateUplinkComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not DonateUplinkComponentState state)
            return;
        ent.Comp.Opened = state.Open;
    }

    private void OnInteractUsing(Entity<DonateUplinkComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;
        if (!_timing.IsFirstTimePredicted)
            return;
        if (!_tool.HasQuality(args.Used, "Screwing"))
            return;
        args.Handled = true;
        _tool.UseTool(args.Used, args.User, ent.Owner, 1f, "Screwing", new DonateUplinkScrewingFinishedEvent());
    }

    private void OnDeconstructed(Entity<DonateUplinkComponent> ent, ref DonateUplinkScrewingFinishedEvent args)
    {
        ent.Comp.Opened = !ent.Comp.Opened;
        Dirty(ent);
        var meta = MetaData(ent);
        if (ent.Comp.Opened)
        {
            _metaData.SetEntityDescription(ent, $"На внутренней стороне написано \"{ent.Comp.BackplateText}\"", meta);
        }
        else
        {
            _metaData.SetEntityDescription(ent, string.Empty, meta);
        }
        if (TryComp<AppearanceComponent>(ent.Owner, out var appearance))
            _appearance.SetData(ent.Owner, DonateUplinkVisualLayers.Opened, ent.Comp.Opened, appearance);
    }

    private void OnUIOpenAttempt(Entity<DonateUplinkComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (ent.Comp.Opened)
            args.Cancel();
    }
}
