using System.Numerics;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.Administration.UI;

[GenerateTypedNameReferences]
public sealed partial class AdminMenuWindow : DefaultWindow
{
    public event Action? OnDisposed;

    public AdminMenuWindow()
    {
        MinSize = new Vector2(650, 250);
        Title = Loc.GetString("admin-menu-title");
        RobustXamlLoader.Load(this);
        MasterTabContainer.SetTabTitle((int) TabIndex.Admin, Loc.GetString("admin-menu-admin-tab"));
        MasterTabContainer.SetTabTitle((int) TabIndex.Adminbus, Loc.GetString("admin-menu-adminbus-tab"));
        MasterTabContainer.SetTabTitle((int) TabIndex.Atmos, Loc.GetString("admin-menu-atmos-tab"));
        MasterTabContainer.SetTabTitle((int) TabIndex.Round, Loc.GetString("admin-menu-round-tab"));
        MasterTabContainer.SetTabTitle((int) TabIndex.Server, Loc.GetString("admin-menu-server-tab"));
        MasterTabContainer.SetTabTitle((int) TabIndex.PanicBunker, Loc.GetString("admin-menu-panic-bunker-tab"));
        /*
         * TODO: Remove baby jail code once a more mature gateway process is established. This code is only being issued as a stopgap to help with potential tiding in the immediate future.
         */
        MasterTabContainer.SetTabTitle((int) TabIndex.BabyJail, Loc.GetString("admin-menu-baby-jail-tab"));
        MasterTabContainer.SetTabTitle((int) TabIndex.Players, Loc.GetString("admin-menu-players-tab"));
        MasterTabContainer.SetTabTitle((int) TabIndex.Objects, Loc.GetString("admin-menu-objects-tab"));
        MasterTabContainer.OnTabChanged += OnTabChanged;
    }

    private void OnTabChanged(int tabIndex)
    {
        var tabEnum = (TabIndex)tabIndex;
        if (tabEnum == TabIndex.Objects)
            ObjectsTabControl.RefreshObjectList();
    }

    protected override void Dispose(bool disposing)
    {
        OnDisposed?.Invoke();
        base.Dispose(disposing);
        OnDisposed = null;
    }

    private enum TabIndex
    {
        Admin = 0,
        Adminbus,
        Atmos,
        Round,
        Server,
        PanicBunker,
        BabyJail,
        Players,
        Objects,
    }
}
