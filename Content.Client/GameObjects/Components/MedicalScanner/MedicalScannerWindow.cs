using System.Text;
using Content.Shared.Damage;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using static Content.Shared.GameObjects.Components.Medical.SharedMedicalScannerComponent;

namespace Content.Client.GameObjects.Components.MedicalScanner
{
    public class MedicalScannerWindow : SS14Window
    {
        protected override Vector2? CustomSize => (485, 90);

        public void Populate(MedicalScannerBoundUserInterfaceState state)
        {
            Contents.RemoveAllChildren();
            var text = new StringBuilder();

            if (!state.Entity.HasValue ||
                !state.HasDamage() ||
                !IoCManager.Resolve<IEntityManager>().TryGetEntity(state.Entity.Value, out var entity))
            {
                text.Append(Loc.GetString("No patient data."));
            }
            else
            {
                text.Append($"{entity.Name}{Loc.GetString("'s health:")}\n");

                foreach (var (@class, classAmount) in state.DamageClasses)
                {
                    text.Append($"\n{Loc.GetString("{0}: {1}", @class, classAmount)}");

                    foreach (var type in @class.ToTypes())
                    {
                        if (!state.DamageTypes.TryGetValue(type, out var typeAmount))
                        {
                            continue;
                        }

                        text.Append($"\n- {Loc.GetString("{0}: {1}", type, typeAmount)}");
                    }

                    text.Append("\n");
                }
            }

            Contents.AddChild(new Label() {Text = text.ToString()});
        }
    }
}
