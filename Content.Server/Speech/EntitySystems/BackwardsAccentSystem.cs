using System;
using Content.Server.Speech.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.Speech.EntitySystems
{
    public class BackwardsAccentSystem : EntitySystem
    {
        public override void Initialize()
        {
            SubscribeLocalEvent<BackwardsAccentComponent, AccentGetEvent>(OnAccent);
        }

        public string Accentuate(string message)
        {
            var arr = message.ToCharArray();
            Array.Reverse(arr);
            return new string(arr);
        }

        private void OnAccent(EntityUid uid, BackwardsAccentComponent component, AccentGetEvent args)
        {
            args.Message = Accentuate(args.Message);
        }
    }
}
