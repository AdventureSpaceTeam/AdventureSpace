﻿using Content.Shared.Audio;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Items
{
    [RegisterComponent]
    public class ToysComponent : Component, IActivate, IUse, ILand
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        public override string Name => "Toys";

        [ViewVariables]
        public string _soundCollectionName = "ToySqueak";

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _soundCollectionName, "toySqueak", "ToySqueak");
        }

        public void Squeak()
        {
            PlaySqueakEffect();
        }

        public void PlaySqueakEffect()
        {
            if (!string.IsNullOrWhiteSpace(_soundCollectionName))
            {
                var soundCollection = _prototypeManager.Index<SoundCollectionPrototype>(_soundCollectionName);
                var file = _random.Pick(soundCollection.PickFiles);
                EntitySystem.Get<AudioSystem>().PlayFromEntity(file, Owner, AudioParams.Default);
            }
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            Squeak();
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            Squeak();
            return false;
        }

        void ILand.Land(LandEventArgs eventArgs)
        {
            Squeak();
        }
    }
}

