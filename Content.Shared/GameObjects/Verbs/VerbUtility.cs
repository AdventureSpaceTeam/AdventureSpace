﻿using System;
using System.Collections.Generic;
using System.Reflection;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Utility;

namespace Content.Shared.GameObjects
{
    public static class VerbUtility
    {
        public const float InteractionRange = 2;
        public const float InteractionRangeSquared = InteractionRange * InteractionRange;

        // TODO: This is a quick hack. Verb objects should absolutely be cached properly.
        // This works for now though.
        public static IEnumerable<(IComponent, Verb)> GetVerbs(IEntity entity)
        {
            foreach (var component in entity.GetAllComponents())
            {
                var type = component.GetType();
                foreach (var nestedType in type.GetAllNestedTypes())
                {
                    if (!typeof(Verb).IsAssignableFrom(nestedType) || nestedType.IsAbstract)
                    {
                        continue;
                    }

                    var verb = (Verb)Activator.CreateInstance(nestedType);
                    yield return (component, verb);
                }
            }
        }

        /// <summary>
        /// Returns an IEnumerable of all classes inheriting <see cref="GlobalVerb"/> with the <see cref="GlobalVerbAttribute"/> attribute.
        /// </summary>
        /// <param name="assembly">The assembly to search for global verbs in.</param>
        public static IEnumerable<GlobalVerb> GetGlobalVerbs(Assembly assembly)
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (Attribute.IsDefined(type, typeof(GlobalVerbAttribute)))
                {
                    if (!typeof(GlobalVerb).IsAssignableFrom(type) || type.IsAbstract)
                    {
                        continue;
                    }
                    yield return (GlobalVerb)Activator.CreateInstance(type);
                }
            }
        }

        public static bool InVerbUseRange(IEntity user, IEntity target)
        {
            var distanceSquared = (user.Transform.WorldPosition - target.Transform.WorldPosition)
                .LengthSquared;
            if (distanceSquared > InteractionRangeSquared)
            {
                return false;
            }
            return true;
        }
    }
}
