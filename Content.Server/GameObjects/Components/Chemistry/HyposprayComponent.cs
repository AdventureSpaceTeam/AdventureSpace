﻿using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Mobs.State;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Chemistry;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.ComponentDependencies;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

#nullable enable

namespace Content.Server.GameObjects.Components.Chemistry
{
    [RegisterComponent]
    public sealed class HyposprayComponent : SharedHyposprayComponent, IAttack, ISolutionChange, IAfterInteract
    {
        [ViewVariables(VVAccess.ReadWrite)] public float ClumsyFailChance { get; set; }
        [ViewVariables(VVAccess.ReadWrite)] public ReagentUnit TransferAmount { get; set; }

        [ComponentDependency] private readonly SolutionContainerComponent? _solution = default!;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => x.ClumsyFailChance, "ClumsyFailChance", 0.5f);
            serializer.DataField(this, x => x.TransferAmount, "TransferAmount", ReagentUnit.New(5));
        }

        public override void Initialize()
        {
            base.Initialize();

            Dirty();
        }

        bool IAttack.ClickAttack(AttackEventArgs eventArgs)
        {
            var target = eventArgs.TargetEntity;
            var user = eventArgs.User;

            return TryDoInject(target, user);
        }

        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (!eventArgs.CanReach)
                return false;

            return TryDoInject(eventArgs.Target, eventArgs.User);
        }

        private bool TryDoInject(IEntity? target, IEntity user)
        {
            if (target == null || !EligibleEntity(target))
                return false;

            var msgFormat = "You inject {0:TheName}.";

            if (target == user)
            {
                msgFormat = "You inject yourself.";
            }
            else if (EligibleEntity(user) && ClumsyComponent.TryRollClumsy(user, ClumsyFailChance))
            {
                msgFormat = "Oops! You injected yourself!";
                target = user;
            }

            if (_solution == null || _solution.CurrentVolume == 0)
            {
                user.PopupMessageCursor(Loc.GetString("It's empty!"));
                return true;
            }

            user.PopupMessage(Loc.GetString(msgFormat, target));
            if (target != user)
            {
                target.PopupMessage(Loc.GetString("You feel a tiny prick!"));
                var meleeSys = EntitySystem.Get<MeleeWeaponSystem>();
                var angle = new Angle(target.Transform.WorldPosition - user.Transform.WorldPosition);
                meleeSys.SendLunge(angle, user);
            }

            EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Items/hypospray.ogg", user);

            var targetSolution = target.GetComponent<SolutionContainerComponent>();

            // Get transfer amount. May be smaller than _transferAmount if not enough room
            var realTransferAmount = ReagentUnit.Min(TransferAmount, targetSolution.EmptyVolume);

            if (realTransferAmount <= 0)
            {
                user.PopupMessage(user, Loc.GetString("{0:TheName} is already full!", targetSolution.Owner));
                return true;
            }

            // Move units from attackSolution to targetSolution
            var removedSolution = _solution.SplitSolution(realTransferAmount);

            if (!targetSolution.CanAddSolution(removedSolution))
            {
                return true;
            }

            removedSolution.DoEntityReaction(target, ReactionMethod.Injection);

            targetSolution.TryAddSolution(removedSolution);

            static bool EligibleEntity(IEntity entity)
            {
                // TODO: Does checking for BodyComponent make sense as a "can be hypospray'd" tag?
                // In SS13 the hypospray ONLY works on mobs, NOT beakers or anything else.
                return entity.HasComponent<SolutionContainerComponent>() && entity.HasComponent<MobStateComponent>();
            }

            return true;
        }

        void ISolutionChange.SolutionChanged(SolutionChangeEventArgs eventArgs)
        {
            Dirty();
        }

        public override ComponentState GetComponentState()
        {
            if (_solution == null)
                return new HyposprayComponentState(ReagentUnit.Zero, ReagentUnit.Zero);

            return new HyposprayComponentState(_solution.CurrentVolume, _solution.MaxVolume);
        }
    }
}
