﻿#nullable enable
using Content.Shared.DragDrop;
using Content.Shared.Interaction;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Climbing
{
    public interface IClimbable { }

    public abstract class SharedClimbableComponent : Component, IClimbable, IDragDropOn
    {
        public sealed override string Name => "Climbable";

        /// <summary>
        ///     The range from which this entity can be climbed.
        /// </summary>
        [ViewVariables] [DataField("range")] protected float Range = SharedInteractionSystem.InteractionRange / 1.4f;

        public virtual bool CanDragDropOn(DragDropEvent eventArgs)
        {
            return eventArgs.Dragged.HasComponent<SharedClimbingComponent>();
        }

        public abstract bool DragDropOn(DragDropEvent eventArgs);
    }
}
