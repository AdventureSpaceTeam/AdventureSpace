using Content.Server.Actions;
using Content.Shared.Actions.Events;
using Content.Shared.Mobs.Systems;
using Content.Shared.Throwing;
using Content.Shared._c4llv07e.Aliens;
using Robust.Shared.Player;
using Robust.Shared.Random;
using System.Numerics;

namespace Content.Server._c4llv07e.Aliens;

public sealed class FaceJumpSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FaceHuggerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<FaceHuggerComponent, FaceJumpInstantActionEvent>(OnFaceJumpInstantAction);
    }

    private void OnMapInit(Entity<FaceHuggerComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.Action, "c4ll_ActionFaceJump");
    }

    private void OnFaceJumpInstantAction(Entity<FaceHuggerComponent> ent, ref FaceJumpInstantActionEvent args)
    {
        // if (!TryComp<TransformComponent>(ent, out var transform))
        //     return;
        // var direction = coordinates.ToMapPos(EntityManager, _transformSystem) - Transform(player).WorldPosition;
        // _throwingSystem.TryThrow(ent, direction, 100.0f, ent);
    }
}
