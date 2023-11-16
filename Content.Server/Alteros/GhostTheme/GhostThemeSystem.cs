using Content.Corvax.Interfaces.Server;
using Content.Shared.Alteros.GhostTheme;
using Content.Shared.Ghost;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Server.Alteros.GhostTheme;

public sealed class GhostThemeSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GhostComponent, PlayerAttachedEvent>(OnPlayerAttached);
    }

    private void OnPlayerAttached(EntityUid uid, GhostComponent component, PlayerAttachedEvent args)
    {
        var sponsors = IoCManager.Resolve<IServerSponsorsManager>(); // Alteros-Sponsors
        if (!sponsors.TryGetGhostTheme(args.Player.UserId, out var ghostTheme) ||
            !_prototypeManager.TryIndex<GhostThemePrototype>(ghostTheme, out var ghostThemePrototype)
           )
        {
            return;
        }
        foreach (var entry in ghostThemePrototype!.Components.Values)
        {
            var comp = (Component) _serialization.CreateCopy(entry.Component, notNullableOverride: true);
            comp.Owner = uid;
            EntityManager.AddComponent(uid, comp, true);
        }

        EnsureComp<GhostThemeComponent>(uid).GhostTheme = ghostTheme;

    }
}
