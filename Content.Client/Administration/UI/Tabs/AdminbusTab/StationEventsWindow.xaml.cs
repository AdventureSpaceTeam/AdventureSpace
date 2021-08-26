using System.Collections.Generic;
using System.Linq;
using Content.Client.StationEvents.Managers;
using JetBrains.Annotations;
using Robust.Client.AutoGenerated;
using Robust.Client.Console;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Client.Administration.UI.Tabs.AdminbusTab
{
    [GenerateTypedNameReferences]
    [UsedImplicitly]
    public partial class StationEventsWindow : SS14Window
    {
        private List<string>? _data;

        [Dependency]
        private readonly IStationEventManager _eventManager = default!;

        public StationEventsWindow()
        {
            IoCManager.InjectDependencies(this);

            MinSize = SetSize = (300, 200);
            RobustXamlLoader.Load(this);
        }

        protected override void EnteredTree()
        {
            _eventManager.OnStationEventsReceived += OnStationEventsReceived;
            _eventManager.RequestEvents();

            EventsOptions.AddItem(Loc.GetString("station-events-window-not-loaded-text"));
        }

        private void OnStationEventsReceived()
        {
            // fill events dropdown
            _data = _eventManager.StationEvents.ToList();
            EventsOptions.Clear();
            foreach (var stationEvent in _data)
            {
                EventsOptions.AddItem(stationEvent);
            }
            EventsOptions.AddItem(Loc.GetString("station-events-window-random-text"));

            // Enable all UI elements
            EventsOptions.Disabled = false;
            PauseButton.Disabled = false;
            ResumeButton.Disabled = false;
            SubmitButton.Disabled = false;

            // Subscribe to UI events
            EventsOptions.OnItemSelected += eventArgs => EventsOptions.SelectId(eventArgs.Id);
            PauseButton.OnPressed += PauseButtonOnOnPressed;
            ResumeButton.OnPressed += ResumeButtonOnOnPressed;
            SubmitButton.OnPressed += SubmitButtonOnOnPressed;
        }

        private static void PauseButtonOnOnPressed(BaseButton.ButtonEventArgs obj)
        {
            IoCManager.Resolve<IClientConsoleHost>().ExecuteCommand("events pause");
        }

        private static void ResumeButtonOnOnPressed(BaseButton.ButtonEventArgs obj)
        {
            IoCManager.Resolve<IClientConsoleHost>().ExecuteCommand("events resume");
        }

        private void SubmitButtonOnOnPressed(BaseButton.ButtonEventArgs obj)
        {
            if (_data == null)
                return;

            // random is always last option
            var id = EventsOptions.SelectedId;
            var selectedEvent = id < _data.Count ? _data[id] : "random";

            IoCManager.Resolve<IClientConsoleHost>().ExecuteCommand($"events run \"{selectedEvent}\"");
        }
    }
}
