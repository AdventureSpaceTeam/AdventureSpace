﻿using Content.Client.Construction;
using Content.Client.GameObjects.Components.Construction;
using Content.Client.UserInterface;
using Content.Shared.Input;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Player;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.GameObjects.EntitySystems
{
    public sealed class ConstructorSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IGameHud _gameHud;
        [Dependency] private readonly IPlayerManager _playerManager;
        [Dependency] private readonly IEntityManager _entityManager;
#pragma warning restore 649

        public override void Initialize()
        {
            base.Initialize();

            var inputSys = EntitySystemManager.GetEntitySystem<InputSystem>();
            inputSys.BindMap.BindFunction(ContentKeyFunctions.OpenCraftingMenu,
                new PointerInputCmdHandler(HandleOpenCraftingMenu));

            inputSys.BindMap.BindFunction(EngineKeyFunctions.Use,
                new PointerInputCmdHandler(HandleUse));
        }

        private bool HandleOpenCraftingMenu(in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            if (_playerManager.LocalPlayer.ControlledEntity == null
                || !_playerManager.LocalPlayer.ControlledEntity.TryGetComponent(out ConstructorComponent constructor))
            {
                return false;
            }

            var menu = constructor.ConstructionMenu;

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

            return true;
        }

        private bool HandleUse(in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            if (!args.EntityUid.IsValid() || !args.EntityUid.IsClientSide())
                return false;

            var entity = _entityManager.GetEntity(args.EntityUid);

            if (!entity.TryGetComponent(out ConstructionGhostComponent ghostComp))
                return false;

            ghostComp.Master.TryStartConstruction(ghostComp.GhostID);
            return true;

        }

        private void _setOpenValue(ConstructionMenu menu, bool value)
        {
            if (value)
            {
                _gameHud.CraftingButtonDown = true;
                menu.OpenCentered();
            }
            else
            {
                _gameHud.CraftingButtonDown = false;
                menu.Close();
            }
        }
    }
}
