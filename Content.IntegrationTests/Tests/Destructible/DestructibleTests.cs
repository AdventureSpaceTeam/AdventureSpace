﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Destructible;
using Content.Server.GameObjects.Components.Destructible.Thresholds;
using Content.Server.GameObjects.Components.Destructible.Thresholds.Behavior;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Destructible
{
    [TestFixture]
    [TestOf(typeof(DestructibleComponent))]
    [TestOf(typeof(Threshold))]
    public class DestructibleTests : ContentIntegrationTest
    {
        private static readonly string SpawnedEntityId = "DestructibleTestsSpawnedEntity";
        private static readonly string DestructibleEntityId = "DestructibleTestsDestructibleEntity";
        private static readonly string DestructibleDestructionEntityId = "DestructibleTestsDestructibleDestructionEntity";

        private static readonly string Prototypes = $@"
- type: entity
  id: {SpawnedEntityId}
  name: {SpawnedEntityId}

- type: entity
  id: {DestructibleEntityId}
  name: {DestructibleEntityId}
  components:
  - type: Damageable
  - type: Destructible
    thresholds:
      20:
        triggersOnce: false
      50:
        triggersOnce: false
        behaviors:
        - !type:PlaySoundBehavior
          sound: /Audio/Effects/woodhit.ogg
        - !type:SpawnEntitiesBehavior
          spawn:
            {SpawnedEntityId}:
              min: 1
              max: 1
        - !type:DoActsBehavior
          acts: [""Breakage""]
  - type: TestThresholdListener

- type: entity
  id: {DestructibleDestructionEntityId}
  name: {DestructibleDestructionEntityId}
  components:
  - type: Damageable
  - type: Destructible
    thresholds:
      50:
        behaviors:
        - !type:PlaySoundBehavior
          sound: /Audio/Effects/woodhit.ogg
        - !type:SpawnEntitiesBehavior
          spawn:
            {SpawnedEntityId}:
              min: 1
              max: 1
        - !type:DoActsBehavior # This must come last as it destroys the entity.
          acts: [""Destruction""]
  - type: TestThresholdListener
";

        private class TestThresholdListenerComponent : Component
        {
            public override string Name => "TestThresholdListener";

            public List<DestructibleThresholdReachedMessage> ThresholdsReached { get; } = new();

            public override void HandleMessage(ComponentMessage message, IComponent component)
            {
                base.HandleMessage(message, component);

                switch (message)
                {
                    case DestructibleThresholdReachedMessage msg:
                        ThresholdsReached.Add(msg);
                        break;
                }
            }
        }

        [Test]
        public async Task TestThresholdActivation()
        {
            var server = StartServerDummyTicker(new ServerContentIntegrationOption
            {
                ExtraPrototypes = Prototypes,
                ContentBeforeIoC = () =>
                {
                    IoCManager.Resolve<IComponentFactory>().Register<TestThresholdListenerComponent>();
                }
            });

            await server.WaitIdleAsync();

            var sEntityManager = server.ResolveDependency<IEntityManager>();
            var sMapManager = server.ResolveDependency<IMapManager>();

            IEntity sDestructibleEntity;
            IDamageableComponent sDamageableComponent = null;
            DestructibleComponent sDestructibleComponent = null;
            TestThresholdListenerComponent sThresholdListenerComponent = null;

            await server.WaitPost(() =>
            {
                var mapId = new MapId(1);
                var coordinates = new MapCoordinates(0, 0, mapId);
                sMapManager.CreateMap(mapId);

                sDestructibleEntity = sEntityManager.SpawnEntity(DestructibleEntityId, coordinates);
                sDamageableComponent = sDestructibleEntity.GetComponent<IDamageableComponent>();
                sDestructibleComponent = sDestructibleEntity.GetComponent<DestructibleComponent>();
                sThresholdListenerComponent = sDestructibleEntity.GetComponent<TestThresholdListenerComponent>();
            });

            await server.WaitRunTicks(5);

            await server.WaitAssertion(() =>
            {
                Assert.That(sThresholdListenerComponent.ThresholdsReached.Count, Is.Zero);
            });

            await server.WaitAssertion(() =>
            {
                Assert.True(sDamageableComponent.ChangeDamage(DamageType.Blunt, 10, true));

                // No thresholds reached yet, the earliest one is at 20 damage
                Assert.That(sThresholdListenerComponent.ThresholdsReached.Count, Is.Zero);

                Assert.True(sDamageableComponent.ChangeDamage(DamageType.Blunt, 10, true));

                // Only one threshold reached, 20
                Assert.That(sThresholdListenerComponent.ThresholdsReached.Count, Is.EqualTo(1));

                var msg = sThresholdListenerComponent.ThresholdsReached[0];

                // Check that it matches the total damage dealt
                Assert.That(msg.TotalDamage, Is.EqualTo(20));

                var threshold = msg.Threshold;

                // Check that it matches the YAML prototype
                Assert.That(threshold.Behaviors, Is.Empty);
                Assert.That(threshold.Triggered, Is.True);

                sThresholdListenerComponent.ThresholdsReached.Clear();

                Assert.True(sDamageableComponent.ChangeDamage(DamageType.Blunt, 30, true));

                // Only one threshold reached, 50, since 20 was already reached before
                Assert.That(sThresholdListenerComponent.ThresholdsReached.Count, Is.EqualTo(1));

                msg = sThresholdListenerComponent.ThresholdsReached[0];

                // Check that it matches the total damage dealt
                Assert.That(msg.TotalDamage, Is.EqualTo(50));

                threshold = msg.Threshold;

                // Check that it matches the YAML prototype
                Assert.That(threshold.Behaviors, Has.Count.EqualTo(3));

                var soundThreshold = (PlaySoundBehavior) threshold.Behaviors[0];
                var spawnThreshold = (SpawnEntitiesBehavior) threshold.Behaviors[1];
                var actsThreshold = (DoActsBehavior) threshold.Behaviors[2];

                Assert.That(actsThreshold.Acts, Is.EqualTo(ThresholdActs.Breakage));
                Assert.That(soundThreshold.Sound, Is.EqualTo("/Audio/Effects/woodhit.ogg"));
                Assert.That(spawnThreshold.Spawn, Is.Not.Null);
                Assert.That(spawnThreshold.Spawn.Count, Is.EqualTo(1));
                Assert.That(spawnThreshold.Spawn.Single().Key, Is.EqualTo(SpawnedEntityId));
                Assert.That(spawnThreshold.Spawn.Single().Value.Min, Is.EqualTo(1));
                Assert.That(spawnThreshold.Spawn.Single().Value.Max, Is.EqualTo(1));
                Assert.That(threshold.Triggered, Is.True);

                sThresholdListenerComponent.ThresholdsReached.Clear();

                // Damage for 50 again, up to 100 now
                Assert.True(sDamageableComponent.ChangeDamage(DamageType.Blunt, 50, true));

                // No new thresholds reached as even though they don't only trigger once, the entity was not healed below the threshold
                Assert.That(sThresholdListenerComponent.ThresholdsReached, Is.Empty);

                // Heal the entity for 40 damage, down to 60
                sDamageableComponent.ChangeDamage(DamageType.Blunt, -40, true);

                // Thresholds don't work backwards
                Assert.That(sThresholdListenerComponent.ThresholdsReached, Is.Empty);

                // Damage for 10, up to 70
                sDamageableComponent.ChangeDamage(DamageType.Blunt, 10, true);

                // Not enough healing to de-trigger a threshold
                Assert.That(sThresholdListenerComponent.ThresholdsReached, Is.Empty);

                // Heal by 30, down to 40
                sDamageableComponent.ChangeDamage(DamageType.Blunt, -30, true);

                // Thresholds don't work backwards
                Assert.That(sThresholdListenerComponent.ThresholdsReached, Is.Empty);

                // Damage up to 50 again
                sDamageableComponent.ChangeDamage(DamageType.Blunt, 10, true);

                // The 50 threshold should have triggered again, after being healed
                Assert.That(sThresholdListenerComponent.ThresholdsReached.Count, Is.EqualTo(1));

                msg = sThresholdListenerComponent.ThresholdsReached[0];

                // Check that it matches the total damage dealt
                Assert.That(msg.TotalDamage, Is.EqualTo(50));

                threshold = msg.Threshold;

                // Check that it matches the YAML prototype
                Assert.That(threshold.Behaviors, Has.Count.EqualTo(3));

                soundThreshold = (PlaySoundBehavior) threshold.Behaviors[0];
                spawnThreshold = (SpawnEntitiesBehavior) threshold.Behaviors[1];
                actsThreshold = (DoActsBehavior) threshold.Behaviors[2];

                // Check that it matches the YAML prototype
                Assert.That(actsThreshold.Acts, Is.EqualTo(ThresholdActs.Breakage));
                Assert.That(soundThreshold.Sound, Is.EqualTo("/Audio/Effects/woodhit.ogg"));
                Assert.That(spawnThreshold.Spawn, Is.Not.Null);
                Assert.That(spawnThreshold.Spawn.Count, Is.EqualTo(1));
                Assert.That(spawnThreshold.Spawn.Single().Key, Is.EqualTo(SpawnedEntityId));
                Assert.That(spawnThreshold.Spawn.Single().Value.Min, Is.EqualTo(1));
                Assert.That(spawnThreshold.Spawn.Single().Value.Max, Is.EqualTo(1));
                Assert.That(threshold.Triggered, Is.True);

                // Reset thresholds reached
                sThresholdListenerComponent.ThresholdsReached.Clear();

                // Heal all damage
                sDamageableComponent.Heal();

                // Damage up to 50
                sDamageableComponent.ChangeDamage(DamageType.Blunt, 50, true);

                // Check that the total damage matches
                Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(50));

                // Both thresholds should have triggered
                Assert.That(sThresholdListenerComponent.ThresholdsReached, Has.Exactly(2).Items);

                // Verify the first one, should be the lowest one (20)
                msg = sThresholdListenerComponent.ThresholdsReached[0];
                Assert.That(msg.ThresholdAmount, Is.EqualTo(20));

                // The total damage should be 50
                Assert.That(msg.TotalDamage, Is.EqualTo(50));

                threshold = msg.Threshold;

                // Check that it matches the YAML prototype
                Assert.That(threshold.Behaviors, Is.Empty);

                // Verify the second one, should be the highest one (50)
                msg = sThresholdListenerComponent.ThresholdsReached[1];
                Assert.That(msg.ThresholdAmount, Is.EqualTo(50));

                // Check that it matches the total damage dealt
                Assert.That(msg.TotalDamage, Is.EqualTo(50));

                threshold = msg.Threshold;

                Assert.That(threshold.Behaviors, Has.Count.EqualTo(3));

                soundThreshold = (PlaySoundBehavior) threshold.Behaviors[0];
                spawnThreshold = (SpawnEntitiesBehavior) threshold.Behaviors[1];
                actsThreshold = (DoActsBehavior) threshold.Behaviors[2];

                // Check that it matches the YAML prototype
                Assert.That(actsThreshold.Acts, Is.EqualTo(ThresholdActs.Breakage));
                Assert.That(soundThreshold.Sound, Is.EqualTo("/Audio/Effects/woodhit.ogg"));
                Assert.That(spawnThreshold.Spawn, Is.Not.Null);
                Assert.That(spawnThreshold.Spawn.Count, Is.EqualTo(1));
                Assert.That(spawnThreshold.Spawn.Single().Key, Is.EqualTo(SpawnedEntityId));
                Assert.That(spawnThreshold.Spawn.Single().Value.Min, Is.EqualTo(1));
                Assert.That(spawnThreshold.Spawn.Single().Value.Max, Is.EqualTo(1));
                Assert.That(threshold.Triggered, Is.True);

                // Reset thresholds reached
                sThresholdListenerComponent.ThresholdsReached.Clear();

                // Heal the entity completely
                sDamageableComponent.Heal();

                // Check that the entity has 0 damage
                Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(0));

                // Set both thresholds to only trigger once
                foreach (var destructibleThreshold in sDestructibleComponent.LowestToHighestThresholds.Values)
                {
                    destructibleThreshold.TriggersOnce = true;
                }

                // Damage the entity up to 50 damage again
                sDamageableComponent.ChangeDamage(DamageType.Blunt, 50, true);

                // Check that the total damage matches
                Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(50));

                // No thresholds should have triggered as they were already triggered before, and they are set to only trigger once
                Assert.That(sThresholdListenerComponent.ThresholdsReached, Is.Empty);

                // Set both thresholds to trigger multiple times
                foreach (var destructibleThreshold in sDestructibleComponent.LowestToHighestThresholds.Values)
                {
                    destructibleThreshold.TriggersOnce = false;
                }

                // Check that the total damage matches
                Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(50));

                // They shouldn't have been triggered by changing TriggersOnce
                Assert.That(sThresholdListenerComponent.ThresholdsReached, Is.Empty);
            });
        }

        [Test]
        public async Task DestructibleDestructionTest()
        {
            var server = StartServerDummyTicker(new ServerContentIntegrationOption
            {
                ExtraPrototypes = Prototypes,
                ContentBeforeIoC = () =>
                {
                    IoCManager.Resolve<IComponentFactory>().Register<TestThresholdListenerComponent>();
                }
            });

            await server.WaitIdleAsync();

            var sEntityManager = server.ResolveDependency<IEntityManager>();
            var sMapManager = server.ResolveDependency<IMapManager>();

            IEntity sDestructibleEntity = null;
            IDamageableComponent sDamageableComponent = null;
            DestructibleComponent sDestructibleComponent = null;
            TestThresholdListenerComponent sThresholdListenerComponent = null;

            await server.WaitPost(() =>
            {
                var mapId = new MapId(1);
                var coordinates = new MapCoordinates(0, 0, mapId);
                sMapManager.CreateMap(mapId);

                sDestructibleEntity = sEntityManager.SpawnEntity(DestructibleDestructionEntityId, coordinates);
                sDamageableComponent = sDestructibleEntity.GetComponent<IDamageableComponent>();
                sDestructibleComponent = sDestructibleEntity.GetComponent<DestructibleComponent>();
                sThresholdListenerComponent = sDestructibleEntity.GetComponent<TestThresholdListenerComponent>();
            });

            await server.WaitAssertion(() =>
            {
                var coordinates = sDestructibleEntity.Transform.Coordinates;

                Assert.DoesNotThrow(() =>
                {
                    Assert.True(sDamageableComponent.ChangeDamage(DamageClass.Brute, 50, true));
                });

                Assert.That(sThresholdListenerComponent.ThresholdsReached.Count, Is.EqualTo(1));

                var threshold = sThresholdListenerComponent.ThresholdsReached[0].Threshold;

                Assert.That(threshold.Triggered, Is.True);
                Assert.That(threshold.Behaviors.Count, Is.EqualTo(3));

                var spawnEntitiesBehavior = (SpawnEntitiesBehavior) threshold.Behaviors.Single(b => b is SpawnEntitiesBehavior);

                Assert.That(spawnEntitiesBehavior.Spawn.Count, Is.EqualTo(1));
                Assert.That(spawnEntitiesBehavior.Spawn.Keys.Single(), Is.EqualTo(SpawnedEntityId));
                Assert.That(spawnEntitiesBehavior.Spawn.Values.Single(), Is.EqualTo(new MinMax {Min = 1, Max = 1}));

                var entitiesInRange = sEntityManager.GetEntitiesInRange(coordinates, 2);
                var found = false;

                foreach (var entity in entitiesInRange)
                {
                    if (entity.Prototype == null)
                    {
                        continue;
                    }

                    if (entity.Prototype.Name != SpawnedEntityId)
                    {
                        continue;
                    }

                    found = true;
                    break;
                }

                Assert.That(found, Is.True);
            });
        }
    }
}
