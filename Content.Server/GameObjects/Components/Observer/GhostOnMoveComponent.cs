#nullable enable
using System;
using Content.Server.Commands.Observer;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Movement;
using Content.Shared.GameObjects.Components.Movement;
using Robust.Server.Console;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Players;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Observer
{
    [RegisterComponent]
    [ComponentReference(typeof(IGhostOnMove))]
    public class GhostOnMoveComponent : Component, IRelayMoveInput, IGhostOnMove
    {
        public override string Name => "GhostOnMove";

        public bool CanReturn { get; set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => x.CanReturn, "canReturn", true);
        }

        void IRelayMoveInput.MoveInputPressed(ICommonSession session)
        {
            // Let's not ghost if our mind is visiting...
            if (Owner.HasComponent<VisitingMindComponent>()) return;
            if (!Owner.TryGetComponent(out MindComponent? mind) || !mind.HasMind || mind.Mind!.IsVisitingEntity) return;

            var host = IoCManager.Resolve<IServerConsoleHost>();
            new Ghost().Execute(new ConsoleShell(host, session), string.Empty, Array.Empty<string>());
        }
    }
}
