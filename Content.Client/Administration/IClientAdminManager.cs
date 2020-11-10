﻿using System;
using Content.Shared.Administration;

#nullable enable

namespace Content.Client.Administration
{
    /// <summary>
    ///     Manages server admin permissions for the local player.
    /// </summary>
    public interface IClientAdminManager
    {
        /// <summary>
        ///     Fired when the admin status of the local player changes, such as losing admin privileges.
        /// </summary>
        event Action AdminStatusUpdated;

        /// <summary>
        ///     Checks whether the local player has an admin flag.
        /// </summary>
        /// <param name="flag">The flags to check. Multiple flags can be specified, they must all be held.</param>
        /// <returns>False if the local player is not an admin, inactive, or does not have all the flags specified.</returns>
        bool HasFlag(AdminFlags flag);

        /// <summary>
        ///     Check if a player can execute a specified console command.
        /// </summary>
        bool CanCommand(string cmdName);

        /// <summary>
        ///     Check if the local player can open the VV menu.
        /// </summary>
        bool CanViewVar();

        /// <summary>
        ///     Check if the local player can spawn stuff in with the entity/tile spawn panel.
        /// </summary>
        bool CanAdminPlace();

        /// <summary>
        ///     Check if the local player can execute server-side C# scripts.
        /// </summary>
        bool CanScript();

        /// <summary>
        ///     Check if the local player can open the admin menu.
        /// </summary>
        bool CanAdminMenu();

        void Initialize();
    }
}
