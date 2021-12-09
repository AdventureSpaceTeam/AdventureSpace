using Content.Server.Nutrition.Components;
using Content.Server.Popups;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Nutrition.EntitySystems
{
    /// <summary>
    /// Handles usage of the utensils on the food items
    /// </summary>
    internal class UtensilSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly FoodSystem _foodSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<UtensilComponent, AfterInteractEvent>(OnAfterInteract);
        }

        /// <summary>
        /// Clicked with utensil
        /// </summary>
        private void OnAfterInteract(EntityUid uid, UtensilComponent component, AfterInteractEvent ev)
        {
            if (ev.Target == null)
                return;

            if (TryUseUtensil(ev.User, ev.Target.Value, component))
                ev.Handled = true;
        }

        private bool TryUseUtensil(EntityUid user, EntityUid target, UtensilComponent component)
        {
            if (!EntityManager.TryGetComponent(target, out FoodComponent food))
                return false;

            //Prevents food usage with a wrong utensil
            if ((food.Utensil & component.Types) == 0)
            {
                _popupSystem.PopupEntity(Loc.GetString("food-system-wrong-utensil", ("food", food.Owner), ("utensil", component.Owner)), user, Filter.Entities(user));
                return false;
            }

            if (!user.InRangeUnobstructed(target, popup: true))
                return false;

            return _foodSystem.TryUseFood(target, user);
        }

        /// <summary>
        /// Attempt to break the utensil after interaction.
        /// </summary>
        /// <param name="uid">Utensil.</param>
        /// <param name="userUid">User of the utensil.</param>
        public void TryBreak(EntityUid uid, EntityUid userUid, UtensilComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            if (_robustRandom.Prob(component.BreakChance))
            {
                SoundSystem.Play(Filter.Pvs(userUid), component.BreakSound.GetSound(), userUid, AudioParams.Default.WithVolume(-2f));
                EntityManager.DeleteEntity(component.Owner);
            }
        }
    }
}
