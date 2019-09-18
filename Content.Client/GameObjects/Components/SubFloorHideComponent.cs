﻿using Content.Shared.Maps;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;

namespace Content.Client.GameObjects.Components
{
    /// <summary>
    ///     Simple component that automatically hides the sibling <see cref="ISpriteComponent"/> when the tile it's on
    ///     is not a sub floor (plating).
    /// </summary>
    /// <seealso cref="ContentTileDefinition.IsSubFloor"/>
    [RegisterComponent]
    public sealed class SubFloorHideComponent : Component
    {
        private SnapGridComponent _snapGridComponent;

        public override string Name => "SubFloorHide";

        public override void Initialize()
        {
            base.Initialize();

            _snapGridComponent = Owner.GetComponent<SnapGridComponent>();
        }

        /// <inheritdoc />
        protected override void Startup()
        {
            base.Startup();

            _snapGridComponent.OnPositionChanged += SnapGridOnPositionChanged;
            Owner.EntityManager.RaiseEvent(Owner, new SubFloorHideDirtyEvent());
        }

        /// <inheritdoc />
        protected override void Shutdown()
        {
            base.Shutdown();

            _snapGridComponent.OnPositionChanged -= SnapGridOnPositionChanged;
        }

        private void SnapGridOnPositionChanged()
        {
            Owner.EntityManager.RaiseEvent(Owner, new SubFloorHideDirtyEvent());
        }
    }

    internal sealed class SubFloorHideDirtyEvent : EntitySystemMessage
    {
    }
}
