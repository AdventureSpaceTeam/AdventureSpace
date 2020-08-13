using System;
using Content.Shared.GameObjects.Components;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timers;

namespace Content.Server.GameObjects.Components.StationEvents
{
    [RegisterComponent]
    public sealed class RadiationPulseComponent : SharedRadiationPulseComponent
    {
        private const float MinPulseLifespan = 0.8f;
        private const float MaxPulseLifespan = 2.5f;

        public float DPS => _dps;
        private float _dps;
        
        private TimeSpan _endTime;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _dps, "dps", 40.0f);
        }

        public override void Initialize()
        {
            base.Initialize();

            var currentTime = IoCManager.Resolve<IGameTiming>().CurTime;
            var duration  =
                TimeSpan.FromSeconds(
                    IoCManager.Resolve<IRobustRandom>().NextFloat() * (MaxPulseLifespan - MinPulseLifespan) +
                    MinPulseLifespan);

            _endTime = currentTime + duration;
            
            Timer.Spawn(duration, 
                () =>
            {
                if (!Owner.Deleted)
                {
                    Owner.Delete();
                }
            });

            EntitySystem.Get<AudioSystem>().PlayAtCoords("/Audio/Weapons/Guns/Gunshots/laser3.ogg", Owner.Transform.GridPosition);
            Dirty();
        }

        public override ComponentState GetComponentState()
        {
            return new RadiationPulseMessage(_endTime);
        }
    }
}