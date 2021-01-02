using Content.Server.GameObjects.Components.Mobs;
using Content.Server.Utility;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Nutrition;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.ViewVariables;
using Serilog;

namespace Content.Server.GameObjects.Components.Nutrition
{
    [RegisterComponent]
    public class CreamPiedComponent : SharedCreamPiedComponent, IReagentReaction, IThrowCollide
    {
        private bool _creamPied;

        [ViewVariables]
        public bool CreamPied
        {
            get => _creamPied;
            private set
            {
                _creamPied = value;
                if (Owner.TryGetComponent(out AppearanceComponent appearance))
                {
                    appearance.SetData(CreamPiedVisuals.Creamed, CreamPied);
                }
            }
        }

        public void Wash()
        {
            if(CreamPied)
                CreamPied = false;
        }

        public ReagentUnit ReagentReactTouch(ReagentPrototype reagent, ReagentUnit volume)
        {
            switch (reagent.ID)
            {
                case "chem.SpaceCleaner":
                case "chem.Water":
                    Wash();
                    break;
            }

            return ReagentUnit.Zero;
        }

        public void HitBy(ThrowCollideEventArgs eventArgs)
        {
            if (!eventArgs.Thrown.HasComponent<CreamPieComponent>() || CreamPied) return;

            CreamPied = true;
            Owner.PopupMessage(Loc.GetString("You have been creamed by {0:theName}!", eventArgs.Thrown));
            Owner.PopupMessageOtherClients(Loc.GetString("{0:theName} has been creamed by {1:theName}!", Owner, eventArgs.Thrown));

            if (Owner.TryGetComponent(out StunnableComponent stun))
            {
                stun.Paralyze(1f);
            }
        }
    }
}
