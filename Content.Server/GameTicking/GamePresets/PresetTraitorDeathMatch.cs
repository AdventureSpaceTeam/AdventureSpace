using System;
using System.Collections.Generic;
using Content.Server.GameTicking.GameRules;
using Content.Server.Interfaces.GameTicking;
using Content.Server.Interfaces.Chat;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.PDA;
using Content.Server.GameObjects.Components.Markers;
using Content.Server.GameObjects.Components.TraitorDeathMatch;
using Content.Server.Mobs;
using Content.Server.Mobs.Roles.Traitor;
using Content.Server.Players;
using Content.Server.Atmos;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Inventory;
using Content.Shared.GameObjects.Components.PDA;
using Content.Shared.GameObjects.Components.Mobs.State;
using Content.Shared;
using Robust.Shared.Map;
using Robust.Server.Interfaces.Player;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Log;

namespace Content.Server.GameTicking.GamePresets
{
    public sealed class PresetTraitorDeathMatch : GamePreset
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IGameTicker _gameTicker = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        public string PDAPrototypeName => "CaptainPDA";
        public string BeltPrototypeName => "ClothingBeltJanitorFilled";
        public string BackpackPrototypeName => "ClothingBackpackFilled";

        private RuleMaxTimeRestart _restarter;
        private bool _safeToEndRound = false;

        private Dictionary<UplinkAccount, string> _allOriginalNames = new();

        public override bool Start(IReadOnlyList<IPlayerSession> readyPlayers, bool force = false)
        {
            _gameTicker.AddGameRule<RuleTraitorDeathMatch>();
            _restarter = _gameTicker.AddGameRule<RuleMaxTimeRestart>();
            _restarter.RoundMaxTime = TimeSpan.FromMinutes(30);
            _restarter.RestartTimer();
            _safeToEndRound = true;
            return true;
        }

        public override void OnSpawnPlayerCompleted(IPlayerSession session, IEntity mob, bool lateJoin)
        {
            int startingBalance = _cfg.GetCVar(CCVars.TraitorDeathMatchStartingBalance);

            // Yup, they're a traitor
            var mind = session.Data.ContentData()?.Mind;
            var traitorRole = new TraitorRole(mind);
            if (mind == null)
            {
                Logger.ErrorS("preset", "Failed getting mind for TDM player.");
                return;
            }

            mind.AddRole(traitorRole);

            // Delete anything that may contain "dangerous" role-specific items.
            // (This includes the PDA, as everybody gets the captain PDA in this mode for true-all-access reasons.)
            var inventory = mind.OwnedEntity.GetComponent<InventoryComponent>();
            var victimSlots = new[] {EquipmentSlotDefines.Slots.IDCARD, EquipmentSlotDefines.Slots.BELT, EquipmentSlotDefines.Slots.BACKPACK};
            foreach (var slot in victimSlots)
                if (inventory.TryGetSlotItem(slot, out ItemComponent vItem))
                    vItem.Owner.Delete();

            // Replace their items:

            //  pda
            var newPDA = _entityManager.SpawnEntity(PDAPrototypeName, mind.OwnedEntity.Transform.Coordinates);
            inventory.Equip(EquipmentSlotDefines.Slots.IDCARD, newPDA.GetComponent<ItemComponent>());

            //  belt
            var newTmp = _entityManager.SpawnEntity(BeltPrototypeName, mind.OwnedEntity.Transform.Coordinates);
            inventory.Equip(EquipmentSlotDefines.Slots.BELT, newTmp.GetComponent<ItemComponent>());

            //  backpack
            newTmp = _entityManager.SpawnEntity(BackpackPrototypeName, mind.OwnedEntity.Transform.Coordinates);
            inventory.Equip(EquipmentSlotDefines.Slots.BACKPACK, newTmp.GetComponent<ItemComponent>());

            // Like normal traitors, they need access to a traitor account.
            var uplinkAccount = new UplinkAccount(mind.OwnedEntity.Uid, startingBalance);
            var pdaComponent = newPDA.GetComponent<PDAComponent>();
            pdaComponent.InitUplinkAccount(uplinkAccount);
            _allOriginalNames[uplinkAccount] = mind.OwnedEntity.Name;

            // The PDA needs to be marked with the correct owner.
            pdaComponent.SetPDAOwner(mind.OwnedEntity.Name);
            newPDA.AddComponent<TraitorDeathMatchReliableOwnerTagComponent>().UserId = mind.UserId;

            // Finally, it would be preferrable if they spawned as far away from other players as reasonably possible.
            if (FindAnyIsolatedSpawnLocation(mind, out var bestTarget))
            {
                mind.OwnedEntity.Transform.Coordinates = bestTarget;
            }
            else
            {
                // The station is too drained of air to safely continue.
                if (_safeToEndRound)
                {
                    _chatManager.DispatchServerAnnouncement(Loc.GetString("The station is too unsafe to continue. You have one minute."));
                    _restarter.RoundMaxTime = TimeSpan.FromMinutes(1);
                    _restarter.RestartTimer();
                    _safeToEndRound = false;
                }
            }
        }

        // It would be nice if this function were moved to some generic helpers class.
        private bool FindAnyIsolatedSpawnLocation(Mind ignoreMe, out EntityCoordinates bestTarget)
        {
            // Collate people to avoid...
            var existingPlayerPoints = new List<EntityCoordinates>();
            foreach (var player in _playerManager.GetAllPlayers())
            {
                var avoidMeMind = player.Data.ContentData()?.Mind;
                if ((avoidMeMind == null) || (avoidMeMind == ignoreMe))
                    continue;
                var avoidMeEntity = avoidMeMind.OwnedEntity;
                if (avoidMeEntity == null)
                    continue;
                if (avoidMeEntity.TryGetComponent(out IMobStateComponent mobState))
                {
                    // Does have mob state component; if critical or dead, they don't really matter for spawn checks
                    if (mobState.IsCritical() || mobState.IsDead())
                        continue;
                }
                else
                {
                    // Doesn't have mob state component. Assume something interesting is going on and don't count this as someone to avoid.
                    continue;
                }
                existingPlayerPoints.Add(avoidMeEntity.Transform.Coordinates);
            }

            // Iterate over each possible spawn point, comparing to the existing player points.
            // On failure, the returned target is the location that we're already at.
            var bestTargetDistanceFromNearest = -1.0f;
            // Need the random shuffle or it stuffs the first person into Atmospherics pretty reliably
            var ents = new List<IEntity>(_entityManager.GetEntities(new TypeEntityQuery(typeof(SpawnPointComponent))));
            _robustRandom.Shuffle(ents);
            var foundATarget = false;
            bestTarget = EntityCoordinates.Invalid;
            foreach (var entity in ents)
            {
                if (!entity.Transform.Coordinates.IsTileAirProbablySafe())
                    continue;
                var distanceFromNearest = float.PositiveInfinity;
                foreach (var existing in existingPlayerPoints)
                {
                    if (entity.Transform.Coordinates.TryDistance(_entityManager, existing, out var dist))
                        distanceFromNearest = Math.Min(distanceFromNearest, dist);
                }
                if (bestTargetDistanceFromNearest < distanceFromNearest)
                {
                    bestTarget = entity.Transform.Coordinates;
                    bestTargetDistanceFromNearest = distanceFromNearest;
                    foundATarget = true;
                }
            }
            return foundATarget;
        }

        public override bool OnGhostAttempt(Mind mind, bool canReturnGlobal)
        {
            var entity = mind.OwnedEntity;
            if ((entity != null) && (entity.TryGetComponent(out IMobStateComponent mobState)))
            {
                if (mobState.IsCritical())
                {
                    // TODO: This is copy/pasted from ghost code. Really, IDamagableComponent needs a method to reliably kill the target.
                    if (entity.TryGetComponent(out IDamageableComponent damageable))
                    {
                        //todo: what if they dont breathe lol
                        damageable.ChangeDamage(DamageType.Asphyxiation, 100, true);
                    }
                }
                else if (!mobState.IsDead())
                {
                    if (entity.HasComponent<HandsComponent>())
                    {
                        return false;
                    }
                }
            }
            var session = mind.Session;
            if (session == null)
                return false;
            _gameTicker.Respawn(session);
            return true;
        }

        public override string GetRoundEndDescription()
        {
            var lines = new List<string>();
            lines.Add("The PDAs recovered afterwards...");
            foreach (var entity in _entityManager.GetEntities(new TypeEntityQuery(typeof(PDAComponent))))
            {
                var pda = entity.GetComponent<PDAComponent>();
                var uplink = pda.SyndicateUplinkAccount;
                if ((uplink != null) && _allOriginalNames.ContainsKey(uplink))
                {
                    lines.Add(Loc.GetString("{0}'s PDA, with {1} TC", _allOriginalNames[uplink], uplink.Balance));
                }
            }
            return string.Join('\n', lines);
        }

        public override string ModeTitle => "Traitor Deathmatch";
        public override string Description => Loc.GetString("Everyone's a traitor. Everyone wants each other dead.");
    }
}
