using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using static Content.Shared.Research.Components.SharedResearchClientComponent;

namespace Content.Client.Research.UI
{
    public class ResearchClientBoundUserInterface : BoundUserInterface
    {
        private ResearchClientServerSelectionMenu? _menu;

        public ResearchClientBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
            SendMessage(new ResearchClientSyncMessage());
        }

        protected override void Open()
        {
            base.Open();

            _menu = new ResearchClientServerSelectionMenu(this);
            _menu.OnClose += Close;
            _menu.OpenCentered();
        }

        public void SelectServer(int serverId)
        {
            SendMessage(new ResearchClientServerSelectedMessage(serverId));
        }

        public void DeselectServer()
        {
            SendMessage(new ResearchClientServerDeselectedMessage());
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (state is not ResearchClientBoundInterfaceState rState) return;
            _menu?.Populate(rState.ServerCount, rState.ServerNames, rState.ServerIds, rState.SelectedServerId);
        }
    }
}
