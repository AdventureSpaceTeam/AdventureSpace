﻿using System.Collections.Generic;
using Content.Client.Construction;
using Content.Client.UserInterface;
using Content.Shared.Construction;
using Content.Shared.GameObjects.Components.Construction;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;

namespace Content.Client.GameObjects.Components.Construction
{
    [RegisterComponent]
    public class ConstructorComponent : SharedConstructorComponent
    {
#pragma warning disable 649
        [Dependency] private readonly IGameHud _gameHud;
#pragma warning restore 649

        private int nextId;
        private readonly Dictionary<int, ConstructionGhostComponent> Ghosts = new Dictionary<int, ConstructionGhostComponent>();
        public ConstructionMenu ConstructionMenu { get; private set; }

        public override void Initialize()
        {
            base.Initialize();

            Owner.GetComponent<ITransformComponent>();
        }

        public override void HandleMessage(ComponentMessage message, IComponent component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case PlayerAttachedMsg _:
                    if (ConstructionMenu == null)
                    {
                        ConstructionMenu = new ConstructionMenu {Owner = this};
                        ConstructionMenu.OnClose += () => _gameHud.CraftingButtonDown = false;
                    }

                    _gameHud.CraftingButtonVisible = true;
                    _gameHud.CraftingButtonToggled = b =>
                    {
                        if (b)
                        {
                            ConstructionMenu.Open();
                        }
                        else
                        {
                            ConstructionMenu.Close();
                        }
                    };
                    break;

                case PlayerDetachedMsg _:
                    _gameHud.CraftingButtonVisible = false;
                    break;

                default:
                    break;
            }
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel channel, ICommonSession session = null)
        {
            base.HandleNetworkMessage(message, channel, session);

            switch (message)
            {
                case AckStructureConstructionMessage ackMsg:
                    ClearGhost(ackMsg.Ack);
                    break;

                default:
                    break;
            }
        }

        public override void OnRemove()
        {
            ConstructionMenu?.Dispose();
        }

        public void SpawnGhost(ConstructionPrototype prototype, GridCoordinates loc, Direction dir)
        {
            var entMgr = IoCManager.Resolve<IClientEntityManager>();
            var ghost = entMgr.SpawnEntity("constructionghost", loc);
            var comp = ghost.GetComponent<ConstructionGhostComponent>();
            comp.Prototype = prototype;
            comp.Master = this;
            comp.GhostID = nextId++;
            ghost.GetComponent<ITransformComponent>().LocalRotation = dir.ToAngle();
            var sprite = ghost.GetComponent<SpriteComponent>();
            sprite.LayerSetSprite(0, prototype.Icon);
            sprite.LayerSetVisible(0, true);

            Ghosts.Add(comp.GhostID, comp);
        }

        public void TryStartConstruction(int ghostId)
        {
            var ghost = Ghosts[ghostId];
            var transform = ghost.Owner.GetComponent<ITransformComponent>();
            var msg = new TryStartStructureConstructionMessage(transform.GridPosition, ghost.Prototype.ID, transform.LocalRotation, ghostId);
            SendNetworkMessage(msg);
        }

        public void ClearGhost(int ghostId)
        {
            if (Ghosts.TryGetValue(ghostId, out var ghost))
            {
                ghost.Owner.Delete();
                Ghosts.Remove(ghostId);
            }
        }
    }
}
