﻿using Content.Shared.Interfaces;

namespace Content.Client.Utility
{
    /// <summary>
    /// Client implementation of IModuleManager.
    /// Provides simple way for shared code to check if it's being run by
    /// the client of the server.
    /// </summary>
    public class ClientModuleManager : IModuleManager
    {
        bool IModuleManager.IsClientModule => true;
        bool IModuleManager.IsServerModule => false;
    }
}
