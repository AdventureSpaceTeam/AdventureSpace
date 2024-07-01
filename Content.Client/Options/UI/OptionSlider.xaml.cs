using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface;

namespace Content.Client.Options.UI;

/// <summary>
/// Standard UI control used for sliders in the options menu. Intended for use with <see cref="OptionsTabControlRow"/>.
/// </summary>
/// <seealso cref="OptionsTabControlRow.AddOptionSlider"/>
/// <seealso cref="OptionsTabControlRow.AddOptionPercentSlider"/>
[GenerateTypedNameReferences]
public sealed partial class OptionSlider : Control
{
    /// <summary>
    /// The text describing what this slider controls.
    /// </summary>
    public string? Title
    {
        get => NameLabel.Text;
        set => NameLabel.Text = value;
    }
}
