using System.Threading.Tasks;
using Content.Server.Weapon.Melee.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Interaction;
using Content.Shared.Mining;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Mining.Components
{
    [RegisterComponent]
    public class AsteroidRockComponent : Component, IInteractUsing
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        public override string Name => "AsteroidRock";
        private static readonly string[] SpriteStates = {"0", "1", "2", "3", "4"};

        // TODO PROTOTYPE Replace this datafield variable with prototype references, once they are supported.
        [DataField("damageType")]
        private readonly string _damageTypeID = "Blunt"!;
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageTypePrototype DamageType = default!;

        protected override void Initialize()
        {
            base.Initialize();
            DamageType = IoCManager.Resolve<IPrototypeManager>().Index<DamageTypePrototype>(_damageTypeID);
            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(AsteroidRockVisuals.State, _random.Pick(SpriteStates));
            }
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            var item = eventArgs.Using;
            if (!item.TryGetComponent(out MeleeWeaponComponent? meleeWeaponComponent))
                return false;

            Owner.GetComponent<IDamageableComponent>().TryChangeDamage(DamageType, meleeWeaponComponent.Damage);

            if (!item.TryGetComponent(out PickaxeComponent? pickaxeComponent))
                return true;

            SoundSystem.Play(Filter.Pvs(Owner), pickaxeComponent.MiningSound.GetSound(), Owner, AudioParams.Default);
            return true;
        }
    }
}
