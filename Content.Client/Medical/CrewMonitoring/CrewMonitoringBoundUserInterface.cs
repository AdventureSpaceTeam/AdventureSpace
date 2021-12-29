﻿using Content.Shared.Medical.CrewMonitoring;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.Medical.CrewMonitoring
{
    public class CrewMonitoringBoundUserInterface : BoundUserInterface
    {
        private CrewMonitoringWindow? _menu;

        public CrewMonitoringBoundUserInterface([NotNull] ClientUserInterfaceComponent owner, [NotNull] object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            _menu = new CrewMonitoringWindow();
            _menu.OpenCentered();
            _menu.OnClose += Close;
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            switch (state)
            {
                case CrewMonitoringState st:
                    _menu?.ShowSensors(st.Sensors);
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            _menu?.Dispose();
        }
    }
}
