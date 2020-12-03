﻿namespace Content.Server.GameObjects.Components.Mobs.Speech
{
    internal interface IAccentComponent
    {
        /// <summary>
        /// Transforms a message with the given Accent
        /// </summary>
        /// <param name="message">The spoken message</param>
        /// <returns>The message after the transformation</returns>
        public string Accentuate(string message);
    }
}
