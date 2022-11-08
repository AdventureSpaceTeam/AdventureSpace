﻿using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.CartridgeLoader.Cartridges;

[GenerateTypedNameReferences]
public sealed partial class NotekeeperUiFragment : BoxContainer
{

    public event Action<string>? OnNoteAdded;
    public event Action<string>? OnNoteRemoved;

    public NotekeeperUiFragment()
    {
        RobustXamlLoader.Load(this);
        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;
        VerticalExpand = true;

        Input.OnTextEntered += _ =>
        {
            AddNote(Input.Text);
            OnNoteAdded?.Invoke(Input.Text);
            Input.Clear();
        };

        UpdateState(new List<string>());
    }

    public void UpdateState(List<string> notes)
    {
        MessageContainer.RemoveAllChildren();

        foreach (var note in notes)
        {
           AddNote(note);
        }
    }

    private void AddNote(string note)
    {
        var row = new BoxContainer();
        row.HorizontalExpand = true;
        row.Orientation = LayoutOrientation.Horizontal;
        row.Margin = new Thickness(4);

        var label = new Label();
        label.Text = note;
        label.HorizontalExpand = true;
        label.ClipText = true;

        var removeButton = new TextureButton();
        removeButton.AddStyleClass("windowCloseButton");
        removeButton.OnPressed += _ => OnNoteRemoved?.Invoke(label.Text);

        row.AddChild(label);
        row.AddChild(removeButton);

        MessageContainer.AddChild(row);
    }
}
