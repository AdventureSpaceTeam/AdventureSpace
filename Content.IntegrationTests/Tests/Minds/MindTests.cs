﻿#nullable enable
using System;
using System.Threading.Tasks;
using Content.Server.Mind;
using Content.Server.Mind.Components;
using Content.Server.Roles;
using Content.Server.Traitor;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Roles;
using NUnit.Framework;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Minds;

[TestFixture]
public sealed class MindTests
{
    private const string Prototypes = @"
- type: entity
  id: MindTestEntity
  components:
  - type: MindContainer

- type: entity
  parent: MindTestEntity
  id: MindTestEntityDamageable
  components:
  - type: Damageable
    damageContainer: Biological
  - type: Body
    prototype: Human
    requiredLegs: 2
  - type: MobState
  - type: MobThresholds
    thresholds:
      0: Alive
      100: Critical
      200: Dead
  - type: Destructible
    thresholds:
    - trigger:
        !type:DamageTypeTrigger
        damageType: Blunt
        damage: 400
        behaviors:
        - !type:GibBehavior { }
";

    /// <summary>
    ///     Exception handling for PlayerData and NetUserId invalid due to testing.
    ///     Can be removed when Players can be mocked.
    /// </summary>
    /// <param name="func"></param>
    private void CatchPlayerDataException(Action func)
    {
        try
        {
            func();
        }
        catch (ArgumentException e)
        {
            // Prevent exiting due to PlayerData not being initialized.
            if (e.Message == "New owner must have previously logged into the server. (Parameter 'newOwner')")
                return;
            throw;
        }
    }

    [Test]
    public async Task TestCreateAndTransferMindToNewEntity()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{ NoClient = true });
        var server = pairTracker.Pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();

        await server.WaitAssertion(() =>
        {
            var mindSystem = entMan.EntitySysManager.GetEntitySystem<MindSystem>();

            var entity = entMan.SpawnEntity(null, new MapCoordinates());
            var mindComp = entMan.EnsureComponent<MindContainerComponent>(entity);

            var mind = mindSystem.CreateMind(null);

            Assert.That(mind.UserId, Is.EqualTo(null));

            mindSystem.TransferTo(mind, entity);
            Assert.That(mindSystem.GetMind(entity, mindComp), Is.EqualTo(mind));
        });

        await pairTracker.CleanReturnAsync();
    }

    [Test]
    public async Task TestReplaceMind()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{ NoClient = true });
        var server = pairTracker.Pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();

        await server.WaitAssertion(() =>
        {
            var mindSystem = entMan.EntitySysManager.GetEntitySystem<MindSystem>();

            var entity = entMan.SpawnEntity(null, new MapCoordinates());
            var mindComp = entMan.EnsureComponent<MindContainerComponent>(entity);

            var mind = mindSystem.CreateMind(null);
            mindSystem.TransferTo(mind, entity);
            Assert.That(mindSystem.GetMind(entity, mindComp), Is.EqualTo(mind));
            
            var mind2 = mindSystem.CreateMind(null);
            mindSystem.TransferTo(mind2, entity);
            Assert.That(mindSystem.GetMind(entity, mindComp), Is.EqualTo(mind2));
            Assert.That(mind.OwnedEntity != entity);
        });

        await pairTracker.CleanReturnAsync();
    }

    [Test]
    public async Task TestEntityDeadWhenGibbed()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{ NoClient = true, ExtraPrototypes = Prototypes });
        var server = pairTracker.Pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();
        var protoMan = server.ResolveDependency<IPrototypeManager>();

        EntityUid entity = default!;
        MindContainerComponent mindContainerComp = default!;
        Mind mind = default!;
        var mindSystem = entMan.EntitySysManager.GetEntitySystem<MindSystem>();
        var damageableSystem = entMan.EntitySysManager.GetEntitySystem<DamageableSystem>();

        await server.WaitAssertion(() =>
        {
            entity = entMan.SpawnEntity("MindTestEntityDamageable", new MapCoordinates());
            mindContainerComp = entMan.EnsureComponent<MindContainerComponent>(entity);

            mind = mindSystem.CreateMind(null);

            mindSystem.TransferTo(mind, entity);
            Assert.That(mindSystem.GetMind(entity, mindContainerComp), Is.EqualTo(mind));
            Assert.That(!mindSystem.IsCharacterDeadPhysically(mind));
        });

        await PoolManager.RunTicksSync(pairTracker.Pair, 5);

        await server.WaitAssertion(() =>
        {
            var damageable = entMan.GetComponent<DamageableComponent>(entity);
            if (!protoMan.TryIndex<DamageTypePrototype>("Blunt", out var prototype))
            {
                return;
            }

            damageableSystem.SetDamage(entity, damageable, new DamageSpecifier(prototype, FixedPoint2.New(401)));
            Assert.That(mindSystem.GetMind(entity, mindContainerComp), Is.EqualTo(mind));
        });

        await PoolManager.RunTicksSync(pairTracker.Pair, 5);

        await server.WaitAssertion(() =>
        {
            Assert.That(mindSystem.IsCharacterDeadPhysically(mind));
        });

        await pairTracker.CleanReturnAsync();
    }

    [Test]
    public async Task TestMindTransfersToOtherEntity()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{ NoClient = true });
        var server = pairTracker.Pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();

        await server.WaitAssertion(() =>
        {
            var mindSystem = entMan.EntitySysManager.GetEntitySystem<MindSystem>();

            var entity = entMan.SpawnEntity(null, new MapCoordinates());
            var targetEntity = entMan.SpawnEntity(null, new MapCoordinates());
            var mindComp = entMan.EnsureComponent<MindContainerComponent>(entity);
            entMan.EnsureComponent<MindContainerComponent>(targetEntity);

            var mind = mindSystem.CreateMind(null);

            mindSystem.TransferTo(mind, entity);

            Assert.That(mindSystem.GetMind(entity, mindComp), Is.EqualTo(mind));

            mindSystem.TransferTo(mind, targetEntity);
            Assert.That(mindSystem.GetMind(entity, mindComp), Is.EqualTo(null));
            Assert.That(mindSystem.GetMind(targetEntity), Is.EqualTo(mind));
        });

        await pairTracker.CleanReturnAsync();
    }

    [Test]
    public async Task TestOwningPlayerCanBeChanged()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{ NoClient = true });
        var server = pairTracker.Pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();

        await server.WaitAssertion(() =>
        {
            var mindSystem = entMan.EntitySysManager.GetEntitySystem<MindSystem>();

            var entity = entMan.SpawnEntity(null, new MapCoordinates());
            var mindComp = entMan.EnsureComponent<MindContainerComponent>(entity);

            var mind = mindSystem.CreateMind(null);

            mindSystem.TransferTo(mind, entity);

            Assert.That(mindSystem.GetMind(entity, mindComp), Is.EqualTo(mind));

            var newUserId = new NetUserId(Guid.NewGuid());
            Assert.That(mindComp.HasMind);
            CatchPlayerDataException(() =>
                mindSystem.ChangeOwningPlayer(mindComp.Mind!, newUserId));

            Assert.That(mind.UserId, Is.EqualTo(newUserId));
        });

        await pairTracker.CleanReturnAsync();
    }

    [Test]
    public async Task TestAddRemoveHasRoles()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{ NoClient = true });
        var server = pairTracker.Pair.Server;

        var entMan = server.ResolveDependency<IServerEntityManager>();

        await server.WaitAssertion(() =>
        {
            var mindSystem = entMan.EntitySysManager.GetEntitySystem<MindSystem>();

            var entity = entMan.SpawnEntity(null, new MapCoordinates());
            var mindComp = entMan.EnsureComponent<MindContainerComponent>(entity);

            var mind = mindSystem.CreateMind(null);

            Assert.That(mind.UserId, Is.EqualTo(null));

            mindSystem.TransferTo(mind, entity);
            Assert.That(mindSystem.GetMind(entity, mindComp), Is.EqualTo(mind));

            Assert.That(!mindSystem.HasRole<TraitorRole>(mind));
            Assert.That(!mindSystem.HasRole<Job>(mind));

            var traitorRole = new TraitorRole(mind, new AntagPrototype());
            
            mindSystem.AddRole(mind, traitorRole);
            
            Assert.That(mindSystem.HasRole<TraitorRole>(mind));
            Assert.That(!mindSystem.HasRole<Job>(mind));

            var jobRole = new Job(mind, new JobPrototype());
            
            mindSystem.AddRole(mind, jobRole);
            
            Assert.That(mindSystem.HasRole<TraitorRole>(mind));
            Assert.That(mindSystem.HasRole<Job>(mind));
            
            mindSystem.RemoveRole(mind, traitorRole);
            
            Assert.That(!mindSystem.HasRole<TraitorRole>(mind));
            Assert.That(mindSystem.HasRole<Job>(mind));
            
            mindSystem.RemoveRole(mind, jobRole);
            
            Assert.That(!mindSystem.HasRole<TraitorRole>(mind));
            Assert.That(!mindSystem.HasRole<Job>(mind));
        });

        await pairTracker.CleanReturnAsync();
    }
}
