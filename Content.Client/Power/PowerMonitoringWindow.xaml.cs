using System;
using System.Linq;
using Content.Client.Computer;
using Content.Client.IoC;
using Content.Shared.Power;
using JetBrains.Annotations;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Prototypes;

namespace Content.Client.Power;

[GenerateTypedNameReferences]
public sealed partial class PowerMonitoringWindow : DefaultWindow, IComputerWindow<PowerMonitoringConsoleBoundInterfaceState>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public PowerMonitoringWindow()
    {
        RobustXamlLoader.Load(this);
        SetSize = MinSize = (300, 450);
        IoCManager.InjectDependencies(this);
        MasterTabContainer.SetTabTitle(0, Loc.GetString("power-monitoring-window-tab-sources"));
        MasterTabContainer.SetTabTitle(1, Loc.GetString("power-monitoring-window-tab-loads"));
    }

    public void UpdateState(PowerMonitoringConsoleBoundInterfaceState scc)
    {
        UpdateList(TotalSourcesNum, scc.TotalSources, SourcesList, scc.Sources);
        var loads = scc.Loads;
        if (!ShowInactiveConsumersCheckBox.Pressed)
        {
            // Not showing inactive consumers, so hiding them.
            // This means filtering out loads that are not either:
            // + Batteries (always important)
            // + Meaningful (size above 0)
            loads = loads.Where(a => a.IsBattery || (a.Size > 0.0f)).ToArray();
        }
        UpdateList(TotalLoadsNum, scc.TotalLoads, LoadsList, loads);
    }

    public void UpdateList(Label number, double numberVal, ItemList list, PowerMonitoringConsoleEntry[] listVal)
    {
        number.Text = Loc.GetString("power-monitoring-window-value", ("value", numberVal));
        // This magic is important to prevent scrolling issues.
        while (list.Count > listVal.Length)
        {
            list.RemoveAt(list.Count - 1);
        }
        while (list.Count < listVal.Length)
        {
            list.AddItem("YOU SHOULD NEVER SEE THIS (REALLY!)", null, false);
        }
        // Now overwrite the items properly...
        for (var i = 0; i < listVal.Length; i++)
        {
            var ent = listVal[i];
            _prototypeManager.TryIndex(ent.IconEntityPrototypeId, out EntityPrototype? entityPrototype);
            IRsiStateLike? iconState = null;
            if (entityPrototype != null)
                iconState = SpriteComponent.GetPrototypeIcon(entityPrototype, StaticIoC.ResC);
            var icon = iconState?.GetFrame(RSI.State.Direction.South, 0);
            var item = list[i];
            item.Text = $"{ent.NameLocalized} {Loc.GetString("power-monitoring-window-value", ("value", ent.Size))}";
            item.Icon = icon;
        }
    }
}

[UsedImplicitly]
public sealed class PowerMonitoringConsoleBoundUserInterface : ComputerBoundUserInterface<PowerMonitoringWindow, PowerMonitoringConsoleBoundInterfaceState>
{
    public PowerMonitoringConsoleBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey) {}
}

