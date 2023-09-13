using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Timing;

namespace Content.Client.Instruments.UI;

[GenerateTypedNameReferences]
public sealed partial class BandMenu : DefaultWindow
{
    private readonly InstrumentBoundUserInterface _owner;

    public BandMenu(InstrumentBoundUserInterface owner) : base()
    {
        RobustXamlLoader.Load(this);

        _owner = owner;
        BandList.OnItemSelected += OnItemSelected;
        RefreshButton.OnPressed += OnRefreshPressed;
    }

    private void OnRefreshPressed(BaseButton.ButtonEventArgs obj)
    {
        _owner.RefreshBands();
    }

    private void OnItemSelected(ItemList.ItemListSelectedEventArgs args)
    {
        _owner.Instruments.SetMaster(_owner.Owner, (EntityUid)args.ItemList[args.ItemIndex].Metadata!);
        BandList.Clear();
        Timer.Spawn(0, Close);
    }

    public void Populate((NetEntity, string)[] nearby, IEntityManager entManager)
    {
        BandList.Clear();

        foreach (var (nent, name) in nearby)
        {
            var uid = entManager.GetEntity(nent);
            var item = BandList.AddItem(name, null, true, uid);
            item.Selected = _owner.Instrument?.Master == uid;
        }
    }
}
