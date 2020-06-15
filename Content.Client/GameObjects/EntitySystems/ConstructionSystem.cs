﻿using System.Collections.Generic;
using Content.Client.Construction;
using Content.Client.GameObjects.Components.Construction;
using Content.Client.UserInterface;
using Content.Shared.Construction;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Player;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects.EntitySystems
{
    /// <summary>
    /// The client-side implementation of the construction system, which is used for constructing entities in game.
    /// </summary>
    [UsedImplicitly]
    public class ConstructionSystem : Shared.GameObjects.EntitySystems.ConstructionSystem
    {
#pragma warning disable 649
        [Dependency] private readonly IGameHud _gameHud;
        [Dependency] private readonly IPlayerManager _playerManager;
        [Dependency] private readonly IEntityManager _entityManager;
#pragma warning restore 649

        private int _nextId;
        private readonly Dictionary<int, ConstructionGhostComponent> _ghosts = new Dictionary<int, ConstructionGhostComponent>();
        private ConstructionMenu _constructionMenu;

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PlayerAttachSysMessage>(HandlePlayerAttached);
            SubscribeNetworkEvent<AckStructureConstructionMessage>(HandleAckStructure);

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.OpenCraftingMenu,
                    new PointerInputCmdHandler(HandleOpenCraftingMenu))
                .Bind(EngineKeyFunctions.Use,
                    new PointerInputCmdHandler(HandleUse))
                .Register<ConstructionSystem>();
        }

        private void HandleAckStructure(AckStructureConstructionMessage msg)
        {
            ClearGhost(msg.GhostId);
        }

        private void HandlePlayerAttached(PlayerAttachSysMessage msg)
        {
            if (msg.AttachedEntity == null)
            {
                _gameHud.CraftingButtonVisible = false;
                return;
            }

            if (_constructionMenu == null)
            {
                _constructionMenu = new ConstructionMenu();
                _constructionMenu.OnClose += () => _gameHud.CraftingButtonDown = false;
            }

            _gameHud.CraftingButtonVisible = true;
            _gameHud.CraftingButtonToggled = b =>
            {
                if (b)
                {
                    _constructionMenu.Open();
                }
                else
                {
                    _constructionMenu.Close();
                }
            };
        }

        /// <inheritdoc />
        public override void Shutdown()
        {
            _constructionMenu?.Dispose();

            CommandBinds.Unregister<ConstructionSystem>();
            base.Shutdown();
        }

        private bool HandleOpenCraftingMenu(in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            if (_playerManager.LocalPlayer.ControlledEntity == null)
            {
                return false;
            }

            var menu = _constructionMenu;

            if (menu.IsOpen)
            {
                if (menu.IsAtFront())
                {
                    SetOpenValue(menu, false);
                }
                else
                {
                    menu.MoveToFront();
                }
            }
            else
            {
                SetOpenValue(menu, true);
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

            TryStartConstruction(ghostComp.GhostID);
            return true;

        }

        private void SetOpenValue(ConstructionMenu menu, bool value)
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

        /// <summary>
        /// Creates a construction ghost at the given location.
        /// </summary>
        public void SpawnGhost(ConstructionPrototype prototype, GridCoordinates loc, Direction dir)
        {
            var ghost = _entityManager.SpawnEntity("constructionghost", loc);
            var comp = ghost.GetComponent<ConstructionGhostComponent>();
            comp.Prototype = prototype;
            comp.GhostID = _nextId++;
            ghost.Transform.LocalRotation = dir.ToAngle();
            var sprite = ghost.GetComponent<SpriteComponent>();
            sprite.LayerSetSprite(0, prototype.Icon);
            sprite.LayerSetVisible(0, true);

            _ghosts.Add(comp.GhostID, comp);
        }

        private void TryStartConstruction(int ghostId)
        {
            var ghost = _ghosts[ghostId];
            var transform = ghost.Owner.Transform;
            var msg = new TryStartStructureConstructionMessage(transform.GridPosition, ghost.Prototype.ID, transform.LocalRotation, ghostId);
            RaiseNetworkEvent(msg);
        }

        /// <summary>
        /// Starts constructing an item underneath the attached entity.
        /// </summary>
        public void TryStartItemConstruction(string prototypeName)
        {
            RaiseNetworkEvent(new TryStartItemConstructionMessage(prototypeName));
        }

        /// <summary>
        /// Removes a construction ghost entity with the given ID.
        /// </summary>
        public void ClearGhost(int ghostId)
        {
            if (_ghosts.TryGetValue(ghostId, out var ghost))
            {
                ghost.Owner.Delete();
                _ghosts.Remove(ghostId);
            }
        }
    }
}
