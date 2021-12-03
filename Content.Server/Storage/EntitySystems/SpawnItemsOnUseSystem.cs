﻿using System.Collections.Generic;
using Content.Server.Storage.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Storage.EntitySystems
{
    public class SpawnItemsOnUseSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SpawnItemsOnUseComponent, UseInHandEvent>(OnUseInHand);
        }

        private void OnUseInHand(EntityUid uid, SpawnItemsOnUseComponent component, UseInHandEvent args)
        {
            if (args.Handled)
                return;

            var owner = EntityManager.GetEntity(uid);
            var alreadySpawnedGroups = new List<string>();
            IEntity? entityToPlaceInHands = null;
            foreach (var storageItem in component.Items)
            {
                if (!string.IsNullOrEmpty(storageItem.GroupId) &&
                    alreadySpawnedGroups.Contains(storageItem.GroupId)) continue;

                if (storageItem.SpawnProbability != 1f &&
                    !_random.Prob(storageItem.SpawnProbability))
                {
                    continue;
                }

                for (var i = 0; i < storageItem.Amount; i++)
                {
                    entityToPlaceInHands = EntityManager.SpawnEntity(storageItem.PrototypeId, IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(args.User).Coordinates);
                }

                if (!string.IsNullOrEmpty(storageItem.GroupId)) alreadySpawnedGroups.Add(storageItem.GroupId);
            }

            if (component.Sound != null)
                SoundSystem.Play(Filter.Pvs(owner), component.Sound.GetSound());

            component.Uses--;
            if (component.Uses == 0)
            {
                args.Handled = true;
                IoCManager.Resolve<IEntityManager>().DeleteEntity((EntityUid) owner);
            }

            if (entityToPlaceInHands != null
                && IoCManager.Resolve<IEntityManager>().TryGetComponent<SharedHandsComponent?>(args.User, out var hands))
            {
                hands.TryPutInAnyHand(entityToPlaceInHands);
            }
        }
    }
}
