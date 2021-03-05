#nullable enable
using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Items.Storage
{
    [RegisterComponent]
    public sealed class StorageFillComponent : Component, IMapInit
    {
        public override string Name => "StorageFill";

        [DataField("contents")]
        private List<StorageFillEntry> _contents = new();

        public IReadOnlyList<StorageFillEntry> Contents => _contents;

        void IMapInit.MapInit()
        {
            if (_contents.Count == 0)
            {
                return;
            }

            if (!Owner.TryGetComponent(out IStorageComponent? storage))
            {
                Logger.Error($"StorageFillComponent couldn't find any StorageComponent ({Owner})");
                return;
            }

            var random = IoCManager.Resolve<IRobustRandom>();

            var alreadySpawnedGroups = new List<string>();
            foreach (var storageItem in _contents)
            {
                if (string.IsNullOrEmpty(storageItem.PrototypeName)) continue;
                if (!string.IsNullOrEmpty(storageItem.GroupId) && alreadySpawnedGroups.Contains(storageItem.GroupId)) continue;

                if (storageItem.SpawnProbability != 1f &&
                    !random.Prob(storageItem.SpawnProbability))
                {
                    continue;
                }

                for (var i = 0; i < storageItem.Amount; i++)
                {
                    storage.Insert(Owner.EntityManager.SpawnEntity(storageItem.PrototypeName, Owner.Transform.Coordinates));
                }
                if (!string.IsNullOrEmpty(storageItem.GroupId)) alreadySpawnedGroups.Add(storageItem.GroupId);
            }
        }

        [Serializable]
        [DataDefinition]
        public struct StorageFillEntry : IPopulateDefaultValues
        {
            [DataField("name")]
            public string? PrototypeName;

            [DataField("prob")]
            public float SpawnProbability;

            [DataField("orGroup")]
            public string GroupId;

            [DataField("amount")]
            public int Amount;

            public void PopulateDefaultValues()
            {
                Amount = 1;
                SpawnProbability = 1;
            }
        }
    }
}
