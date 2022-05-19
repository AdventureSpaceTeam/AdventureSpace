using Content.Shared.GameTicking;

namespace Content.Server.Polymorph.Systems
{
    public sealed partial class PolymorphableSystem : EntitySystem
    {
        public EntityUid? PausedMap { get; private set; } = null;

        /// <summary>
        /// Used to subscribe to the round restart event
        /// </summary>
        private void InitializeMap()
        {
            SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        }

        private void OnRoundRestart(RoundRestartCleanupEvent _)
        {
            if (PausedMap == null || !Exists(PausedMap))
                return;

            EntityManager.DeleteEntity(PausedMap.Value);
        }

        /// <summary>
        /// Used internally to ensure a paused map that is
        /// stores polymorphed entities.
        /// </summary>
        private void EnsurePausesdMap()
        {
            if (PausedMap != null && Exists(PausedMap))
                return;
            
            var newmap = _mapManager.CreateMap();
            _mapManager.SetMapPaused(newmap, true);
            PausedMap = _mapManager.GetMapEntityId(newmap);

            Dirty(PausedMap.Value);
        }
    }
}
