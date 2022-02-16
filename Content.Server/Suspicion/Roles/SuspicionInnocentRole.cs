using Content.Server.Chat.Managers;
using Content.Shared.Roles;
using Robust.Shared.IoC;

namespace Content.Server.Suspicion.Roles
{
    public sealed class SuspicionInnocentRole : SuspicionRole
    {
        public AntagPrototype Prototype { get; }

        public SuspicionInnocentRole(Mind.Mind mind, AntagPrototype antagPrototype) : base(mind)
        {
            Prototype = antagPrototype;
            Name = antagPrototype.Name;
            Antagonist = antagPrototype.Antagonist;
        }

        public override string Name { get; }
        public string Objective => Prototype.Objective;
        public override bool Antagonist { get; }

        public override void Greet()
        {
            base.Greet();

            var chat = IoCManager.Resolve<IChatManager>();

            if (Mind.TryGetSession(out var session))
            {
                chat.DispatchServerMessage(session, $"You're an {Name}!");
                chat.DispatchServerMessage(session, $"Objective: {Objective}");
            }
        }
    }
}
