using System.Numerics;
using Content.Shared.Movement.Events;
using Content.Shared.AdventureSpace.Zombie.Smoker.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.AdventureSpace.Zombie.Smoker;

public abstract class SharedZombieSmokerSystem : EntitySystem
{
    [Dependency] protected readonly SharedPhysicsSystem Physics = default!;
    [Dependency] protected readonly SharedJointSystem Joints = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;

    private const float ReelRate = 2.5f;
    protected const string SmokeCufJoint = "SmokeZombie";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ZombieSmokerTargetComponent, UpdateCanMoveEvent>(OnTargetCanMove);
        SubscribeLocalEvent<ZombieSmokerComponent, UpdateCanMoveEvent>(OnCanMove);
        SubscribeAllEvent<ZombieSmokerMoveTargetRequestEvent>(OnTargetMove);
    }

    private void OnTargetMove(ZombieSmokerMoveTargetRequestEvent msg, EntitySessionEventArgs args)
    {
        var user = args.SenderSession.AttachedEntity;
        if (user == null)
            return;

        if (!TryComp<Components.ZombieSmokerComponent>(user, out var smoker))
            return;

        if (smoker.CurrentTarget == EntityUid.Invalid)
            return;

        SetReeling((user.Value, smoker), msg.Reeling);
    }

    private void OnCanMove(EntityUid uid, Components.ZombieSmokerComponent component, UpdateCanMoveEvent args)
    {
        if (component.SmokerState == ZombieSmokerState.Prepare)
        {
            args.Cancel();
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<Components.ZombieSmokerComponent>();
        while (query.MoveNext(out var uid, out var smoker))
        {
            if (!smoker.Reeling)
                continue;

            if (!TryComp<JointComponent>(uid, out var jointComp))
                continue;

            if (!jointComp.GetJoints.TryGetValue(SmokeCufJoint, out var joint) || joint is not DistanceJoint distance)
            {
                SetReeling((uid, smoker), false);
                continue;
            }

            distance.MaxLength = MathF.Max(distance.MinLength, distance.MaxLength - ReelRate * frameTime);
            distance.Length = MathF.Min(distance.MaxLength, distance.Length);

            Physics.WakeBody(joint.BodyAUid);
            Physics.WakeBody(joint.BodyBUid);

            if (jointComp.Relay != null)
            {
                Physics.WakeBody(jointComp.Relay.Value);
            }

            Dirty(uid, jointComp);

            if (distance.MaxLength.Equals(distance.MinLength))
            {
                SetReeling((uid, smoker), false);
            }
        }
    }

    private void SetReeling(Entity<Components.ZombieSmokerComponent> smoker, bool value)
    {
        if (smoker.Comp.Reeling == value)
            return;

        smoker.Comp.Reeling = value;

        Dirty(smoker.Owner, smoker.Comp);
    }

    private void OnTargetCanMove(EntityUid uid, ZombieSmokerTargetComponent component, UpdateCanMoveEvent args)
    {
        args.Cancel();
    }

    protected void CreateDistanceJoint(Entity<Components.ZombieSmokerComponent> smoker, Entity<ZombieSmokerTargetComponent> target)
    {
        if (!Timing.IsFirstTimePredicted)
            return;

        var jointComp = EnsureComp<JointComponent>(smoker);
        var joint = Joints.CreateDistanceJoint(smoker, target, anchorA: new Vector2(0f, 0.5f), id: SmokeCufJoint);

        joint.MaxLength = joint.Length + 0.2f;
        joint.Stiffness = 1f;
        joint.MinLength = 0.35f;

        Dirty(smoker, jointComp);
    }

    protected void ClearJoint(EntityUid smoker, EntityUid target)
    {
        Joints.RemoveJoint(target, SmokeCufJoint);
        Joints.RemoveJoint(smoker, SmokeCufJoint);
    }
}
