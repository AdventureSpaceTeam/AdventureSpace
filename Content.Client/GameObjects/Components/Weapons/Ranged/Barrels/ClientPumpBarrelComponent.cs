﻿using System;
using Content.Client.UserInterface.Stylesheets;
using Content.Client.Utility;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Weapons.Ranged.Barrels;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components.Weapons.Ranged.Barrels
{
    [RegisterComponent]
    public class ClientPumpBarrelComponent : Component, IItemStatus
    {
        public override string Name => "PumpBarrel";
        public override uint? NetID => ContentNetIDs.PUMP_BARREL;

        private StatusControl _statusControl;

        /// <summary>
        ///     chambered is true when a bullet is chambered
        ///     spent is true when the chambered bullet is spent
        /// </summary>
        [ViewVariables]
        public (bool chambered, bool spent) Chamber { get; private set; }

        /// <summary>
        ///     Count of bullets in the magazine.
        /// </summary>
        /// <remarks>
        ///     Null if no magazine is inserted.
        /// </remarks>
        [ViewVariables]
        public (int count, int max)? MagazineCount { get; private set; }

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            if (!(curState is PumpBarrelComponentState cast))
                return;

            Chamber = cast.Chamber;
            MagazineCount = cast.Magazine;
            _statusControl?.Update();
        }

        public Control MakeControl()
        {
            _statusControl = new StatusControl(this);
            _statusControl.Update();
            return _statusControl;
        }

        public void DestroyControl(Control control)
        {
            if (_statusControl == control)
            {
                _statusControl = null;
            }
        }

        private sealed class StatusControl : Control
        {
            private readonly ClientPumpBarrelComponent _parent;
            private readonly HBoxContainer _bulletsListTop;
            private readonly HBoxContainer _bulletsListBottom;
            private readonly TextureRect _chamberedBullet;
            private readonly Label _noMagazineLabel;

            public StatusControl(ClientPumpBarrelComponent parent)
            {
                _parent = parent;
                SizeFlagsHorizontal = SizeFlags.FillExpand;
                SizeFlagsVertical = SizeFlags.ShrinkCenter;
                AddChild(new VBoxContainer
                {
                    SizeFlagsHorizontal = SizeFlags.FillExpand,
                    SizeFlagsVertical = SizeFlags.ShrinkCenter,
                    SeparationOverride = 0,
                    Children =
                    {
                        (_bulletsListTop = new HBoxContainer {SeparationOverride = 0}),
                        new HBoxContainer
                        {
                            SizeFlagsHorizontal = SizeFlags.FillExpand,
                            Children =
                            {
                                new Control
                                {
                                    SizeFlagsHorizontal = SizeFlags.FillExpand,
                                    Children =
                                    {
                                        (_bulletsListBottom = new HBoxContainer
                                        {
                                            SizeFlagsVertical = SizeFlags.ShrinkCenter,
                                            SeparationOverride = 0
                                        }),
                                        (_noMagazineLabel = new Label
                                        {
                                            Text = "No Magazine!",
                                            StyleClasses = {StyleNano.StyleClassItemStatus}
                                        })
                                    }
                                },
                                (_chamberedBullet = new TextureRect
                                {
                                    Texture = StaticIoC.ResC.GetTexture("/Textures/Interface/ItemStatus/Bullets/chambered.png"),
                                    SizeFlagsVertical = SizeFlags.ShrinkCenter,
                                    SizeFlagsHorizontal = SizeFlags.ShrinkEnd | SizeFlags.Fill,
                                })
                            }
                        }
                    }
                });
            }

            public void Update()
            {
                _chamberedBullet.ModulateSelfOverride =
                    _parent.Chamber.chambered ?
                    _parent.Chamber.spent ? Color.Red : Color.FromHex("#d7df60")
                    : Color.Black;

                _bulletsListTop.RemoveAllChildren();
                _bulletsListBottom.RemoveAllChildren();

                if (_parent.MagazineCount == null)
                {
                    _noMagazineLabel.Visible = true;
                    return;
                }

                var (count, capacity) = _parent.MagazineCount.Value;

                _noMagazineLabel.Visible = false;

                string texturePath;
                if (capacity <= 20)
                {
                    texturePath = "/Textures/Interface/ItemStatus/Bullets/normal.png";
                }
                else if (capacity <= 30)
                {
                    texturePath = "/Textures/Interface/ItemStatus/Bullets/small.png";
                }
                else
                {
                    texturePath = "/Textures/Interface/ItemStatus/Bullets/tiny.png";
                }

                var texture = StaticIoC.ResC.GetTexture(texturePath);

                const int tinyMaxRow = 60;

                if (capacity > tinyMaxRow)
                {
                    FillBulletRow(_bulletsListBottom, Math.Min(tinyMaxRow, count), tinyMaxRow, texture);
                    FillBulletRow(_bulletsListTop, Math.Max(0, count - tinyMaxRow), capacity - tinyMaxRow, texture);
                }
                else
                {
                    FillBulletRow(_bulletsListBottom, count, capacity, texture);
                }
            }

            private static void FillBulletRow(Control container, int count, int capacity, Texture texture)
            {
                var colorA = Color.FromHex("#b68f0e");
                var colorB = Color.FromHex("#d7df60");
                var colorGoneA = Color.FromHex("#000000");
                var colorGoneB = Color.FromHex("#222222");

                var altColor = false;

                for (var i = count; i < capacity; i++)
                {
                    container.AddChild(new TextureRect
                    {
                        Texture = texture,
                        ModulateSelfOverride = altColor ? colorGoneA : colorGoneB
                    });

                    altColor ^= true;
                }

                for (var i = 0; i < count; i++)
                {
                    container.AddChild(new TextureRect
                    {
                        Texture = texture,
                        ModulateSelfOverride = altColor ? colorA : colorB
                    });

                    altColor ^= true;
                }
            }

            protected override Vector2 CalculateMinimumSize()
            {
                return Vector2.ComponentMax((0, 15), base.CalculateMinimumSize());
            }
        }
    }
}
