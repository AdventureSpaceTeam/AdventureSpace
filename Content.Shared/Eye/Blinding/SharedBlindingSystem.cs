using Content.Shared.Clothing.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Inventory;
using Content.Shared.Rejuvenate;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using JetBrains.Annotations;

namespace Content.Shared.Eye.Blinding
{
    public sealed class SharedBlindingSystem : EntitySystem
    {
        public const string BlindingStatusEffect = "TemporaryBlindness";
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<BlindfoldComponent, GotEquippedEvent>(OnEquipped);
            SubscribeLocalEvent<BlindfoldComponent, GotUnequippedEvent>(OnUnequipped);

            SubscribeLocalEvent<VisionCorrectionComponent, GotEquippedEvent>(OnGlassesEquipped);
            SubscribeLocalEvent<VisionCorrectionComponent, GotUnequippedEvent>(OnGlassesUnequipped);

            SubscribeLocalEvent<BlurryVisionComponent, ComponentGetState>(OnGetState);

            SubscribeLocalEvent<TemporaryBlindnessComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<TemporaryBlindnessComponent, ComponentShutdown>(OnShutdown);

            SubscribeLocalEvent<BlindableComponent, RejuvenateEvent>(OnRejuvenate);
        }

        private void OnEquipped(EntityUid uid, BlindfoldComponent component, GotEquippedEvent args)
        {
            if (!TryComp<ClothingComponent>(uid, out var clothing) || clothing.Slots == SlotFlags.PREVENTEQUIP) // we live in a society
                return;
            // Is the clothing in its actual slot?
            if (!clothing.Slots.HasFlag(args.SlotFlags))
                return;

            component.IsActive = true;
            if (!TryComp<BlindableComponent>(args.Equipee, out var blindComp))
                return;
            AdjustBlindSources(args.Equipee, 1, blindComp);
        }

        private void OnUnequipped(EntityUid uid, BlindfoldComponent component, GotUnequippedEvent args)
        {
            if (!component.IsActive)
                return;
            component.IsActive = false;
            if (!TryComp<BlindableComponent>(args.Equipee, out var blindComp))
                return;
            AdjustBlindSources(args.Equipee, -1, blindComp);
        }

        private void OnGlassesEquipped(EntityUid uid, VisionCorrectionComponent component, GotEquippedEvent args)
        {
            if (!TryComp<ClothingComponent>(uid, out var clothing) || clothing.Slots == SlotFlags.PREVENTEQUIP) // we live in a society
                return;
            // Is the clothing in its actual slot?
            if (!clothing.Slots.HasFlag(args.SlotFlags))
                return;

            if (!TryComp<BlurryVisionComponent>(args.Equipee, out var blur))
                return;

            component.IsActive = true;
            blur.Magnitude += component.VisionBonus;
            blur.Dirty();
        }

        private void OnGlassesUnequipped(EntityUid uid, VisionCorrectionComponent component, GotUnequippedEvent args)
        {
            if (!component.IsActive || !TryComp<BlurryVisionComponent>(args.Equipee, out var blur))
                return;
            component.IsActive = false;
            blur.Magnitude -= component.VisionBonus;
            blur.Dirty();
        }

        private void OnGetState(EntityUid uid, BlurryVisionComponent component, ref ComponentGetState args)
        {
            args.State = new BlurryVisionComponentState(component.Magnitude);
        }

        private void OnInit(EntityUid uid, TemporaryBlindnessComponent component, ComponentInit args)
        {
            AdjustBlindSources(uid, 1);
        }

        private void OnShutdown(EntityUid uid, TemporaryBlindnessComponent component, ComponentShutdown args)
        {
            AdjustBlindSources(uid, -1);
        }

        private void OnRejuvenate(EntityUid uid, BlindableComponent component, RejuvenateEvent args)
        {
            AdjustEyeDamage(uid, -component.EyeDamage, component);
        }

        [PublicAPI]
        public void AdjustBlindSources(EntityUid uid, int amount, BlindableComponent? blindable = null)
        {
            if (!Resolve(uid, ref blindable, false))
                return;

            blindable.Sources += amount;
            blindable.Sources = Math.Max(blindable.Sources, 0);

            Dirty(blindable);
        }

        public void AdjustEyeDamage(EntityUid uid, int amount, BlindableComponent? blindable = null)
        {
            if (!Resolve(uid, ref blindable, false))
                return;

            blindable.EyeDamage += amount;

            if (blindable.EyeDamage > 0)
            {
                var blurry = EnsureComp<BlurryVisionComponent>(uid);
                blurry.Magnitude = (9 - blindable.EyeDamage);
                blurry.Dirty();
            }
            else
            {
                RemComp<BlurryVisionComponent>(uid);
            }

            if (!blindable.EyeTooDamaged && blindable.EyeDamage >= 8)
            {
                blindable.EyeTooDamaged = true;
                AdjustBlindSources(uid, 1, blindable);
            }
            if (blindable.EyeTooDamaged && blindable.EyeDamage < 8)
            {
                blindable.EyeTooDamaged = false;
                AdjustBlindSources(uid, -1, blindable);
            }

            blindable.EyeDamage = Math.Clamp(blindable.EyeDamage, 0, 8);
        }
    }

    // I have no idea why blurry vision needs this but blindness doesn't
    [Serializable, NetSerializable]
    public sealed class BlurryVisionComponentState : ComponentState
    {
        public float Magnitude;
        public BlurryVisionComponentState(float magnitude)
        {
            Magnitude = magnitude;
        }
    }
}
