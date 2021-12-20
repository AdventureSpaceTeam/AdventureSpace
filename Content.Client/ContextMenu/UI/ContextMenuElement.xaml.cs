using Content.Client.Resources;
using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client.ContextMenu.UI
{
    /// <summary>
    ///     This is a basic entry in a context menu. It has a label and room for some sort of icon on the left.
    ///     If this entry has a sub-menu, it also shows a little ">" icon on the right.
    /// </summary>
    [GenerateTypedNameReferences]
    public partial class ContextMenuElement : ContainerButton
    {
        public const string StyleClassContextMenuButton = "contextMenuButton";

        public const float ElementMargin = 2;
        public const float ElementHeight = 32;

        /// <summary>
        ///     The menu that contains this element
        /// </summary>
        public ContextMenuPopup? ParentMenu;

        private ContextMenuPopup? _subMenu;

        /// <summary>
        ///     The pop-up menu that is opened when hovering over this element.
        /// </summary>
        public ContextMenuPopup? SubMenu
        {
            get => _subMenu;
            set
            {
                _subMenu = value;
                ExpansionIndicator.Visible = _subMenu != null;
            }
        }

        /// <summary>
        ///     Convenience property to set label text.
        /// </summary>
        public string Text { set => Label.SetMessage(FormattedMessage.FromMarkupPermissive(value.Trim())); }

        public ContextMenuElement(string? text = null)
        {
            RobustXamlLoader.Load(this);
            Margin = new Thickness(ElementMargin, ElementMargin, ElementMargin, ElementMargin);
            SetOnlyStyleClass(StyleClassContextMenuButton);

            if (text != null)
                Text = text;

            ExpansionIndicator.Texture = IoCManager.Resolve<IResourceCache>()
                .GetTexture("/Textures/Interface/VerbIcons/group.svg.192dpi.png");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _subMenu?.Dispose();
            _subMenu = null;
            ParentMenu = null;
        }

        protected override void Draw(DrawingHandleScreen handle)
        {
            UpdateStyle();
            base.Draw(handle);
        }

        /// <summary>
        ///     If this element's sub-menu is currently visible, give it the hovered pseudo class.
        /// </summary>
        /// <remarks>
        ///     Basically: if we are in a sub menu, keep the element in the parent menu highlighted even though we are
        ///     not actually hovering over it.
        /// </remarks>
        protected virtual void UpdateStyle()
        {
            if ((_subMenu?.Visible ?? false) && !HasStylePseudoClass(StylePseudoClassHover))
            {
                AddStylePseudoClass(StylePseudoClassHover);
                return;
            }

            if (DrawMode == DrawModeEnum.Hover)
                return;

            if (_subMenu?.Visible ?? true)
                return;

            RemoveStylePseudoClass(StylePseudoClassHover);
        }
    }
}
