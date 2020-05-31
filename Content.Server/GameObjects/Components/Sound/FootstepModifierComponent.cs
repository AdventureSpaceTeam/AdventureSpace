﻿using Content.Shared.Audio;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
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
        [Dependency] private readonly IRobustRandom _footstepRandom;
#pragma warning restore 649
        /// <inheritdoc />
        ///

        public override string Name => "FootstepModifier";

        public string _soundCollectionName;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _soundCollectionName, "footstepSoundCollection", "");
        }

        public void PlayFootstep()
        {
            if (!string.IsNullOrWhiteSpace(_soundCollectionName))
            {
                var soundCollection = _prototypeManager.Index<SoundCollectionPrototype>(_soundCollectionName);
                var file = _footstepRandom.Pick(soundCollection.PickFiles);
                EntitySystem.Get<AudioSystem>().Play(file, Owner, AudioParams.Default.WithVolume(-2f));
            }
        }
    }
}
