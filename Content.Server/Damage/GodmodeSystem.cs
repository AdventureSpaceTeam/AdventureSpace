﻿#nullable enable
using System.Collections.Generic;
using Content.Server.Atmos.Components;
using System.Linq;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Resistances;
using Content.Shared.GameTicking;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Damage
{
    [UsedImplicitly]
    public class GodmodeSystem : EntitySystem, IResettingEntitySystem
    {
        private readonly Dictionary<IEntity, OldEntityInformation> _entities = new();

        public void Reset()
        {
            _entities.Clear();
        }

        public bool EnableGodmode(IEntity entity)
        {
            if (_entities.ContainsKey(entity))
            {
                return false;
            }

            _entities[entity] = new OldEntityInformation(entity);

            if (entity.TryGetComponent(out MovedByPressureComponent? moved))
            {
                moved.Enabled = false;
            }

            if (entity.TryGetComponent(out IDamageableComponent? damageable))
            {
                damageable.SupportedTypes.Clear();
                damageable.SupportedClasses.Clear();
            }

            return true;
        }

        public bool HasGodmode(IEntity entity)
        {
            return _entities.ContainsKey(entity);
        }

        public bool DisableGodmode(IEntity entity)
        {
            if (!_entities.Remove(entity, out var old))
            {
                return false;
            }

            if (entity.TryGetComponent(out MovedByPressureComponent? moved))
            {
                moved.Enabled = old.MovedByPressure;
            }

            if (entity.TryGetComponent(out IDamageableComponent? damageable))
            {
                if (old.SupportedTypes != null)
                {
                    damageable.SupportedTypes.UnionWith(old.SupportedTypes);
                }

                if (old.SupportedClasses != null)
                {
                    damageable.SupportedClasses.UnionWith(old.SupportedClasses);
                }
            }

            return true;
        }

        /// <summary>
        ///     Toggles godmode for a given entity.
        /// </summary>
        /// <param name="entity">The entity to toggle godmode for.</param>
        /// <returns>true if enabled, false if disabled.</returns>
        public bool ToggleGodmode(IEntity entity)
        {
            if (HasGodmode(entity))
            {
                DisableGodmode(entity);
                return false;
            }
            else
            {
                EnableGodmode(entity);
                return true;
            }
        }

        public class OldEntityInformation
        {
            public OldEntityInformation(IEntity entity)
            {
                Entity = entity;
                MovedByPressure = entity.IsMovedByPressure();

                if (entity.TryGetComponent(out IDamageableComponent? damageable))
                {
                    SupportedTypes = damageable.SupportedTypes.ToHashSet();
                    SupportedClasses = damageable.SupportedClasses.ToHashSet();
                }
            }

            public IEntity Entity { get; }

            public bool MovedByPressure { get; }

            public HashSet<DamageType>? SupportedTypes { get; }

            public HashSet<DamageClass>? SupportedClasses { get; }
        }
    }
}
