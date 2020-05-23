﻿using Content.Server.GameObjects.EntitySystems;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Research;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Research
{
    [RegisterComponent]
    public class ResearchClientComponent : SharedResearchClientComponent, IActivate
    {
        // TODO: Create GUI for changing RD server.

        private BoundUserInterface _userInterface;

#pragma warning disable 649
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
#pragma warning restore 649

        public bool ConnectedToServer => Server != null;

        [ViewVariables(VVAccess.ReadOnly)]
        public ResearchServerComponent Server { get; set; }

        public bool RegisterServer(ResearchServerComponent server)
        {
            var result = server != null && server.RegisterClient(this);
            return result;
        }

        public void UnregisterFromServer()
        {
            Server?.UnregisterClient(this);
        }

        public override void Initialize()
        {
            base.Initialize();
            // For now it just registers on the first server it can find.
            var servers = _entitySystemManager.GetEntitySystem<ResearchSystem>().Servers;
            if(servers.Count > 0)
                RegisterServer(servers[0]);
            _userInterface = Owner.GetComponent<ServerUserInterfaceComponent>().GetBoundUserInterface(ResearchClientUiKey.Key);
            _userInterface.OnReceiveMessage += UserInterfaceOnOnReceiveMessage;
        }

        public void OpenUserInterface(IPlayerSession session)
        {
            UpdateUserInterface();
            _userInterface.Open(session);
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent actor))
                return;

            OpenUserInterface(actor.playerSession);
        }

        public void UpdateUserInterface()
        {
            _userInterface?.SetState(GetNewUiState());
        }

        private ResearchClientBoundInterfaceState GetNewUiState()
        {
            var rd = _entitySystemManager.GetEntitySystem<ResearchSystem>();

            return new ResearchClientBoundInterfaceState(rd.Servers.Count, rd.GetServerNames(),
                rd.GetServerIds(), ConnectedToServer ? Server.Id : -1);
        }

        private void UserInterfaceOnOnReceiveMessage(ServerBoundUserInterfaceMessage msg)
        {
            switch (msg.Message)
            {
                case ResearchClientSyncMessage _:
                    UpdateUserInterface();
                    break;

                case ResearchClientServerSelectedMessage selectedMessage:
                    UnregisterFromServer();
                    RegisterServer(_entitySystemManager.GetEntitySystem<ResearchSystem>().GetServerById(selectedMessage.ServerId));
                    UpdateUserInterface();
                    break;

                case ResearchClientServerDeselectedMessage _:
                    UnregisterFromServer();
                    UpdateUserInterface();
                    break;
            }
        }

        /// <inheritdoc />
        protected override void Shutdown()
        {
            base.Shutdown();
            UnregisterFromServer();

        }
    }
}
