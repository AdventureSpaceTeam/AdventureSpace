﻿using System;
using System.Collections.Generic;
using Content.Client.GameObjects.EntitySystems;
using JetBrains.Annotations;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Content.Client.GameObjects.Components.IconSmoothing
{
    // TODO: Potential improvements:
    //  Defer updating of these.
    //  Get told by somebody to use a loop.
    /// <summary>
    ///     Makes sprites of other grid-aligned entities like us connect.
    /// </summary>
    /// <remarks>
    ///     The system is based on Baystation12's smoothwalling, and thus will work with those.
    ///     To use, set <c>base</c> equal to the prefix of the corner states in the sprite base RSI.
    ///     Any objects with the same <c>key</c> will connect.
    /// </remarks>
    [RegisterComponent]
    public class IconSmoothComponent : Component
    {
        private string _smoothKey;
        private string _stateBase;
        private IconSmoothingMode _mode;

        public override string Name => "IconSmooth";

        internal ISpriteComponent Sprite { get; private set; }
        internal SnapGridComponent SnapGrid { get; private set; }
        private (GridId, Vector2i) _lastPosition;

        /// <summary>
        ///     We will smooth with other objects with the same key.
        /// </summary>
        public string SmoothKey => _smoothKey;

        /// <summary>
        ///     Prepended to the RSI state.
        /// </summary>
        public string StateBase => _stateBase;

        /// <summary>
        ///     Mode that controls how the icon should be selected.
        /// </summary>
        public IconSmoothingMode Mode => _mode;

        /// <summary>
        ///     Used by <see cref="IconSmoothSystem"/> to reduce redundant updates.
        /// </summary>
        internal int UpdateGeneration { get; set; }

        public override void Initialize()
        {
            base.Initialize();

            SnapGrid = Owner.GetComponent<SnapGridComponent>();
            Sprite = Owner.GetComponent<ISpriteComponent>();
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataFieldCached(ref _stateBase, "base", "");
            serializer.DataFieldCached(ref _smoothKey, "key", null);
            serializer.DataFieldCached(ref _mode, "mode", IconSmoothingMode.Corners);
        }

        /// <inheritdoc />
        protected override void Startup()
        {
            base.Startup();

            SnapGrid.OnPositionChanged += SnapGridOnPositionChanged;
            // ensures lastposition initial value is populated on spawn. Just calling
            // the hook here would cause a dirty event to fire needlessly
            _lastPosition = (Owner.Transform.GridID, SnapGrid.Position);
            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new IconSmoothDirtyEvent(Owner,null, SnapGrid.Offset, Mode));
            if (Mode == IconSmoothingMode.Corners)
            {
                var state0 = $"{StateBase}0";
                Sprite.LayerMapSet(CornerLayers.SE, Sprite.AddLayerState(state0));
                Sprite.LayerSetDirOffset(CornerLayers.SE, DirectionOffset.None);
                Sprite.LayerMapSet(CornerLayers.NE, Sprite.AddLayerState(state0));
                Sprite.LayerSetDirOffset(CornerLayers.NE, DirectionOffset.CounterClockwise);
                Sprite.LayerMapSet(CornerLayers.NW, Sprite.AddLayerState(state0));
                Sprite.LayerSetDirOffset(CornerLayers.NW, DirectionOffset.Flip);
                Sprite.LayerMapSet(CornerLayers.SW, Sprite.AddLayerState(state0));
                Sprite.LayerSetDirOffset(CornerLayers.SW, DirectionOffset.Clockwise);
            }
        }

        internal virtual void CalculateNewSprite()
        {
            switch (Mode)
            {
                case IconSmoothingMode.Corners:
                    CalculateNewSpriteCorners();
                    break;
                case IconSmoothingMode.CardinalFlags:
                    CalculateNewSpriteCardinal();
                    break;
                case IconSmoothingMode.NoSprite:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void CalculateNewSpriteCardinal()
        {
            var dirs = CardinalConnectDirs.None;

            if (MatchingEntity(SnapGrid.GetInDir(Direction.North)))
                dirs |= CardinalConnectDirs.North;
            if (MatchingEntity(SnapGrid.GetInDir(Direction.South)))
                dirs |= CardinalConnectDirs.South;
            if (MatchingEntity(SnapGrid.GetInDir(Direction.East)))
                dirs |= CardinalConnectDirs.East;
            if (MatchingEntity(SnapGrid.GetInDir(Direction.West)))
                dirs |= CardinalConnectDirs.West;

            Sprite.LayerSetState(0, $"{StateBase}{(int) dirs}");
        }

        private void CalculateNewSpriteCorners()
        {
            var (cornerNE, cornerNW, cornerSW, cornerSE) = CalculateCornerFill();

            Sprite.LayerSetState(CornerLayers.NE, $"{StateBase}{(int) cornerNE}");
            Sprite.LayerSetState(CornerLayers.SE, $"{StateBase}{(int) cornerSE}");
            Sprite.LayerSetState(CornerLayers.SW, $"{StateBase}{(int) cornerSW}");
            Sprite.LayerSetState(CornerLayers.NW, $"{StateBase}{(int) cornerNW}");
        }

        protected (CornerFill ne, CornerFill nw, CornerFill sw, CornerFill se) CalculateCornerFill()
        {
            var n = MatchingEntity(SnapGrid.GetInDir(Direction.North));
            var ne = MatchingEntity(SnapGrid.GetInDir(Direction.NorthEast));
            var e = MatchingEntity(SnapGrid.GetInDir(Direction.East));
            var se = MatchingEntity(SnapGrid.GetInDir(Direction.SouthEast));
            var s = MatchingEntity(SnapGrid.GetInDir(Direction.South));
            var sw = MatchingEntity(SnapGrid.GetInDir(Direction.SouthWest));
            var w = MatchingEntity(SnapGrid.GetInDir(Direction.West));
            var nw = MatchingEntity(SnapGrid.GetInDir(Direction.NorthWest));

            // ReSharper disable InconsistentNaming
            var cornerNE = CornerFill.None;
            var cornerSE = CornerFill.None;
            var cornerSW = CornerFill.None;
            var cornerNW = CornerFill.None;
            // ReSharper restore InconsistentNaming

            if (n)
            {
                cornerNE |= CornerFill.CounterClockwise;
                cornerNW |= CornerFill.Clockwise;
            }

            if (ne)
            {
                cornerNE |= CornerFill.Diagonal;
            }

            if (e)
            {
                cornerNE |= CornerFill.Clockwise;
                cornerSE |= CornerFill.CounterClockwise;
            }

            if (se)
            {
                cornerSE |= CornerFill.Diagonal;
            }

            if (s)
            {
                cornerSE |= CornerFill.Clockwise;
                cornerSW |= CornerFill.CounterClockwise;
            }

            if (sw)
            {
                cornerSW |= CornerFill.Diagonal;
            }

            if (w)
            {
                cornerSW |= CornerFill.Clockwise;
                cornerNW |= CornerFill.CounterClockwise;
            }

            if (nw)
            {
                cornerNW |= CornerFill.Diagonal;
            }

            switch (Owner.Transform.WorldRotation.GetCardinalDir())
            {
                case Direction.North:
                    return (cornerSW, cornerSE, cornerNE, cornerNW);
                case Direction.West:
                    return (cornerSE, cornerNE, cornerNW, cornerSW);
                case Direction.South:
                    return (cornerNE, cornerNW, cornerSW, cornerSE);
                default:
                    return (cornerNW, cornerSW, cornerSE, cornerNE);
            }
        }

        /// <inheritdoc />
        protected override void Shutdown()
        {
            base.Shutdown();

            SnapGrid.OnPositionChanged -= SnapGridOnPositionChanged;
            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new IconSmoothDirtyEvent(Owner, _lastPosition, SnapGrid.Offset, Mode));
        }

        private void SnapGridOnPositionChanged()
        {
            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new IconSmoothDirtyEvent(Owner, _lastPosition, SnapGrid.Offset, Mode));
            _lastPosition = (Owner.Transform.GridID, SnapGrid.Position);
        }

        [System.Diagnostics.Contracts.Pure]
        protected bool MatchingEntity(IEnumerable<IEntity> candidates)
        {
            foreach (var entity in candidates)
            {
                if (!entity.TryGetComponent(out IconSmoothComponent other))
                {
                    continue;
                }

                if (other.SmoothKey == SmoothKey)
                {
                    return true;
                }
            }

            return false;
        }

        [Flags]
        private enum CardinalConnectDirs : byte
        {
            None = 0,
            North = 1,
            South = 2,
            East = 4,
            West = 8
        }

        [Flags]
        public enum CornerFill : byte
        {
            // These values are pulled from Baystation12.
            // I'm too lazy to convert the state names.
            None = 0,

            // The cardinal tile counter-clockwise of this corner is filled.
            CounterClockwise = 1,

            // The diagonal tile in the direction of this corner.
            Diagonal = 2,

            // The cardinal tile clockwise of this corner is filled.
            Clockwise = 4,
        }

        public enum CornerLayers : byte
        {
            SE,
            NE,
            NW,
            SW,
        }
    }

    /// <summary>
    ///     Controls the mode with which icon smoothing is calculated.
    /// </summary>
    [PublicAPI]
    public enum IconSmoothingMode : byte
    {
        /// <summary>
        ///     Each icon is made up of 4 corners, each of which can get a different state depending on
        ///     adjacent entities clockwise, counter-clockwise and diagonal with the corner.
        /// </summary>
        Corners,

        /// <summary>
        ///     There are 16 icons, only one of which is used at once.
        ///     The icon selected is a bit field made up of the cardinal direction flags that have adjacent entities.
        /// </summary>
        CardinalFlags,

        /// <summary>
        ///     Where this component contributes to our neighbors being calculated but we do not update our own sprite.
        /// </summary>
        NoSprite,
    }
}
