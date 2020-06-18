using System;
using System.Collections.Generic;
using Content.Shared.AI;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects.EntitySystems.AI
{
#if DEBUG
    public class ClientAiDebugSystem : EntitySystem
    {
        private AiDebugMode _tooltips = AiDebugMode.None;
        private readonly Dictionary<IEntity, PanelContainer> _aiBoxes = new Dictionary<IEntity,PanelContainer>();

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            if (_tooltips == 0)
            {
                return;
            }

            var eyeManager = IoCManager.Resolve<IEyeManager>();
            foreach (var (entity, panel) in _aiBoxes)
            {
                if (entity == null) continue;

                if (!eyeManager.GetWorldViewport().Contains(entity.Transform.WorldPosition))
                {
                    panel.Visible = false;
                    continue;
                }

                var (x, y) = eyeManager.WorldToScreen(entity.Transform.GridPosition).Position;
                var offsetPosition = new Vector2(x - panel.Width / 2, y - panel.Height - 50f);
                panel.Visible = true;

                LayoutContainer.SetPosition(panel, offsetPosition);
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<SharedAiDebug.UtilityAiDebugMessage>(HandleUtilityAiDebugMessage);
            SubscribeNetworkEvent<SharedAiDebug.AStarRouteMessage>(HandleAStarRouteMessage);
            SubscribeNetworkEvent<SharedAiDebug.JpsRouteMessage>(HandleJpsRouteMessage);
        }

        private void HandleUtilityAiDebugMessage(SharedAiDebug.UtilityAiDebugMessage message)
        {
            if ((_tooltips & AiDebugMode.Thonk) != 0)
            {
                // I guess if it's out of range we don't know about it?
                var entityManager = IoCManager.Resolve<IEntityManager>();
                var entity = entityManager.GetEntity(message.EntityUid);
                if (entity == null)
                {
                    return;
                }

                TryCreatePanel(entity);

                // Probably shouldn't access by index but it's a debugging tool so eh
                var label = (Label) _aiBoxes[entity].GetChild(0).GetChild(0);
                label.Text = $"Current Task: {message.FoundTask}\n" +
                             $"Task score: {message.ActionScore}\n" +
                             $"Planning time (ms): {message.PlanningTime * 1000:0.0000}\n" +
                             $"Considered {message.ConsideredTaskCount} tasks";
            }
        }

        private void HandleAStarRouteMessage(SharedAiDebug.AStarRouteMessage message)
        {
            if ((_tooltips & AiDebugMode.Paths) != 0)
            {
                var entityManager = IoCManager.Resolve<IEntityManager>();
                var entity = entityManager.GetEntity(message.EntityUid);
                if (entity == null)
                {
                    return;
                }

                TryCreatePanel(entity);

                var label = (Label) _aiBoxes[entity].GetChild(0).GetChild(1);
                label.Text = $"Pathfinding time (ms): {message.TimeTaken * 1000:0.0000}\n" +
                             $"Nodes traversed: {message.ClosedTiles.Count}\n" +
                             $"Nodes per ms: {message.ClosedTiles.Count / (message.TimeTaken * 1000)}";
            }
        }

        private void HandleJpsRouteMessage(SharedAiDebug.JpsRouteMessage message)
        {
            if ((_tooltips & AiDebugMode.Paths) != 0)
            {
                var entityManager = IoCManager.Resolve<IEntityManager>();
                var entity = entityManager.GetEntity(message.EntityUid);
                if (entity == null)
                {
                    return;
                }

                TryCreatePanel(entity);

                var label = (Label) _aiBoxes[entity].GetChild(0).GetChild(1);
                label.Text = $"Pathfinding time (ms): {message.TimeTaken * 1000:0.0000}\n" +
                             $"Jump Nodes: {message.JumpNodes.Count}\n" +
                             $"Jump Nodes per ms: {message.JumpNodes.Count / (message.TimeTaken * 1000)}";
            }
        }

        public void Disable()
        {
            foreach (var tooltip in _aiBoxes.Values)
            {
                tooltip.Dispose();
            }
            _aiBoxes.Clear();
            _tooltips = AiDebugMode.None;
        }


        private void EnableTooltip(AiDebugMode tooltip)
        {
            _tooltips |= tooltip;
        }

        private void DisableTooltip(AiDebugMode tooltip)
        {
            _tooltips &= ~tooltip;
        }

        public void ToggleTooltip(AiDebugMode tooltip)
        {
            if ((_tooltips & tooltip) != 0)
            {
                DisableTooltip(tooltip);
            }
            else
            {
                EnableTooltip(tooltip);
            }
        }

        private bool TryCreatePanel(IEntity entity)
        {
            if (!_aiBoxes.ContainsKey(entity))
            {
                var userInterfaceManager = IoCManager.Resolve<IUserInterfaceManager>();

                var actionLabel = new Label
                {
                    MouseFilter = Control.MouseFilterMode.Ignore,
                };

                var pathfindingLabel = new Label
                {
                    MouseFilter = Control.MouseFilterMode.Ignore,
                };

                var vBox = new VBoxContainer()
                {
                    SeparationOverride = 15,
                    Children = {actionLabel, pathfindingLabel},
                };

                var panel = new PanelContainer
                {
                    StyleClasses = {"tooltipBox"},
                    Children = {vBox},
                    MouseFilter = Control.MouseFilterMode.Ignore,
                    ModulateSelfOverride = Color.White.WithAlpha(0.75f),
                };

                userInterfaceManager.StateRoot.AddChild(panel);

                _aiBoxes[entity] = panel;
                return true;
            }

            return false;
        }
    }

    [Flags]
    public enum AiDebugMode : byte
    {
        None = 0,
        Paths = 1 << 1,
        Thonk = 1 << 2,
    }
#endif
}
