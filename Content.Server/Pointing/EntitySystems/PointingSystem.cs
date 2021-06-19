#nullable enable
using System;
using System.Collections.Generic;
using Content.Server.Ghost.Components;
using Content.Server.Players;
using Content.Server.Pointing.Components;
using Content.Server.Visible;
using Content.Shared.ActionBlocker;
using Content.Shared.Input;
using Content.Shared.Interaction.Events;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Notification.Managers;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Timing;

namespace Content.Server.Pointing.EntitySystems
{
    [UsedImplicitly]
    internal sealed class PointingSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private static readonly TimeSpan PointDelay = TimeSpan.FromSeconds(0.5f);

        /// <summary>
        ///     A dictionary of players to the last time that they
        ///     pointed at something.
        /// </summary>
        private readonly Dictionary<ICommonSession, TimeSpan> _pointers = new();

        private const float PointingRange = 15f;

        private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            if (e.NewStatus != SessionStatus.Disconnected)
            {
                return;
            }

            _pointers.Remove(e.Session);
        }

        // TODO: FOV
        private void SendMessage(IEntity source, IList<IPlayerSession> viewers, IEntity? pointed, string selfMessage,
            string viewerMessage, string? viewerPointedAtMessage = null)
        {
            foreach (var viewer in viewers)
            {
                var viewerEntity = viewer.AttachedEntity;
                if (viewerEntity == null)
                {
                    continue;
                }

                var message = viewerEntity == source
                    ? selfMessage
                    : viewerEntity == pointed && viewerPointedAtMessage != null
                        ? viewerPointedAtMessage
                        : viewerMessage;

                source.PopupMessage(viewerEntity, message);
            }
        }

        public bool InRange(IEntity pointer, EntityCoordinates coordinates)
        {
            if (pointer.HasComponent<GhostComponent>()){
                return pointer.Transform.Coordinates.InRange(EntityManager, coordinates, 15);
            }
            else
            {
                return pointer.InRangeUnOccluded(coordinates, 15, e => e == pointer);
            }
        }

        public bool TryPoint(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
        {
            var player = (session as IPlayerSession)?.ContentData()?.Mind?.CurrentEntity;
            if (player == null)
            {
                return false;
            }

            if (_pointers.TryGetValue(session!, out var lastTime) &&
                _gameTiming.CurTime < lastTime + PointDelay)
            {
                return false;
            }

            if (EntityManager.TryGetEntity(uid, out var entity) && entity.HasComponent<PointingArrowComponent>())
            {
                // this is a pointing arrow. no pointing here...
                return false;
            }

            if (!InRange(player, coords))
            {
                player.PopupMessage(Loc.GetString("You can't reach there!"));
                return false;
            }

            if (EntitySystem.Get<ActionBlockerSystem>().CanChangeDirection(player))
            {
                var diff = coords.ToMapPos(EntityManager) - player.Transform.MapPosition.Position;
                if (diff.LengthSquared > 0.01f)
                {
                    player.Transform.LocalRotation = new Angle(diff);
                }
            }

            var arrow = EntityManager.SpawnEntity("pointingarrow", coords);

            var layer = (int)VisibilityFlags.Normal;
            if (player.TryGetComponent(out VisibilityComponent? playerVisibility))
            {
                var arrowVisibility = arrow.EnsureComponent<VisibilityComponent>();
                layer = arrowVisibility.Layer = playerVisibility.Layer;
            }

            // Get players that are in range and whose visibility layer matches the arrow's.
            var viewers = _playerManager.GetPlayersBy((playerSession) =>
            {
                var ent = playerSession.ContentData()?.Mind?.CurrentEntity;

                if (ent is null || (!ent.TryGetComponent<EyeComponent>(out var eyeComp) || (eyeComp.VisibilityMask & layer) != 0))
                    return false;

                return ent.Transform.MapPosition.InRange(player.Transform.MapPosition, PointingRange);
            });

            string selfMessage;
            string viewerMessage;
            string? viewerPointedAtMessage = null;

            if (EntityManager.TryGetEntity(uid, out var pointed))
            {
                selfMessage = player == pointed
                    ? Loc.GetString("You point at yourself.")
                    : Loc.GetString("You point at {0:theName}.", pointed);

                viewerMessage = player == pointed
                    ? $"{player.Name} {Loc.GetString("points at {0:themself}.", player)}"
                    : $"{player.Name} {Loc.GetString("points at {0:theName}.", pointed)}";

                viewerPointedAtMessage = $"{player.Name} {Loc.GetString("points at you.")}";
            }
            else
            {
                var tileRef = _mapManager.GetGrid(coords.GetGridId(EntityManager)).GetTileRef(coords);
                var tileDef = _tileDefinitionManager[tileRef.Tile.TypeId];

                selfMessage = Loc.GetString("You point at {0}.", tileDef.DisplayName);

                viewerMessage = $"{player.Name} {Loc.GetString("points at {0}.", tileDef.DisplayName)}";
            }

            _pointers[session!] = _gameTiming.CurTime;

            SendMessage(player, viewers, pointed, selfMessage, viewerMessage, viewerPointedAtMessage);

            return true;
        }

        public override void Initialize()
        {
            base.Initialize();

            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.Point, new PointerInputCmdHandler(TryPoint))
                .Register<PointingSystem>();
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
            _pointers.Clear();
        }

        public override void Update(float frameTime)
        {
            foreach (var component in ComponentManager.EntityQuery<PointingArrowComponent>(true))
            {
                component.Update(frameTime);
            }
        }
    }
}
