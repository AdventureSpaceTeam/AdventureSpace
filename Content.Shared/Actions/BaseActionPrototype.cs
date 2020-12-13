﻿using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Interfaces;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Actions
{
    /// <summary>
    /// Base class for action prototypes.
    /// </summary>
    public abstract class BaseActionPrototype : IPrototype
    {
        /// <summary>
        /// Icon representing this action in the UI.
        /// </summary>
        [ViewVariables]
        public SpriteSpecifier Icon { get; private set; }

        /// <summary>
        /// For toggle actions only, icon to show when toggled on. If omitted,
        /// the action will simply be highlighted when turned on.
        /// </summary>
        [ViewVariables]
        public SpriteSpecifier IconOn { get; private set; }



        /// <summary>
        /// Name to show in UI. Accepts formatting.
        /// </summary>
        public FormattedMessage Name { get; private set; }

        /// <summary>
        /// Description to show in UI. Accepts formatting.
        /// </summary>
        public FormattedMessage Description { get; private set; }

        /// <summary>
        /// Requirements message to show in UI. Accepts formatting, but generally should be avoided
        /// so the requirements message isn't too prominent in the tooltip.
        /// </summary>
        public string Requires { get; private set; }

        /// <summary>
        /// The type of behavior this action has. This is valid clientside and serverside.
        /// </summary>
        public BehaviorType BehaviorType { get; protected set; }

        /// <summary>
        /// For targetpoint or targetentity actions, if this is true the action will remain
        /// selected after it is used, so it can be continuously re-used. If this is false,
        /// the action will be deselected after one use.
        /// </summary>
        public bool Repeat { get; private set; }

        /// <summary>
        /// Filters that can be used to filter this item in action menu.
        /// </summary>
        public IEnumerable<string> Filters { get; private set; }

        /// <summary>
        /// Keywords that can be used to search this item in action menu.
        /// </summary>
        public IEnumerable<string> Keywords { get; private set; }

        public virtual void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataReadFunction("name", string.Empty,
                s => Name = FormattedMessage.FromMarkup(s));
            serializer.DataReadFunction("description", string.Empty,
                s => Description = FormattedMessage.FromMarkup(s));

            serializer.DataField(this, x => x.Requires,"requires", null);
            serializer.DataField(this, x => x.Icon,"icon", SpriteSpecifier.Invalid);
            serializer.DataField(this, x => x.IconOn,"iconOn", SpriteSpecifier.Invalid);

            // client needs to know what type of behavior it is even if the actual implementation is only
            // on server side. If we wanted to avoid this we'd need to always add a shared or clientside interface
            // for each action even if there was only server-side logic, which would be cumbersome
            serializer.DataField(this, x => x.BehaviorType, "behaviorType", BehaviorType.None);
            if (BehaviorType == BehaviorType.None)
            {
                Logger.ErrorS("action", "Missing behaviorType for action with name {0}", Name);
            }

            if (BehaviorType != BehaviorType.Toggle && IconOn != SpriteSpecifier.Invalid)
            {
                Logger.ErrorS("action", "for action {0}, iconOn was specified but behavior" +
                                        " type was {1}. iconOn is only supported for Toggle behavior type.", Name);
            }

            serializer.DataField(this, x => x.Repeat, "repeat", false);
            if (Repeat && BehaviorType != BehaviorType.TargetEntity && BehaviorType != BehaviorType.TargetPoint)
            {
                Logger.ErrorS("action", " action named {0} used repeat: true, but this is only supported for" +
                                        " TargetEntity and TargetPoint behaviorType and its behaviorType is {1}",
                    Name, BehaviorType);
            }

            serializer.DataReadFunction("filters", new List<string>(),
                rawTags =>
                {
                    Filters = rawTags.Select(rawTag => rawTag.Trim()).ToList();
                });

            serializer.DataReadFunction("keywords", new List<string>(),
                rawTags =>
                {
                    Keywords = rawTags.Select(rawTag => rawTag.Trim()).ToList();
                });
        }

        protected void ValidateBehaviorType(BehaviorType expected, Type actualInterface)
        {
            if (BehaviorType != expected)
            {
                Logger.ErrorS("action", "for action named {0}, behavior implements " +
                                        "{1}, so behaviorType should be {2} but was {3}", Name, actualInterface.Name, expected, BehaviorType);
            }
        }
    }

    /// <summary>
    /// The behavior / logic of the action. Each of these corresponds to a particular IActionBehavior
    /// (for actions) or IItemActionBehavior (for item actions)
    /// interface. Corresponds to action.behaviorType in YAML
    /// </summary>
    public enum BehaviorType
    {
        /// <summary>
        /// Action doesn't do anything.
        /// </summary>
        None,

        /// <summary>
        /// IInstantAction/IInstantItemAction. Action which does something immediately when used and has
        /// no target.
        /// </summary>
        Instant,

        /// <summary>
        /// IToggleAction/IToggleItemAction Action which can be toggled on and off
        /// </summary>
        Toggle,

        /// <summary>
        /// ITargetEntityAction/ITargetEntityItemAction. Action which is used on a targeted entity.
        /// </summary>
        TargetEntity,

        /// <summary>
        /// ITargetPointAction/ITargetPointItemAction. Action which requires the user to select a target point, which
        /// does not necessarily have an entity on it.
        /// </summary>
        TargetPoint
    }
}
