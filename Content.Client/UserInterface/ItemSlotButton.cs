﻿using System;
using Content.Client.UserInterface.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Shaders;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.UserInterface
{
    public class ItemSlotButton : MarginContainer
    {
        private const string HighlightShader = "SelectionOutlineInrange";

        public TextureRect Button { get; }
        public SpriteView SpriteView { get; }
        public SpriteView HoverSpriteView { get; }
        public BaseButton StorageButton { get; }
        public CooldownGraphic CooldownDisplay { get; }

        public Action<GUIBoundKeyEventArgs> OnPressed { get; set; }
        public Action<GUIBoundKeyEventArgs> OnStoragePressed { get; set; }
        public Action<GUIMouseHoverEventArgs> OnHover { get; set; }

        public bool EntityHover => HoverSpriteView.Sprite != null;
        public bool MouseIsHovering = false;

        private readonly PanelContainer _highlightRect;

        public ItemSlotButton(Texture texture, Texture storageTexture)
        {
            CustomMinimumSize = (64, 64);

            AddChild(Button = new TextureRect
            {
                Texture = texture,
                TextureScale = (2, 2),
                MouseFilter = MouseFilterMode.Stop
            });

            AddChild(_highlightRect = new PanelContainer
            {
                StyleClasses = { StyleNano.StyleClassHandSlotHighlight },
                CustomMinimumSize = (32, 32),
                Visible = false
            });

            Button.OnKeyBindDown += OnButtonPressed;

            AddChild(SpriteView = new SpriteView
            {
                Scale = (2, 2),
                OverrideDirection = Direction.South
            });

            AddChild(HoverSpriteView = new SpriteView
            {
                Scale = (2, 2),
                OverrideDirection = Direction.South
            });

            AddChild(StorageButton = new TextureButton
            {
                TextureNormal = storageTexture,
                Scale = (0.75f, 0.75f),
                SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
                SizeFlagsVertical = SizeFlags.ShrinkEnd,
                Visible = false,
            });

            StorageButton.OnKeyBindDown += args =>
            {
                if (args.Function != EngineKeyFunctions.UIClick)
                {
                    OnButtonPressed(args);
                }
            };

            StorageButton.OnPressed += OnStorageButtonPressed;

            Button.OnMouseEntered += _ =>
            {
                MouseIsHovering = true;
            };
            Button.OnMouseEntered += OnButtonHover;

            Button.OnMouseExited += _ =>
            {
                MouseIsHovering = false;
                ClearHover();
            };

            AddChild(CooldownDisplay = new CooldownGraphic
            {
                SizeFlagsHorizontal = SizeFlags.Fill,
                SizeFlagsVertical = SizeFlags.Fill,
                Visible = false,
            });
        }

        public void ClearHover()
        {
            if (EntityHover)
            {
                HoverSpriteView.Sprite?.Owner.Delete();
                HoverSpriteView.Sprite = null;
            }
        }

        public virtual void Highlight(bool highlight)
        {
            if (highlight)
            {
                _highlightRect.Visible = true;
            }
            else
            {
                _highlightRect.Visible = false;
            }
        }

        private void OnButtonPressed(GUIBoundKeyEventArgs args)
        {
            OnPressed?.Invoke(args);
        }

        private void OnStorageButtonPressed(BaseButton.ButtonEventArgs args)
        {
            if (args.Event.Function == EngineKeyFunctions.UIClick)
            {
                OnStoragePressed?.Invoke(args.Event);
            }
            else
            {
                OnPressed?.Invoke(args.Event);
            }
        }

        private void OnButtonHover(GUIMouseHoverEventArgs args)
        {
            OnHover?.Invoke(args);
        }
    }
}
