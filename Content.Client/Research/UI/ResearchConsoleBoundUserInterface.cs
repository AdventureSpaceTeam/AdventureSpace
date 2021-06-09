using Content.Shared.Research.Prototypes;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using static Content.Shared.Research.Components.SharedResearchConsoleComponent;

namespace Content.Client.Research.UI
{
    [UsedImplicitly]
    public class ResearchConsoleBoundUserInterface : BoundUserInterface
    {
        public int Points { get; private set; }
        public int PointsPerSecond { get; private set; }
        private ResearchConsoleMenu? _consoleMenu;
        private TechnologyDatabaseComponent? _technologyDatabase;

        public ResearchConsoleBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
            SendMessage(new ConsoleServerSyncMessage());
        }

        protected override void Open()
        {
            base.Open();

            if (!Owner.Owner.TryGetComponent(out _technologyDatabase)) return;

            _consoleMenu = new ResearchConsoleMenu(this);

            _consoleMenu.OnClose += Close;

            _consoleMenu.ServerSyncButton.OnPressed += (_) =>
            {
                SendMessage(new ConsoleServerSyncMessage());
            };

            _consoleMenu.ServerSelectionButton.OnPressed += (_) =>
            {
                SendMessage(new ConsoleServerSelectionMessage());
            };

            _consoleMenu.UnlockButton.OnPressed += (_) =>
            {
                if (_consoleMenu.TechnologySelected != null)
                {
                    SendMessage(new ConsoleUnlockTechnologyMessage(_consoleMenu.TechnologySelected.ID));
                }
            };

            _consoleMenu.OpenCentered();

            _technologyDatabase.OnDatabaseUpdated += _consoleMenu.Populate;
        }

        public bool IsTechnologyUnlocked(TechnologyPrototype technology)
        {
            return _technologyDatabase?.IsTechnologyUnlocked(technology) ?? false;
        }

        public bool CanUnlockTechnology(TechnologyPrototype technology)
        {
            return _technologyDatabase?.CanUnlockTechnology(technology) ?? false;
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            var castState = (ResearchConsoleBoundInterfaceState)state;
            Points = castState.Points;
            PointsPerSecond = castState.PointsPerSecond;
            // We update the user interface here.
            _consoleMenu?.PopulatePoints();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _consoleMenu?.Dispose();
        }
    }
}
