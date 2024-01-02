using Content.Client._Alteros.NewLife;
using Content.Client.Eui;
using Content.Client.Players.PlayTimeTracking;
using Content.Shared.Eui;
using Content.Shared.NewLife;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;


namespace Content.Client.NewLife
{
    [UsedImplicitly]
    public sealed class NewLifeEui : BaseEui
    {
        private readonly NewLifeWindow _window;

        public NewLifeEui()
        {
            _window = new NewLifeWindow(IoCManager.Resolve<IGameTiming>());

            _window.SpawnRequested += () =>
            {
                SendMessage(new NewLifeRequestSpawnMessage(_window.GetSelectedCharacter(), _window.GetSelectedRole()));
            };

            _window.OnClose += () =>
            {
                SendMessage(new CloseEuiMessage());
            };
        }

        public override void Opened()
        {
            base.Opened();
            _window.OpenCentered();
        }

        public override void Closed()
        {
            base.Closed();
            _window.Close();
        }

        public override void HandleState(EuiStateBase state)
        {
            base.HandleState(state);

            if (state is not NewLifeEuiState ghostState)
                return;

            var entityManager = IoCManager.Resolve<IEntityManager>();
            var sysManager = entityManager.EntitySysManager;
            var spriteSystem = sysManager.GetEntitySystem<SpriteSystem>();
            var requirementsManager = IoCManager.Resolve<JobRequirementsManager>();

            _window.UpdateCharactersList(ghostState.Characters, ghostState.UsedCharactersForRespawn);
            _window.UpdateRolesList(ghostState.Roles);
            _window.UpdateNextRespawn(ghostState.NextRespawnTime);
        }
    }
}
