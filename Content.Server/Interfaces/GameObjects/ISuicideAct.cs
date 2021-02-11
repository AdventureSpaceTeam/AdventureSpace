﻿using Content.Server.Interfaces.Chat;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Server.Interfaces.GameObjects
{
    [RequiresExplicitImplementation]
    public interface ISuicideAct
    {
        public SuicideKind Suicide(IEntity victim, IChatManager chat);
    }

    public enum SuicideKind
    {
        Special, //Doesn't damage the mob, used for "weird" suicides like gibbing

        //Damage type suicides
        Blunt,
        Slash,
        Piercing,
        Heat,
        Shock,
        Cold,
        Poison,
        Radiation,
        Asphyxiation,
        Bloodloss
    }
}
