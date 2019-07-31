﻿using System;
using Content.Shared.Audio;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Sound
{
    /// <summary>
    /// Changes footstep sound
    /// </summary>
    [RegisterComponent]
    public class FootstepModifierComponent : Component
    {
        #pragma warning disable 649
        [Dependency] private readonly IPrototypeManager _prototypeManager;
        #pragma warning restore 649
        /// <inheritdoc />
        ///
        private Random _footstepRandom;

        public override string Name => "FootstepModifier";

        public string _soundCollectionName;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _soundCollectionName, "footstepSoundCollection", "");
        }

        public override void Initialize()
        {
            base.Initialize();
            _footstepRandom = new Random(Owner.Uid.GetHashCode() ^ DateTime.Now.GetHashCode());
        }

        public void PlayFootstep()
        {
            if (!string.IsNullOrWhiteSpace(_soundCollectionName))
            {
                var soundCollection = _prototypeManager.Index<SoundCollectionPrototype>(_soundCollectionName);
                var file = _footstepRandom.Pick(soundCollection.PickFiles);
                Owner.GetComponent<SoundComponent>().Play(file, AudioParams.Default.WithVolume(-2f));
            }
        }
    }
}
