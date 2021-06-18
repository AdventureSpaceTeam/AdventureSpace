﻿using Content.Client.HUD;
using Content.Shared.Ghost;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Ghost
{
    [UsedImplicitly]
    public class GhostSystem : SharedGhostSystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IGameHud _gameHud = default!;

        private bool _ghostVisibility;

        private bool GhostVisibility
        {
            get => _ghostVisibility;
            set
            {
                if (_ghostVisibility == value)
                {
                    return;
                }

                _ghostVisibility = value;

                foreach (var ghost in ComponentManager.GetAllComponents(typeof(GhostComponent), true))
                {
                    if (ghost.Owner.TryGetComponent(out SpriteComponent? sprite))
                    {
                        sprite.Visible = value;
                    }
                }
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GhostComponent, ComponentInit>(OnGhostInit);
            SubscribeLocalEvent<GhostComponent, ComponentRemove>(OnGhostRemove);

            SubscribeLocalEvent<GhostComponent, PlayerAttachedEvent>(OnGhostPlayerAttach);
            SubscribeLocalEvent<GhostComponent, PlayerDetachedEvent>(OnGhostPlayerDetach);

            SubscribeNetworkEvent<GhostWarpsResponseEvent>(OnGhostWarpsResponse);
        }

        private void OnGhostInit(EntityUid uid, GhostComponent component, ComponentInit args)
        {
            if (component.Owner.TryGetComponent(out SpriteComponent? sprite))
            {
                sprite.Visible = GhostVisibility;
            }
        }

        private void OnGhostRemove(EntityUid uid, GhostComponent component, ComponentRemove args)
        {
            component.Gui?.Dispose();

            // PlayerDetachedMsg might not fire due to deletion order so...
            if (component.IsAttached)
            {
                GhostVisibility = false;
            }
        }

        private void OnGhostPlayerAttach(EntityUid uid, GhostComponent component, PlayerAttachedEvent playerAttachedEvent)
        {
            if (component.Gui == null)
            {
                component.Gui = new GhostGui(component, EntityManager.EntityNetManager!);
                component.Gui.Update();
            }
            else
            {
                component.Gui.Orphan();
            }

            _gameHud.HandsContainer.AddChild(component.Gui);
            GhostVisibility = true;
            component.IsAttached = true;
        }

        private void OnGhostPlayerDetach(EntityUid uid, GhostComponent component, PlayerDetachedEvent args)
        {
            component.Gui?.Parent?.RemoveChild(component.Gui);
            GhostVisibility = false;
            component.IsAttached = false;
        }

        private void OnGhostWarpsResponse(GhostWarpsResponseEvent msg)
        {
            var entity = _playerManager.LocalPlayer?.ControlledEntity;

            if (entity == null ||
                !entity.TryGetComponent(out GhostComponent? ghost))
            {
                return;
            }

            var window = ghost.Gui?.TargetWindow;

            if (window != null)
            {
                window.Locations = msg.Locations;
                window.Players = msg.Players;
                window.Populate();
            }
        }
    }
}
