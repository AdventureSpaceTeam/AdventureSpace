using Content.Shared.GameObjects.Components.Research;
using Content.Shared.Research;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Research
{
    [RegisterComponent]
    public class  TechnologyDatabaseComponent : SharedTechnologyDatabaseComponent
    {
        public override ComponentState GetComponentState()
        {
            return new TechnologyDatabaseState(_technologies);
        }

        /// <summary>
        ///     Synchronizes this database against other,
        ///     adding all technologies from the other that
        ///     this one doesn't have.
        /// </summary>
        /// <param name="otherDatabase">The other database</param>
        /// <param name="twoway">Whether the other database should be synced against this one too or not.</param>
        public void Sync(TechnologyDatabaseComponent otherDatabase, bool twoway = true)
        {
            foreach (var tech in otherDatabase.Technologies)
            {
                if (!IsTechnologyUnlocked(tech)) AddTechnology(tech);
            }

            if(twoway)
                otherDatabase.Sync(this, false);

            Dirty();
        }

        /// <summary>
        ///     If there's a research client component attached to the owner entity,
        ///     and the research client is connected to a research server, this method
        ///     syncs against the research server, and the server against the local database.
        /// </summary>
        /// <returns>Whether it could sync or not</returns>
        public bool SyncWithServer()
        {
            if (!Owner.TryGetComponent(out ResearchClientComponent client)) return false;
            if (!client.ConnectedToServer) return false;

            Sync(client.Server.Database);

            return true;
        }

        /// <summary>
        ///     If possible, unlocks a technology on this database.
        /// </summary>
        /// <param name="technology"></param>
        /// <returns></returns>
        public bool UnlockTechnology(TechnologyPrototype technology)
        {
            if (!CanUnlockTechnology(technology)) return false;

            AddTechnology(technology);
            Dirty();
            return true;
        }

        /// <summary>
        ///     Adds a technology to the database without checking if it could be unlocked.
        /// </summary>
        /// <param name="technology"></param>
        public void AddTechnology(TechnologyPrototype technology)
        {
            _technologies.Add(technology);
        }
    }
}
