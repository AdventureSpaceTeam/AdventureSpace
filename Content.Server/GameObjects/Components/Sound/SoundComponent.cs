using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Sound;
using SS14.Server.GameObjects.EntitySystems;
using SS14.Shared.Audio;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Log;
using SS14.Shared.Map;
using SS14.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Sound
{
    public class SoundComponent : SharedSoundComponent
    {
        /// <summary>
        /// Stops all sounds.
        /// </summary>
        /// <param name="channel">User that will be affected.</param>
        public void StopAllSounds(INetChannel channel)
        {
            SendNetworkMessage(new StopAllSoundsMessage(), channel);
        }

        /// <summary>
        /// Stops a certain scheduled sound from playing.
        /// </summary>
        /// <param name="channel">User that will be affected.</param>
        public void StopScheduledSound(string filename, INetChannel channel)
        {
            SendNetworkMessage(new StopSoundScheduleMessage(){Filename = filename}, channel);
        }

        /// <summary>
        /// Adds an scheduled sound to be played.
        /// </summary>
        /// <param name="channel">User that will be affected.</param>
        public void AddScheduledSound(ScheduledSound schedule, INetChannel channel)
        {
            SendNetworkMessage(new ScheduledSoundMessage() {Schedule = schedule}, channel);
        }

        public override void StopAllSounds()
        {
            StopAllSounds(null);
        }

        public override void StopScheduledSound(string filename)
        {
            StopScheduledSound(filename, null);
        }

        public override void AddScheduledSound(ScheduledSound schedule)
        {
            AddScheduledSound(schedule, null);
        }

        /// <summary>
        ///     Play an audio file following the entity.
        /// </summary>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        /// <param name="channel">User that will be affected.</param>
        public void Play(string filename, AudioParams? audioParams = null, INetChannel channel = null)
        {
            AddScheduledSound(new ScheduledSound()
            {
                Filename = filename,
                AudioParams = audioParams,
            }, channel);
        }
    }
}
