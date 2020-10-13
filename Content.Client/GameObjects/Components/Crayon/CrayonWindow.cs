﻿using Content.Client.UserInterface.Stylesheets;
using Content.Shared.GameObjects.Components;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Drawing;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.Utility;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using System.Collections.Generic;

namespace Content.Client.GameObjects.Components.Crayon
{
    public class CrayonWindow : SS14Window
    {
        public CrayonBoundUserInterface Owner { get; }
        private readonly LineEdit _search;
        private readonly GridContainer _grid;
        private Dictionary<string, Texture> _decals;
        private string _selected;
        private Color _color;

        protected override Vector2? CustomSize => (250, 300);

        public CrayonWindow(CrayonBoundUserInterface owner)
        {
            Title = Loc.GetString("Crayon");
            Owner = owner;

            var vbox = new VBoxContainer();
            Contents.AddChild(vbox);

            _search = new LineEdit();
            _search.OnTextChanged += (e) => RefreshList();
            vbox.AddChild(_search);

            _grid = new GridContainer()
            {
                Columns = 6,
            };
            var gridScroll = new ScrollContainer()
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                Children =
                {
                    _grid
                }
            };
            vbox.AddChild(gridScroll);
        }

        private void RefreshList()
        {
            // Clear
            _grid.RemoveAllChildren();
            if (_decals == null)
                return;

            var filter = _search.Text;
            foreach (var (decal, tex) in _decals)
            {
                if (!decal.Contains(filter))
                    continue;

                var button = new TextureButton()
                {
                    TextureNormal = tex,
                    Name = decal,
                    ToolTip = decal,
                    Modulate = _color
                };
                button.OnPressed += Button_OnPressed;
                if (_selected == decal)
                {
                    var panelContainer = new PanelContainer()
                    {
                        PanelOverride = new StyleBoxFlat()
                        {
                            BackgroundColor = StyleNano.ButtonColorDefault,
                        },
                        Children =
                        {
                            button
                        }
                    };
                    _grid.AddChild(panelContainer);
                }
                else
                {
                    _grid.AddChild(button);
                }
            }
        }

        private void Button_OnPressed(BaseButton.ButtonEventArgs obj)
        {
            Owner.Select(obj.Button.Name);
            _selected = obj.Button.Name;
            RefreshList();
        }

        public void UpdateState(CrayonBoundUserInterfaceState state)
        {
            _selected = state.Selected;
            _color = state.Color;
            RefreshList();
        }

        public void Populate(CrayonDecalPrototype proto)
        {
            var path = new ResourcePath(proto.SpritePath);
            _decals = new Dictionary<string, Texture>();
            foreach (var state in proto.Decals)
            {
                var rsi = new SpriteSpecifier.Rsi(path, state);
                _decals.Add(state, rsi.Frame0());
            }
            
            RefreshList();
        }
    }
}
