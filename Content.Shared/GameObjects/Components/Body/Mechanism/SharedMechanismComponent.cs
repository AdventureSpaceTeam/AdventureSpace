﻿#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.GameObjects.Components.Body.Behavior;
using Content.Shared.GameObjects.Components.Body.Part;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Shared.GameObjects.Components.Body.Mechanism
{
    public abstract class SharedMechanismComponent : Component, IMechanism, ISerializationHooks
    {
        public override string Name => "Mechanism";

        protected readonly Dictionary<int, object> OptionsCache = new();
        protected IBody? BodyCache;
        protected int IdHash;
        protected IEntity? PerformerCache;
        private IBodyPart? _part;

        [DataField("behaviors", serverOnly: true)] private HashSet<IMechanismBehavior> _behaviorTypes = new();

        private readonly Dictionary<Type, IMechanismBehavior> _behaviors = new();

        public IBody? Body => Part?.Body;

        public IBodyPart? Part
        {
            get => _part;
            set
            {
                if (_part == value)
                {
                    return;
                }

                var old = _part;
                _part = value;

                if (old != null)
                {
                    if (old.Body == null)
                    {
                        RemovedFromPart(old);
                    }
                    else
                    {
                        RemovedFromPartInBody(old.Body, old);
                    }
                }

                if (value != null)
                {
                    if (value.Body == null)
                    {
                        AddedToPart(value);
                    }
                    else
                    {
                        AddedToPartInBody(value.Body, value);
                    }
                }
            }
        }

        public IReadOnlyDictionary<Type, IMechanismBehavior> Behaviors => _behaviors;

        [DataField("maxDurability")] public int MaxDurability { get; set; } = 10;

        [DataField("currentDurability")] public int CurrentDurability { get; set; } = 10;

        [DataField("destroyThreshold")] public int DestroyThreshold { get; set; } = -10;

        // TODO BODY: Surgery description and adding a message to the examine tooltip of the entity that owns this mechanism
        // TODO BODY
        [DataField("resistance")] public int Resistance { get; set; } = 0;

        // TODO BODY OnSizeChanged
        [DataField("size")] public int Size { get; set; } = 1;

        [DataField("compatibility")]
        public BodyPartCompatibility Compatibility { get; set; } = BodyPartCompatibility.Universal;

        void ISerializationHooks.BeforeSerialization()
        {
            _behaviorTypes = _behaviors.Values.ToHashSet();
        }

        void ISerializationHooks.AfterDeserialization()
        {
            foreach (var behavior in _behaviorTypes)
            {
                var type = behavior.GetType();

                if (!_behaviors.TryAdd(type, behavior))
                {
                    Logger.Warning($"Duplicate behavior in {nameof(SharedMechanismComponent)}: {type}.");
                    continue;
                }

                IoCManager.InjectDependencies(behavior);
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            foreach (var behavior in _behaviors.Values)
            {
                behavior.Initialize(this);
            }
        }

        protected override void Startup()
        {
            base.Startup();

            foreach (var behavior in _behaviors.Values)
            {
                behavior.Startup();
            }
        }

        public bool EnsureBehavior<T>(out T behavior) where T : IMechanismBehavior, new()
        {
            if (_behaviors.TryGetValue(typeof(T), out var rawBehavior))
            {
                behavior = (T) rawBehavior;
                return true;
            }

            behavior = IoCManager.Resolve<IDynamicTypeFactory>().CreateInstance<T>();
            _behaviors.Add(typeof(T), behavior);
            behavior.Initialize(this);
            behavior.Startup();

            return false;
        }

        public bool HasBehavior<T>() where T : IMechanismBehavior
        {
            return _behaviors.ContainsKey(typeof(T));
        }

        public bool TryRemoveBehavior<T>() where T : IMechanismBehavior
        {
            return _behaviors.Remove(typeof(T));
        }

        public void Update(float frameTime)
        {
            foreach (var behavior in _behaviors.Values)
            {
                behavior.Update(frameTime);
            }
        }

        public void AddedToBody(IBody body)
        {
            DebugTools.AssertNotNull(Body);
            DebugTools.AssertNotNull(body);

            foreach (var behavior in _behaviors.Values)
            {
                behavior.AddedToBody(body);
            }
        }

        public void AddedToPart(IBodyPart part)
        {
            DebugTools.AssertNotNull(Part);
            DebugTools.AssertNotNull(part);

            Owner.Transform.AttachParent(part.Owner);

            foreach (var behavior in _behaviors.Values)
            {
                behavior.AddedToPart(part);
            }
        }

        public void AddedToPartInBody(IBody body, IBodyPart part)
        {
            DebugTools.AssertNotNull(Body);
            DebugTools.AssertNotNull(body);
            DebugTools.AssertNotNull(Part);
            DebugTools.AssertNotNull(part);

            Owner.Transform.AttachParent(part.Owner);

            foreach (var behavior in _behaviors.Values)
            {
                behavior.AddedToPartInBody(body, part);
            }
        }

        public void RemovedFromBody(IBody old)
        {
            DebugTools.AssertNull(Body);
            DebugTools.AssertNotNull(old);

            foreach (var behavior in _behaviors.Values)
            {
                behavior.RemovedFromBody(old);
            }
        }

        public void RemovedFromPart(IBodyPart old)
        {
            DebugTools.AssertNull(Part);
            DebugTools.AssertNotNull(old);

            Owner.Transform.AttachToGridOrMap();

            foreach (var behavior in _behaviors.Values)
            {
                behavior.RemovedFromPart(old);
            }
        }

        public void RemovedFromPartInBody(IBody oldBody, IBodyPart oldPart)
        {
            DebugTools.AssertNull(Body);
            DebugTools.AssertNotNull(oldBody);
            DebugTools.AssertNull(Part);
            DebugTools.AssertNotNull(oldPart);

            Owner.Transform.AttachToGridOrMap();

            foreach (var behavior in _behaviors.Values)
            {
                behavior.RemovedFromPartInBody(oldBody, oldPart);
            }
        }
    }
}
