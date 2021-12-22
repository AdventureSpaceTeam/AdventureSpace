using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.Message
{
    public static class RichTextLabelExt
    {
        public static void SetMarkup(this RichTextLabel label, string markup)
        {
            label.SetMessage(FormattedMessage.FromMarkup(markup));
        }
    }
}
