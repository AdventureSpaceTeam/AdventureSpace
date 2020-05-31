using Content.Client.UserInterface;
using Content.Shared.Input;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Player;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;

namespace Content.Client.GameObjects.EntitySystems
{
    public sealed class ClientInventorySystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IGameHud _gameHud;
        [Dependency] private readonly IPlayerManager _playerManager;
#pragma warning restore 649

        public override void Initialize()
        {
            base.Initialize();

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.OpenInventoryMenu,
                    InputCmdHandler.FromDelegate(s => HandleOpenInventoryMenu()))
                .Register<ClientInventorySystem>();
        }

        public override void Shutdown()
        {
            CommandBinds.Unregister<ClientInventorySystem>();
            base.Shutdown();
        }

        private void HandleOpenInventoryMenu()
        {
            if (_playerManager.LocalPlayer.ControlledEntity == null
                || !_playerManager.LocalPlayer.ControlledEntity.TryGetComponent(out ClientInventoryComponent clientInventory))
            {
                return;
            }

            var menu = clientInventory.InterfaceController.Window;

            if (menu.IsOpen)
            {
                if (menu.IsAtFront())
                {
                    _setOpenValue(menu, false);
                }
                else
                {
                    menu.MoveToFront();
                }
            }
            else
            {
                _setOpenValue(menu, true);
            }
        }

        private void _setOpenValue(SS14Window menu, bool value)
        {
            if (value)
            {
                _gameHud.InventoryButtonDown = true;
                menu.OpenCentered();
            }
            else
            {
                _gameHud.InventoryButtonDown = false;
                menu.Close();
            }
        }
    }
}
