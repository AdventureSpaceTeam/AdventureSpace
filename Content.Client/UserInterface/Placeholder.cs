using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface
{
    public sealed class Placeholder : PanelContainer
    {
        public const string StyleClassPlaceholderText = "PlaceholderText";

        private readonly Label _label;

        public string PlaceholderText
        {
            get => _label.Text;
            set => _label.Text = value;
        }

        public Placeholder(IResourceCache _resourceCache)
        {
            _label = new Label
            {
                VerticalAlignment = VAlignment.Stretch,
                Align = Label.AlignMode.Center,
                VAlign = Label.VAlignMode.Center
            };
            _label.AddStyleClass(StyleClassPlaceholderText);
            AddChild(_label);
        }
    }
}
