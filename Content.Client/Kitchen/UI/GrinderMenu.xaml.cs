using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Kitchen;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;

namespace Content.Client.Kitchen.UI
{
    [GenerateTypedNameReferences]
    public sealed partial class GrinderMenu : DefaultWindow
    {
        private readonly IEntityManager _entityManager;
        private readonly IPrototypeManager _prototypeManager ;
        private readonly ReagentGrinderBoundUserInterface _owner;

        private readonly Dictionary<int, EntityUid> _chamberVisualContents = new();

        public GrinderMenu(ReagentGrinderBoundUserInterface owner, IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
            RobustXamlLoader.Load(this);
            _entityManager = entityManager;
            _prototypeManager = prototypeManager;
            _owner = owner;
            GrindButton.OnPressed += owner.StartGrinding;
            JuiceButton.OnPressed += owner.StartJuicing;
            ChamberContentBox.EjectButton.OnPressed += owner.EjectAll;
            BeakerContentBox.EjectButton.OnPressed += owner.EjectBeaker;
            ChamberContentBox.BoxContents.OnItemSelected += OnChamberBoxContentsItemSelected;
            BeakerContentBox.BoxContents.SelectMode = ItemList.ItemListSelectMode.None;
        }

        private void OnChamberBoxContentsItemSelected(ItemList.ItemListSelectedEventArgs args)
        {
            _owner.EjectChamberContent(_chamberVisualContents[args.ItemIndex]);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _chamberVisualContents.Clear();
            GrindButton.OnPressed -= _owner.StartGrinding;
            JuiceButton.OnPressed -= _owner.StartJuicing;
            ChamberContentBox.EjectButton.OnPressed -= _owner.EjectAll;
            BeakerContentBox.EjectButton.OnPressed -= _owner.EjectBeaker;
            ChamberContentBox.BoxContents.OnItemSelected -= OnChamberBoxContentsItemSelected;
        }

        public void UpdateState(ReagentGrinderInterfaceState state)
        {
            BeakerContentBox.EjectButton.Disabled = !state.HasBeakerIn;
            ChamberContentBox.EjectButton.Disabled = state.ChamberContents.Length <= 0;
            GrindButton.Disabled = !state.CanGrind || !state.Powered;
            JuiceButton.Disabled = !state.CanJuice || !state.Powered;
            RefreshContentsDisplay(state.ReagentQuantities, state.ChamberContents, state.HasBeakerIn);
        }

        public void HandleMessage(BoundUserInterfaceMessage message)
        {
            switch (message)
            {
                case ReagentGrinderWorkStartedMessage workStarted:
                    GrindButton.Disabled = true;
                    GrindButton.Modulate = workStarted.GrinderProgram == GrinderProgram.Grind ? Color.Green : Color.White;
                    JuiceButton.Disabled = true;
                    JuiceButton.Modulate = workStarted.GrinderProgram == GrinderProgram.Juice ? Color.Green : Color.White;
                    BeakerContentBox.EjectButton.Disabled = true;
                    ChamberContentBox.EjectButton.Disabled = true;
                    break;
                case ReagentGrinderWorkCompleteMessage:
                    GrindButton.Disabled = false;
                    JuiceButton.Disabled = false;
                    GrindButton.Modulate = Color.White;
                    JuiceButton.Modulate = Color.White;
                    BeakerContentBox.EjectButton.Disabled = false;
                    ChamberContentBox.EjectButton.Disabled = false;
                    break;
            }
        }

        private void RefreshContentsDisplay(IList<Solution.ReagentQuantity>? reagents, IReadOnlyList<EntityUid> containedSolids, bool isBeakerAttached)
        {
            //Refresh chamber contents
            _chamberVisualContents.Clear();

            ChamberContentBox.BoxContents.Clear();
            foreach (var entity in containedSolids)
            {
                if (!_entityManager.EntityExists(entity))
                {
                    return;
                }

                var texture = _entityManager.GetComponent<SpriteComponent>(entity).Icon?.Default;

                var solidItem = ChamberContentBox.BoxContents.AddItem(_entityManager.GetComponent<MetaDataComponent>(entity).EntityName, texture);
                var solidIndex = ChamberContentBox.BoxContents.IndexOf(solidItem);
                _chamberVisualContents.Add(solidIndex, entity);
            }

            //Refresh beaker contents
            BeakerContentBox.BoxContents.Clear();
            //if no beaker is attached use this guard to prevent hitting a null reference.
            if (!isBeakerAttached || reagents == null)
            {
                return;
            }

            //Looks like we have a beaker attached.
            if (reagents.Count <= 0)
            {
                BeakerContentBox.BoxContents.AddItem(Loc.GetString("grinder-menu-beaker-content-box-is-empty"));
            }
            else
            {
                foreach (var reagent in reagents)
                {
                    var reagentName = _prototypeManager.TryIndex(reagent.ReagentId, out ReagentPrototype? proto) ? Loc.GetString($"{reagent.Quantity} {proto.LocalizedName}") : "???";
                    BeakerContentBox.BoxContents.AddItem(reagentName);
                }
            }
        }
    }
}
