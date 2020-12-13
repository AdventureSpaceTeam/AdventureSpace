﻿using System;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;

namespace Content.Shared.Alert
{
    /// <summary>
    /// Defines what should happen when an alert is clicked.
    /// </summary>
    public interface IAlertClick : IExposeData
    {

        /// <summary>
        /// Invoked on server side when user clicks an alert.
        /// </summary>
        /// <param name="args"></param>
        void AlertClicked(ClickAlertEventArgs args);
    }

    public class ClickAlertEventArgs : EventArgs
    {
        /// <summary>
        /// Player clicking the alert
        /// </summary>
        public readonly IEntity Player;
        /// <summary>
        /// Alert that was clicked
        /// </summary>
        public readonly AlertPrototype Alert;

        public ClickAlertEventArgs(IEntity player, AlertPrototype alert)
        {
            Player = player;
            Alert = alert;
        }
    }
}
