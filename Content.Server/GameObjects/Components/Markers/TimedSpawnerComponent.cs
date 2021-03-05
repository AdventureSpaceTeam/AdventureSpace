﻿using System;
using System.Collections.Generic;
using System.Threading;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Markers
{
    [RegisterComponent]
    public class TimedSpawnerComponent : Component, ISerializationHooks
    {
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        public override string Name => "TimedSpawner";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("prototypes")]
        public List<string> Prototypes { get; set; } = new();

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("chance")]
        public float Chance { get; set; } = 1.0f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("intervalSeconds")]
        public int IntervalSeconds { get; set; } = 60;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("MinimumEntitiesSpawned")]
        public int MinimumEntitiesSpawned { get; set; } = 1;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("MaximumEntitiesSpawned")]
        public int MaximumEntitiesSpawned { get; set; } = 1;

        private CancellationTokenSource TokenSource;

        void ISerializationHooks.AfterDeserialization()
        {
            if (MinimumEntitiesSpawned > MaximumEntitiesSpawned)
                throw new ArgumentException("MaximumEntitiesSpawned can't be lower than MinimumEntitiesSpawned!");
        }

        public override void Initialize()
        {
            base.Initialize();
            SetupTimer();
        }

        protected override void Shutdown()
        {
            base.Shutdown();
            TokenSource.Cancel();
        }

        private void SetupTimer()
        {
            TokenSource?.Cancel();
            TokenSource = new CancellationTokenSource();
            Owner.SpawnRepeatingTimer(TimeSpan.FromSeconds(IntervalSeconds), OnTimerFired, TokenSource.Token);
        }

        private void OnTimerFired()
        {
            if (!_robustRandom.Prob(Chance))
                return;

            var number = _robustRandom.Next(MinimumEntitiesSpawned, MaximumEntitiesSpawned);

            for (int i = 0; i < number; i++)
            {
                var entity = _robustRandom.Pick(Prototypes);
                Owner.EntityManager.SpawnEntity(entity, Owner.Transform.Coordinates);
            }
        }
    }
}
