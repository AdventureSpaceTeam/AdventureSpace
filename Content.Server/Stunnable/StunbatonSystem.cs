﻿using System.Linq;
using Content.Server.Items;
using Content.Server.PowerCell.Components;
using Content.Server.Stunnable.Components;
using Content.Server.Weapon.Melee;
using Content.Shared.ActionBlocker;
using Content.Shared.Audio;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Notification.Managers;
using Content.Shared.Throwing;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Stunnable
{
    public class StunbatonSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<StunbatonComponent, MeleeHitEvent>(OnMeleeHit);
            SubscribeLocalEvent<StunbatonComponent, MeleeInteractEvent>(OnMeleeInteract);
            SubscribeLocalEvent<StunbatonComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<StunbatonComponent, ThrowCollideEvent>(OnThrowCollide);
            SubscribeLocalEvent<StunbatonComponent, PowerCellChangedEvent>(OnPowerCellChanged);
            SubscribeLocalEvent<StunbatonComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<StunbatonComponent, ExaminedEvent>(OnExamined);
        }

        private void OnMeleeHit(EntityUid uid, StunbatonComponent comp, MeleeHitEvent args)
        {
            if (!comp.Activated || !args.HitEntities.Any())
                return;

            if (!ComponentManager.TryGetComponent<PowerCellSlotComponent>(uid, out var slot) || slot.Cell == null || !slot.Cell.TryUseCharge(comp.EnergyPerUse))
                return;

            foreach (IEntity entity in args.HitEntities)
            {
                StunEntity(entity, comp);
            }
        }

        private void OnMeleeInteract(EntityUid uid, StunbatonComponent comp, MeleeInteractEvent args)
        {
            if (!comp.Activated)
                return;

            if (!ComponentManager.TryGetComponent<PowerCellSlotComponent>(uid, out var slot) || slot.Cell == null || !slot.Cell.TryUseCharge(comp.EnergyPerUse))
                return;

            if (args.Entity.HasComponent<StunnableComponent>())
            {
                args.CanInteract = true;
                StunEntity(args.Entity, comp);
            }
        }

        private void OnUseInHand(EntityUid uid, StunbatonComponent comp, UseInHandEvent args)
        {
            if (!Get<ActionBlockerSystem>().CanUse(args.User))
                return;

            if (comp.Activated)
            {
                TurnOff(comp);
            }
            else
            {
                TurnOn(comp, args.User);
            }
        }

        private void OnThrowCollide(EntityUid uid, StunbatonComponent comp, ThrowCollideEvent args)
        {
            if (!ComponentManager.TryGetComponent<PowerCellSlotComponent>(uid, out var slot)) return;
            if (!comp.Activated || slot.Cell == null || !slot.Cell.TryUseCharge(comp.EnergyPerUse)) return;

            StunEntity(args.Target, comp);
        }

        private void OnPowerCellChanged(EntityUid uid, StunbatonComponent comp, PowerCellChangedEvent args)
        {
            if (args.Ejected)
            {
                TurnOff(comp);
            }
        }

        private void OnInteractUsing(EntityUid uid, StunbatonComponent comp, InteractUsingEvent args)
        {
            if (!Get<ActionBlockerSystem>().CanInteract(args.User))
                return;

            if (ComponentManager.TryGetComponent<PowerCellSlotComponent>(uid, out var cellslot))
                cellslot.InsertCell(args.Used);
        }

        private void OnExamined(EntityUid uid, StunbatonComponent comp, ExaminedEvent args)
        {
            args.Message.AddText("\n");
            var msg = comp.Activated
                ? Loc.GetString("comp-stunbaton-examined-on")
                : Loc.GetString("comp-stunbaton-examined-off");
            args.Message.AddMarkup(msg);
        }

        private void StunEntity(IEntity entity, StunbatonComponent comp)
        {
            if (!entity.TryGetComponent(out StunnableComponent? stunnable) || !comp.Activated) return;

            SoundSystem.Play(Filter.Pvs(comp.Owner), "/Audio/Weapons/egloves.ogg", comp.Owner.Transform.Coordinates, AudioHelpers.WithVariation(0.25f));
            if(!stunnable.SlowedDown)
            {
                if(_robustRandom.Prob(comp.ParalyzeChanceNoSlowdown))
                    stunnable.Paralyze(comp.ParalyzeTime);
                else
                    stunnable.Slowdown(comp.SlowdownTime);
            }
            else
            {
                if(_robustRandom.Prob(comp.ParalyzeChanceWithSlowdown))
                    stunnable.Paralyze(comp.ParalyzeTime);
                else
                    stunnable.Slowdown(comp.SlowdownTime);
            }


            if (!comp.Owner.TryGetComponent<PowerCellSlotComponent>(out var slot) || slot.Cell == null || !(slot.Cell.CurrentCharge < comp.EnergyPerUse)) return;

            SoundSystem.Play(Filter.Pvs(comp.Owner), AudioHelpers.GetRandomFileFromSoundCollection("sparks"), comp.Owner.Transform.Coordinates, AudioHelpers.WithVariation(0.25f));
            TurnOff(comp);
        }

        private void TurnOff(StunbatonComponent comp)
        {
            if (!comp.Activated)
            {
                return;
            }

            if (!comp.Owner.TryGetComponent<SpriteComponent>(out var sprite) ||
                !comp.Owner.TryGetComponent<ItemComponent>(out var item)) return;

            SoundSystem.Play(Filter.Pvs(comp.Owner), AudioHelpers.GetRandomFileFromSoundCollection("sparks"), comp.Owner.Transform.Coordinates, AudioHelpers.WithVariation(0.25f));
            item.EquippedPrefix = "off";
            // TODO stunbaton visualizer
            sprite.LayerSetState(0, "stunbaton_off");
            comp.Activated = false;
        }

        private void TurnOn(StunbatonComponent comp, IEntity user)
        {
            if (comp.Activated)
            {
                return;
            }

            if (!comp.Owner.TryGetComponent<SpriteComponent>(out var sprite) ||
                !comp.Owner.TryGetComponent<ItemComponent>(out var item)) return;

            var playerFilter = Filter.Pvs(comp.Owner);
            if (!comp.Owner.TryGetComponent<PowerCellSlotComponent>(out var slot))
                return;

            if (slot.Cell == null)
            {
                SoundSystem.Play(playerFilter, "/Audio/Machines/button.ogg", comp.Owner.Transform.Coordinates, AudioHelpers.WithVariation(0.25f));
                user.PopupMessage(Loc.GetString("comp-stunbaton-activated-missing-cell"));
                return;
            }

            if (slot.Cell != null && slot.Cell.CurrentCharge < comp.EnergyPerUse)
            {
                SoundSystem.Play(playerFilter, "/Audio/Machines/button.ogg", comp.Owner.Transform.Coordinates, AudioHelpers.WithVariation(0.25f));
                user.PopupMessage(Loc.GetString("comp-stunbaton-activated-dead-cell"));
                return;
            }

            SoundSystem.Play(playerFilter, AudioHelpers.GetRandomFileFromSoundCollection("sparks"), comp.Owner.Transform.Coordinates, AudioHelpers.WithVariation(0.25f));

            item.EquippedPrefix = "on";
            sprite.LayerSetState(0, "stunbaton_on");
            comp.Activated = true;
        }
    }
}
