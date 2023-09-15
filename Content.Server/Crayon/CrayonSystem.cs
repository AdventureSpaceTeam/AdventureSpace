using System.Linq;
using System.Numerics;
using Content.Server.Administration.Logs;
using Content.Server.Decals;
using Content.Server.Nutrition.EntitySystems;
using Content.Server.Popups;
using Content.Shared.Crayon;
using Content.Shared.Database;
using Content.Shared.Decals;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Crayon;

public sealed class CrayonSystem : SharedCrayonSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly DecalSystem _decals = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CrayonComponent, ComponentInit>(OnCrayonInit);
        SubscribeLocalEvent<CrayonComponent, CrayonSelectMessage>(OnCrayonBoundUI);
        SubscribeLocalEvent<CrayonComponent, CrayonColorMessage>(OnCrayonBoundUIColor);
        SubscribeLocalEvent<CrayonComponent, UseInHandEvent>(OnCrayonUse, before: new[] { typeof(FoodSystem) });
        SubscribeLocalEvent<CrayonComponent, AfterInteractEvent>(OnCrayonAfterInteract, after: new[] { typeof(FoodSystem) });
        SubscribeLocalEvent<CrayonComponent, DroppedEvent>(OnCrayonDropped);
        SubscribeLocalEvent<CrayonComponent, ComponentGetState>(OnCrayonGetState);
    }

    private static void OnCrayonGetState(EntityUid uid, CrayonComponent component, ref ComponentGetState args)
    {
        args.State = new CrayonComponentState(component.Color, component.SelectedState, component.Charges, component.Capacity);
    }

    private void OnCrayonAfterInteract(EntityUid uid, CrayonComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (component.Charges <= 0)
        {
            if (component.DeleteEmpty)
                UseUpCrayon(uid, args.User);
            else
                _popup.PopupEntity(Loc.GetString("crayon-interact-not-enough-left-text"), uid, args.User);

            args.Handled = true;
            return;
        }

        if (!args.ClickLocation.IsValid(EntityManager))
        {
            _popup.PopupEntity(Loc.GetString("crayon-interact-invalid-location"), uid, args.User);
            args.Handled = true;
            return;
        }

        if (!_decals.TryAddDecal(component.SelectedState, args.ClickLocation.Offset(new Vector2(-0.5f, -0.5f)), out _, component.Color, cleanable: true))
            return;

        if (component.UseSound != null)
            _audio.PlayPvs(component.UseSound, uid, AudioParams.Default.WithVariation(0.125f));

        // Decrease "Ammo"
        component.Charges--;
        Dirty(component);
        _adminLogger.Add(LogType.CrayonDraw, LogImpact.Low, $"{EntityManager.ToPrettyString(args.User):user} drew a {component.Color:color} {component.SelectedState}");
        args.Handled = true;

        if (component.DeleteEmpty && component.Charges <= 0)
            UseUpCrayon(uid, args.User);
    }

    private void OnCrayonUse(EntityUid uid, CrayonComponent component, UseInHandEvent args)
    {
        // Open crayon window if neccessary.
        if (args.Handled)
            return;

        if (!TryComp<ActorComponent>(args.User, out var actor) ||
            component.UserInterface == null)
        {
            return;
        }

        _uiSystem.ToggleUi(component.UserInterface, actor.PlayerSession);

        if (component.UserInterface?.SubscribedSessions.Contains(actor.PlayerSession) == true)
        {
            // Tell the user interface the selected stuff
            _uiSystem.SetUiState(component.UserInterface, new CrayonBoundUserInterfaceState(component.SelectedState, component.SelectableColor, component.Color));
        }

        args.Handled = true;
    }

    private void OnCrayonBoundUI(EntityUid uid, CrayonComponent component, CrayonSelectMessage args)
    {
        // Check if the selected state is valid
        if (!_prototypeManager.TryIndex<DecalPrototype>(args.State, out var prototype) || !prototype.Tags.Contains("crayon")) return;

        component.SelectedState = args.State;

        Dirty(component);
    }

    private void OnCrayonBoundUIColor(EntityUid uid, CrayonComponent component, CrayonColorMessage args)
    {
        // you still need to ensure that the given color is a valid color
        if (component.SelectableColor && args.Color != component.Color)
        {
            component.Color = args.Color;

            Dirty(component);
        }

    }

    private void OnCrayonInit(EntityUid uid, CrayonComponent component, ComponentInit args)
    {
        component.Charges = component.Capacity;

        // Get the first one from the catalog and set it as default
        var decal = _prototypeManager.EnumeratePrototypes<DecalPrototype>().FirstOrDefault(x => x.Tags.Contains("crayon"));
        component.SelectedState = decal?.ID ?? string.Empty;
        Dirty(component);
    }

    private void OnCrayonDropped(EntityUid uid, CrayonComponent component, DroppedEvent args)
    {
        if (TryComp<ActorComponent>(args.User, out var actor))
            _uiSystem.TryClose(uid, SharedCrayonComponent.CrayonUiKey.Key, actor.PlayerSession);
    }

    private void UseUpCrayon(EntityUid uid, EntityUid user)
    {
        _popup.PopupEntity(Loc.GetString("crayon-interact-used-up-text", ("owner", uid)), user, user);
        EntityManager.QueueDeleteEntity(uid);
    }
}
