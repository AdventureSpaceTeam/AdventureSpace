using Content.Shared.Paper;
using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Utility;

namespace Content.Client.Paper.UI
{
    [GenerateTypedNameReferences]
    public sealed partial class PaperWindow : BaseWindow
    {
        // <summary>
        // Size of resize handles around the paper
        private const int DRAG_MARGIN_SIZE = 16;

        // We keep a reference to the paper content texture that we create
        // so that we can modify it later.
        private StyleBoxTexture _paperContentTex = new();

        // The number of lines that the content image represents.
        // See PaperVisualsComponent.ContentImageNumLines.
        private float _paperContentLineScale = 1.0f;

        // If paper limits the size in one or both axes, it'll affect whether
        // we're able to resize this UI or not. Default to everything enabled:
        private DragMode _allowedResizeModes  = ~DragMode.None;

        public PaperWindow()
        {
            RobustXamlLoader.Load(this);

            // We can't configure the RichTextLabel contents from xaml, so do it here:
            BlankPaperIndicator.SetMessage(Loc.GetString("paper-ui-blank-page-message"));

            // Hook up the close button:
            CloseButton.OnPressed += _ => Close();
        }

        /// <summary>
        ///     Initialize this UI according to <code>visuals</code> Initializes
        ///     textures, recalculates sizes, and applies some layout rules.
        /// </summary>
        public void InitVisuals(PaperVisualsComponent visuals)
        {
            var resCache = IoCManager.Resolve<IResourceCache>();

            /// Initialize the background:
            PaperBackground.ModulateSelfOverride = visuals.BackgroundModulate;
            var backgroundImage = visuals.BackgroundImagePath != null? resCache.GetResource<TextureResource>(visuals.BackgroundImagePath) : null;
            if (backgroundImage != null)
            {
                var backgroundImageMode = visuals.BackgroundImageTile ? StyleBoxTexture.StretchMode.Tile : StyleBoxTexture.StretchMode.Stretch;
                var backgroundPatchMargin = visuals.BackgroundPatchMargin;
                PaperBackground.PanelOverride = new StyleBoxTexture
                {
                    Texture = backgroundImage,
                    TextureScale = visuals.BackgroundScale,
                    Mode = backgroundImageMode,
                    PatchMarginLeft = backgroundPatchMargin.Left,
                    PatchMarginBottom = backgroundPatchMargin.Bottom,
                    PatchMarginRight = backgroundPatchMargin.Right,
                    PatchMarginTop = backgroundPatchMargin.Top
                };

            }
            else
            {
                PaperBackground.PanelOverride = null;
            }


            // Then the header:
            if (visuals.HeaderImagePath != null)
            {
                HeaderImage.TexturePath = visuals.HeaderImagePath;
                HeaderImage.MinSize = HeaderImage.TextureNormal?.Size ?? Vector2.Zero;
            }

            HeaderImage.ModulateSelfOverride = visuals.HeaderImageModulate;
            HeaderImage.Margin = new Thickness(visuals.HeaderMargin.Left, visuals.HeaderMargin.Top,
                    visuals.HeaderMargin.Right, visuals.HeaderMargin.Bottom);


            PaperContent.ModulateSelfOverride = visuals.ContentImageModulate;
            WrittenTextLabel.ModulateSelfOverride = visuals.FontAccentColor;

            var contentImage = visuals.ContentImagePath != null ? resCache.GetResource<TextureResource>(visuals.ContentImagePath) : null;
            if (contentImage != null)
            {
                // Setup the paper content texture, but keep a reference to it, as we can't set
                // some font-related properties here. We'll fix those up later, in Draw()
                _paperContentTex = new StyleBoxTexture
                {
                    Texture = contentImage,
                    Mode = StyleBoxTexture.StretchMode.Tile,
                };
                PaperContent.PanelOverride = _paperContentTex;
                _paperContentLineScale = visuals.ContentImageNumLines;
            }

            PaperContent.Margin = new Thickness(
                    visuals.ContentMargin.Left, visuals.ContentMargin.Top,
                    visuals.ContentMargin.Right, visuals.ContentMargin.Bottom);

            if (visuals.MaxWritableArea != null)
            {
                var a = (Vector2)visuals.MaxWritableArea;
                // Paper has requested that this has a maximum area that you can write on.
                // So, we'll make the window non-resizable and fix the size of the content.
                // Ideally, would like to be able to allow resizing only one direction.
                ScrollingContents.MinSize = Vector2.Zero;
                ScrollingContents.MinSize = (Vector2)(a);

                if (a.X > 0.0f)
                {
                    ScrollingContents.MaxWidth = a.X;
                    _allowedResizeModes &= ~(DragMode.Left | DragMode.Right);

                    // Since this dimension has been specified by the user, we
                    // need to undo the SetSize which was configured in the xaml.
                    // Controls use NaNs to indicate unset for this value.
                    // This is leaky - there should be a method for this
                    SetWidth = float.NaN;
                }

                if (a.Y > 0.0f)
                {
                    ScrollingContents.MaxHeight = a.Y;
                    _allowedResizeModes &= ~(DragMode.Top | DragMode.Bottom);
                    SetHeight = float.NaN;
                }
            }
        }

        /// <summary>
        ///     Control interface. We'll mostly rely on the children to do the drawing
        ///     but in order to get lines on paper to match up with the rich text labels,
        ///     we need to do a small calculation to sync them up.
        /// </summary>
        protected override void Draw(DrawingHandleScreen handle)
        {
            // Now do the deferred setup of the written area. At the point
            // that InitVisuals runs, the label hasn't had it's style initialized
            // so we need to get some info out now:
            if (WrittenTextLabel.TryGetStyleProperty<Font>("font", out var font))
            {
                float fontLineHeight = font.GetLineHeight(UIScale);
                // This positions the texture so the font baseline is on the bottom:
                _paperContentTex.ExpandMarginTop = font.GetDescent(UIScale);
                // And this scales the texture so that it's a single text line:
                var scaleY = (_paperContentLineScale * fontLineHeight) / _paperContentTex.Texture?.Height ?? fontLineHeight;
                _paperContentTex.TextureScale = new Vector2(1, scaleY);

                // Now, we might need to add some padding to the text to ensure
                // that, even if a header is specified, the text will line up with
                // where the content image expects the font to be rendered (i.e.,
                // adjusting the height of the header image shouldn't cause the
                // text to be offset from a line)
                {
                    var headerHeight = HeaderImage.Size.Y + HeaderImage.Margin.Top + HeaderImage.Margin.Bottom;
                    var headerInLines = headerHeight / (fontLineHeight * _paperContentLineScale);
                    var paddingRequiredInLines = (float)Math.Ceiling(headerInLines) - headerInLines;
                    var verticalMargin = fontLineHeight * paddingRequiredInLines * _paperContentLineScale;
                    TextAlignmentPadding.Margin = new Thickness(0.0f, verticalMargin, 0.0f, 0.0f);
                }
            }

            base.Draw(handle);
        }

        /// <summary>
        ///     Initialize the paper contents, i.e. the text typed by the
        ///     user and any stamps that have peen put on the page.
        /// </summary>
        public void Populate(SharedPaperComponent.PaperBoundUserInterfaceState state)
        {
            bool isEditing = state.Mode == SharedPaperComponent.PaperAction.Write;
            InputContainer.Visible = isEditing;

            var msg = new FormattedMessage();
            // Remove any newlines from the end of the message. There can be a trailing
            // new line at the end of user input, and we would like to display the input
            // box immediately on the next line.
            msg.AddMarkupPermissive(state.Text.TrimEnd('\r', '\n'));
            WrittenTextLabel.SetMessage(msg);
            WrittenTextLabel.Visible = state.Text.Length > 0;

            BlankPaperIndicator.Visible = !isEditing && state.Text.Length == 0;

            StampDisplay.RemoveAllChildren();
            foreach(var stamper in state.StampedBy)
            {
                StampDisplay.AddChild(new StampWidget{ Stamper = stamper });
            }
        }

        /// <summary>
        ///     BaseWindow interface. Allow users to drag UI around by grabbing
        ///     anywhere on the page (like FancyWindow) but try to calculate
        ///     reasonable dragging bounds because this UI can have round corners,
        ///     and it can be hard to judge where to click to resize.
        /// </summary>
        protected override DragMode GetDragModeFor(Vector2 relativeMousePos)
        {
            var mode = DragMode.Move;

            // Be quite generous with resize margins:
            if (relativeMousePos.Y < DRAG_MARGIN_SIZE)
            {
                mode |= DragMode.Top;
            }
            else if (relativeMousePos.Y > Size.Y - DRAG_MARGIN_SIZE)
            {
                mode |= DragMode.Bottom;
            }

            if (relativeMousePos.X < DRAG_MARGIN_SIZE)
            {
                mode |= DragMode.Left;
            }
            else if (relativeMousePos.X > Size.X - DRAG_MARGIN_SIZE)
            {
                mode |= DragMode.Right;
            }

            return mode & _allowedResizeModes;
        }
    }
}
