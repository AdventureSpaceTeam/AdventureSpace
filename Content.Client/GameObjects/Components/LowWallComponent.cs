﻿using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using Content.Client.GameObjects.Components.IconSmoothing;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Content.Client.GameObjects.Components
{
    // TODO: Over layers should be placed ABOVE the window itself too.
    // This is gonna require a client entity & parenting,
    // so IsMapTransform being naive is gonna be a problem.

    /// <summary>
    ///     Override of icon smoothing to handle the specific complexities of low walls.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IconSmoothComponent))]
    public class LowWallComponent : IconSmoothComponent
    {
        public override string Name => "LowWall";

        public CornerFill LastCornerNE { get; private set; }
        public CornerFill LastCornerSE { get; private set; }
        public CornerFill LastCornerSW { get; private set; }
        public CornerFill LastCornerNW { get; private set; }

        [ViewVariables]
        private IEntity _overlayEntity;
        private ISpriteComponent _overlaySprite;

        protected override void Startup()
        {
            base.Startup();

            _overlayEntity = Owner.EntityManager.SpawnEntity("LowWallOverlay", Owner.Transform.Coordinates);
            _overlayEntity.Transform.AttachParent(Owner);
            _overlayEntity.Transform.LocalPosition = Vector2.Zero;

            _overlaySprite = _overlayEntity.GetComponent<ISpriteComponent>();

            var overState0 = $"{StateBase}over_0";
            _overlaySprite.LayerMapSet(OverCornerLayers.SE, _overlaySprite.AddLayerState(overState0));
            _overlaySprite.LayerSetDirOffset(OverCornerLayers.SE, DirectionOffset.None);
            _overlaySprite.LayerMapSet(OverCornerLayers.NE, _overlaySprite.AddLayerState(overState0));
            _overlaySprite.LayerSetDirOffset(OverCornerLayers.NE, DirectionOffset.CounterClockwise);
            _overlaySprite.LayerMapSet(OverCornerLayers.NW, _overlaySprite.AddLayerState(overState0));
            _overlaySprite.LayerSetDirOffset(OverCornerLayers.NW, DirectionOffset.Flip);
            _overlaySprite.LayerMapSet(OverCornerLayers.SW, _overlaySprite.AddLayerState(overState0));
            _overlaySprite.LayerSetDirOffset(OverCornerLayers.SW, DirectionOffset.Clockwise);
        }

        protected override void Shutdown()
        {
            base.Shutdown();

            _overlayEntity.Delete();
        }

        internal override void CalculateNewSprite()
        {
            base.CalculateNewSprite();

            var (n, nl) = MatchingWall(SnapGrid.GetInDir(Direction.North));
            var (ne, nel) = MatchingWall(SnapGrid.GetInDir(Direction.NorthEast));
            var (e, el) = MatchingWall(SnapGrid.GetInDir(Direction.East));
            var (se, sel) = MatchingWall(SnapGrid.GetInDir(Direction.SouthEast));
            var (s, sl) = MatchingWall(SnapGrid.GetInDir(Direction.South));
            var (sw, swl) = MatchingWall(SnapGrid.GetInDir(Direction.SouthWest));
            var (w, wl) = MatchingWall(SnapGrid.GetInDir(Direction.West));
            var (nw, nwl) = MatchingWall(SnapGrid.GetInDir(Direction.NorthWest));

            // ReSharper disable InconsistentNaming
            var cornerNE = CornerFill.None;
            var cornerSE = CornerFill.None;
            var cornerSW = CornerFill.None;
            var cornerNW = CornerFill.None;

            var lowCornerNE = CornerFill.None;
            var lowCornerSE = CornerFill.None;
            var lowCornerSW = CornerFill.None;
            var lowCornerNW = CornerFill.None;
            // ReSharper restore InconsistentNaming

            if (n)
            {
                cornerNE |= CornerFill.CounterClockwise;
                cornerNW |= CornerFill.Clockwise;

                if (!nl)
                {
                    lowCornerNE |= CornerFill.CounterClockwise;
                    lowCornerNW |= CornerFill.Clockwise;
                }
            }

            if (ne)
            {
                cornerNE |= CornerFill.Diagonal;

                if (!nel && (nl || el || n && e))
                {
                    lowCornerNE |= CornerFill.Diagonal;
                }
            }

            if (e)
            {
                cornerNE |= CornerFill.Clockwise;
                cornerSE |= CornerFill.CounterClockwise;

                if (!el)
                {
                    lowCornerNE |= CornerFill.Clockwise;
                    lowCornerSE |= CornerFill.CounterClockwise;
                }
            }

            if (se)
            {
                cornerSE |= CornerFill.Diagonal;

                if (!sel && (sl || el || s && e))
                {
                    lowCornerSE |= CornerFill.Diagonal;
                }
            }

            if (s)
            {
                cornerSE |= CornerFill.Clockwise;
                cornerSW |= CornerFill.CounterClockwise;

                if (!sl)
                {
                    lowCornerSE |= CornerFill.Clockwise;
                    lowCornerSW |= CornerFill.CounterClockwise;
                }
            }

            if (sw)
            {
                cornerSW |= CornerFill.Diagonal;

                if (!swl && (sl || wl || s && w))
                {
                    lowCornerSW |= CornerFill.Diagonal;
                }
            }

            if (w)
            {
                cornerSW |= CornerFill.Clockwise;
                cornerNW |= CornerFill.CounterClockwise;

                if (!wl)
                {
                    lowCornerSW |= CornerFill.Clockwise;
                    lowCornerNW |= CornerFill.CounterClockwise;
                }
            }

            if (nw)
            {
                cornerNW |= CornerFill.Diagonal;

                if (!nwl && (nl || wl || n && w))
                {
                    lowCornerNW |= CornerFill.Diagonal;
                }
            }

            Sprite.LayerSetState(CornerLayers.NE, $"{StateBase}{(int) cornerNE}");
            Sprite.LayerSetState(CornerLayers.SE, $"{StateBase}{(int) cornerSE}");
            Sprite.LayerSetState(CornerLayers.SW, $"{StateBase}{(int) cornerSW}");
            Sprite.LayerSetState(CornerLayers.NW, $"{StateBase}{(int) cornerNW}");

            _overlaySprite.LayerSetState(OverCornerLayers.NE, $"{StateBase}over_{(int) lowCornerNE}");
            _overlaySprite.LayerSetState(OverCornerLayers.SE, $"{StateBase}over_{(int) lowCornerSE}");
            _overlaySprite.LayerSetState(OverCornerLayers.SW, $"{StateBase}over_{(int) lowCornerSW}");
            _overlaySprite.LayerSetState(OverCornerLayers.NW, $"{StateBase}over_{(int) lowCornerNW}");

            LastCornerNE = cornerNE;
            LastCornerSE = cornerSE;
            LastCornerSW = cornerSW;
            LastCornerNW = cornerNW;

            foreach (var entity in SnapGrid.GetLocal())
            {
                if (entity.TryGetComponent(out WindowComponent window))
                {
                    window.UpdateSprite();
                }
            }
        }

        [Pure]
        private (bool connected, bool lowWall) MatchingWall(IEnumerable<IEntity> candidates)
        {
            foreach (var entity in candidates)
            {
                if (!entity.TryGetComponent(out IconSmoothComponent other))
                {
                    continue;
                }

                if (other.SmoothKey == SmoothKey)
                {
                    return (true, other is LowWallComponent);
                }
            }

            return (false, false);
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum OverCornerLayers : byte
        {
            SE,
            NE,
            NW,
            SW,
        }
    }
}
