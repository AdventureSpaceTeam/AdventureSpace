using Content.Shared.Alteros.GhostTheme;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using GhostThemeComponent = Content.Shared.Alteros.GhostTheme.GhostThemeComponent;

namespace Content.Client.Alteros.GhostTheme;

public sealed class GhostThemeSystem: EntitySystem
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GhostThemeComponent, AfterAutoHandleStateEvent>(OnInit);
    }

    private void OnInit(EntityUid uid, GhostThemeComponent component, ref AfterAutoHandleStateEvent args)
    {
        if (component.GhostTheme == null
            || !_prototypeManager.TryIndex<GhostThemePrototype>(component.GhostTheme, out var ghostThemePrototype))
        {
            return;
        }
        foreach (var entry in ghostThemePrototype.Components.Values)
        {
            if (entry.Component is SpriteComponent spriteComponent && EntityManager.TryGetComponent<SpriteComponent>(uid, out var targetsprite))
            {
                targetsprite.CopyFrom(spriteComponent);
            }
        }
    }
}
