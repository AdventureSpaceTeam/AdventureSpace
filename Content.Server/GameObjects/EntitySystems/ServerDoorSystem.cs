#nullable enable
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    /// <summary>
    /// Used on the server side to manage global access level overrides.
    /// </summary>
    class ServerDoorSystem : EntitySystem
    {
        /// <summary>
        ///     Determines the base access behavior of all doors on the station.
        /// </summary>
        public AccessTypes AccessType { get; set; }

        /// <summary>
        /// How door access should be handled.
        /// </summary>
        public enum AccessTypes
        {
            /// <summary> ID based door access. </summary>
            Id,
            /// <summary>
            /// Allows everyone to open doors, except external which airlocks are still handled with ID's
            /// </summary>
            AllowAllIdExternal,
            /// <summary>
            /// Allows everyone to open doors, except external airlocks which are never allowed, even if the user has
            /// ID access.
            /// </summary>
            AllowAllNoExternal,
            /// <summary> Allows everyone to open all doors. </summary>
            AllowAll
        }

        public override void Initialize()
        {
            base.Initialize();

            AccessType = AccessTypes.Id;
        }
    }
}
