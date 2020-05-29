﻿using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Nutrition;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Nutrition
{
    /// <summary>
    /// This container acts as a master object for things like Pizza, which holds slices.
    /// </summary>
    /// TODO: Perhaps implement putting food back (pizza boxes) but that really isn't mandatory.
    /// This doesn't even need to have an actual Container for right now.
    [RegisterComponent]
    public sealed class FoodContainer : SharedFoodContainerComponent, IUse
    {
#pragma warning disable 649
        [Dependency] private readonly IRobustRandom _random;
        [Dependency] private readonly IEntityManager _entityManager;
#pragma warning restore 649
        public override string Name => "FoodContainer";

        private AppearanceComponent _appearance;
        private Dictionary<string, int> _prototypes;
        private uint _capacity;

        public int Capacity => (int)_capacity;
        [ViewVariables]
        public int Count => _count;

        private int _count = 0;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _prototypes, "prototypes", null);
            serializer.DataField<uint>(ref _capacity, "capacity", 5);
        }

        public override void Initialize()
        {
            base.Initialize();
            Owner.TryGetComponent(out _appearance);
            _count = Capacity;
            UpdateAppearance();

        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {

            var hands = eventArgs.User.TryGetComponent(out HandsComponent handsComponent);
            var itemToSpawn = _entityManager.SpawnEntity(GetRandomPrototype(), Owner.Transform.GridPosition);
            handsComponent.PutInHandOrDrop(itemToSpawn.GetComponent<ItemComponent>());
            _count--;
            if (_count < 1)
            {
                Owner.Delete();
                return false;
            }
            return true;

        }


        private string GetRandomPrototype()
        {
            var defaultProto = _prototypes.Keys.FirstOrDefault();
            if (_prototypes.Count == 1)
            {
                return defaultProto;
            }
            var probResult = _random.Next(0, 100);
            var total = 0;
            foreach (var item in _prototypes)
            {
                total += item.Value;
                if (probResult < total)
                {
                    return item.Key;
                }
            }

            return defaultProto;
        }

        private void UpdateAppearance()
        {
            _appearance?.SetData(FoodContainerVisuals.Current, Count);
        }
    }
}
