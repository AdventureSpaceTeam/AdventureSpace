﻿using System.Linq;
using System.Runtime.InteropServices;
using Content.Client.Administration.UI.CustomControls;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using static Robust.Client.UserInterface.Controls.BaseButton;
using static Robust.Client.UserInterface.Controls.LineEdit;

namespace Content.Client.Administration.UI.Logs;

[GenerateTypedNameReferences]
public sealed partial class AdminLogsControl : Control
{
    private readonly Comparer<AdminLogTypeButton> _adminLogTypeButtonComparer =
        Comparer<AdminLogTypeButton>.Create((a, b) =>
            string.Compare(a.Type.ToString(), b.Type.ToString(), StringComparison.Ordinal));

    private readonly Comparer<AdminLogPlayerButton> _adminLogPlayerButtonComparer =
        Comparer<AdminLogPlayerButton>.Create((a, b) =>
            string.Compare(a.Text, b.Text, StringComparison.Ordinal));

    public AdminLogsControl()
    {
        RobustXamlLoader.Load(this);

        TypeSearch.OnTextChanged += TypeSearchChanged;
        PlayerSearch.OnTextChanged += PlayerSearchChanged;
        LogSearch.OnTextChanged += LogSearchChanged;

        SelectAllTypesButton.OnPressed += SelectAllTypes;
        SelectNoTypesButton.OnPressed += SelectNoTypes;

        IncludeNonPlayersButton.OnPressed += IncludeNonPlayers;
        SelectAllPlayersButton.OnPressed += SelectAllPlayers;
        SelectNoPlayersButton.OnPressed += SelectNoPlayers;

        RoundSpinBox.IsValid = i => i > 0 && i <= CurrentRound;
        RoundSpinBox.ValueChanged += RoundSpinBoxChanged;
        RoundSpinBox.InitDefaultButtons();

        ResetRoundButton.OnPressed += ResetRoundPressed;

        SetImpacts(Enum.GetValues<LogImpact>().OrderBy(impact => impact).ToArray());
        SetTypes(Enum.GetValues<LogType>());
    }

    private int CurrentRound { get; set; }

    public int SelectedRoundId => RoundSpinBox.Value;
    public string Search => LogSearch.Text;
    private int ShownLogs { get; set; }
    private int TotalLogs { get; set; }
    private int RoundLogs { get; set; }
    public bool IncludeNonPlayerLogs { get; set; }

    public HashSet<LogType> SelectedTypes { get; } = new();

    public HashSet<Guid> SelectedPlayers { get; } = new();

    public HashSet<LogImpact> SelectedImpacts { get; } = new();

    public void SetCurrentRound(int round)
    {
        CurrentRound = round;
        ResetRoundButton.Text = Loc.GetString("admin-logs-reset-with-id", ("id", round));
        UpdateResetButton();
    }

    public void SetRoundSpinBox(int round)
    {
        RoundSpinBox.Value = round;
        UpdateResetButton();
    }

    private void RoundSpinBoxChanged(ValueChangedEventArgs args)
    {
        UpdateResetButton();
    }

    private void UpdateResetButton()
    {
        ResetRoundButton.Disabled = RoundSpinBox.Value == CurrentRound;
    }

    private void ResetRoundPressed(ButtonEventArgs args)
    {
        RoundSpinBox.Value = CurrentRound;
    }

    private void TypeSearchChanged(LineEditEventArgs args)
    {
        UpdateTypes();
    }

    private void PlayerSearchChanged(LineEditEventArgs args)
    {
        UpdatePlayers();
    }

    private void LogSearchChanged(LineEditEventArgs args)
    {
        UpdateLogs();
    }

    private void SelectAllTypes(ButtonEventArgs args)
    {
        SelectedTypes.Clear();

        foreach (var control in TypesContainer.Children)
        {
            if (control is not AdminLogTypeButton type)
            {
                continue;
            }

            type.Pressed = true;
            SelectedTypes.Add(type.Type);
        }

        UpdateLogs();
    }

    private void SelectNoTypes(ButtonEventArgs args)
    {
        SelectedTypes.Clear();

        foreach (var control in TypesContainer.Children)
        {
            if (control is not AdminLogTypeButton type)
            {
                continue;
            }

            type.Pressed = false;
            type.Visible = ShouldShowType(type);
        }

        UpdateLogs();
    }

    private void IncludeNonPlayers(ButtonEventArgs args)
    {
        IncludeNonPlayerLogs = args.Button.Pressed;

        UpdateLogs();
    }

    private void SelectAllPlayers(ButtonEventArgs args)
    {
        SelectedPlayers.Clear();

        foreach (var control in PlayersContainer.Children)
        {
            if (control is not AdminLogPlayerButton player)
            {
                continue;
            }

            player.Pressed = true;
            SelectedPlayers.Add(player.Id);
        }

        UpdateLogs();
    }

    private void SelectNoPlayers(ButtonEventArgs args)
    {
        SelectedPlayers.Clear();

        foreach (var control in PlayersContainer.Children)
        {
            if (control is not AdminLogPlayerButton player)
            {
                continue;
            }

            player.Pressed = false;
        }

        UpdateLogs();
    }

    public void SetTypesSelection(HashSet<LogType> selectedTypes, bool invert = false)
    {
        SelectedTypes.Clear();

        foreach (var control in TypesContainer.Children)
        {
            if (control is not AdminLogTypeButton type)
            {
                continue;
            }

            if (selectedTypes.Contains(type.Type) ^ invert)
            {
                type.Pressed = true;
                SelectedTypes.Add(type.Type);
            }
            else
            {
                type.Pressed = false;
                type.Visible = ShouldShowType(type);
            }
        }

        UpdateLogs();
    }

    public void UpdateTypes()
    {
        foreach (var control in TypesContainer.Children)
        {
            if (control is not AdminLogTypeButton type)
            {
                continue;
            }

            type.Visible = ShouldShowType(type);
        }
    }

    private void UpdatePlayers()
    {
        foreach (var control in PlayersContainer.Children)
        {
            if (control is not AdminLogPlayerButton player)
            {
                continue;
            }

            player.Visible = ShouldShowPlayer(player);
        }
    }

    private void UpdateLogs()
    {
        ShownLogs = 0;

        foreach (var child in LogsContainer.Children)
        {
            if (child is not AdminLogLabel log)
            {
                continue;
            }

            child.Visible = ShouldShowLog(log);
            if (child.Visible)
            {
                ShownLogs++;
            }
        }

        UpdateCount();
    }

    private bool ShouldShowType(AdminLogTypeButton button)
    {
        return button.Text != null &&
               button.Text.Contains(TypeSearch.Text, StringComparison.OrdinalIgnoreCase);
    }

    private bool ShouldShowPlayer(AdminLogPlayerButton button)
    {
        return button.Text != null &&
               button.Text.Contains(PlayerSearch.Text, StringComparison.OrdinalIgnoreCase);
    }

    private bool LogMatchesPlayerFilter(AdminLogLabel label)
    {
        if (label.Log.Players.Length == 0)
            return SelectedPlayers.Count == 0 || IncludeNonPlayerLogs;

        return SelectedPlayers.Overlaps(label.Log.Players);
    }

    private bool ShouldShowLog(AdminLogLabel label)
    {
        // Check log type
        if (!SelectedTypes.Contains(label.Log.Type))
            return false;

        // Check players
        if (!LogMatchesPlayerFilter(label))
            return false;

        // Check impact
        if (!SelectedImpacts.Contains(label.Log.Impact))
            return false;

        // Check search
        if (!label.Log.Message.Contains(LogSearch.Text, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    private void TypeButtonPressed(ButtonEventArgs args)
    {
        var button = (AdminLogTypeButton) args.Button;
        if (button.Pressed)
        {
            SelectedTypes.Add(button.Type);
        }
        else
        {
            SelectedTypes.Remove(button.Type);
        }

        UpdateLogs();
    }

    private void PlayerButtonPressed(ButtonEventArgs args)
    {
        var button = (AdminLogPlayerButton) args.Button;
        if (button.Pressed)
        {
            SelectedPlayers.Add(button.Id);
        }
        else
        {
            SelectedPlayers.Remove(button.Id);
        }

        UpdateLogs();
    }

    private void ImpactButtonPressed(ButtonEventArgs args)
    {
        var button = (AdminLogImpactButton) args.Button;
        if (button.Pressed)
        {
            SelectedImpacts.Add(button.Impact);
        }
        else
        {
            SelectedImpacts.Remove(button.Impact);
        }

        UpdateLogs();
    }

    private void SetImpacts(LogImpact[] impacts)
    {
        LogImpactContainer.RemoveAllChildren();

        foreach (var impact in impacts)
        {
            var button = new AdminLogImpactButton(impact)
            {
                Text = impact.ToString()
            };

            SelectedImpacts.Add(impact);
            button.OnPressed += ImpactButtonPressed;

            LogImpactContainer.AddChild(button);
        }

        switch (impacts.Length)
        {
            case 0:
                return;
            case 1:
                LogImpactContainer.GetChild(0).StyleClasses.Add("OpenRight");
                return;
        }

        for (var i = 0; i < impacts.Length - 1; i++)
        {
            LogImpactContainer.GetChild(i).StyleClasses.Add("ButtonSquare");
        }

        LogImpactContainer.GetChild(LogImpactContainer.ChildCount - 1).StyleClasses.Add("OpenLeft");
    }

    private void SetTypes(LogType[] types)
    {
        var newTypes = types.ToHashSet();
        var buttons = new SortedSet<AdminLogTypeButton>(_adminLogTypeButtonComparer);

        foreach (var control in TypesContainer.Children.ToArray())
        {
            if (control is not AdminLogTypeButton type ||
                !newTypes.Remove(type.Type))
            {
                continue;
            }

            buttons.Add(type);
        }

        foreach (var type in newTypes)
        {
            var button = new AdminLogTypeButton(type)
            {
                Text = type.ToString(),
                Pressed = true
            };

            SelectedTypes.Add(type);
            button.OnPressed += TypeButtonPressed;

            buttons.Add(button);
        }

        TypesContainer.RemoveAllChildren();

        foreach (var type in buttons)
        {
            TypesContainer.AddChild(type);
        }

        UpdateLogs();
    }

    public void SetPlayers(Dictionary<Guid, string> players)
    {
        var buttons = new SortedSet<AdminLogPlayerButton>(_adminLogPlayerButtonComparer);

        foreach (var control in PlayersContainer.Children.ToArray())
        {
            if (control is not AdminLogPlayerButton player ||
                !players.Remove(player.Id))
            {
                continue;
            }

            buttons.Add(player);
        }

        foreach (var (id, name) in players)
        {
            var button = new AdminLogPlayerButton(id)
            {
                Text = name,
                Pressed = true
            };

            SelectedPlayers.Add(id);
            button.OnPressed += PlayerButtonPressed;

            buttons.Add(button);
        }

        PlayersContainer.RemoveAllChildren();

        foreach (var player in buttons)
        {
            PlayersContainer.AddChild(player);
        }

        UpdateLogs();
    }

    public void AddLogs(List<SharedAdminLog> logs)
    {
        var span = CollectionsMarshal.AsSpan(logs);
        for (var i = 0; i < span.Length; i++)
        {
            ref var log = ref span[i];
            var separator = new HSeparator();
            var label = new AdminLogLabel(ref log, separator);
            label.Visible = ShouldShowLog(label);

            TotalLogs++;
            if (label.Visible)
            {
                ShownLogs++;
            }

            LogsContainer.AddChild(label);
            LogsContainer.AddChild(separator);
        }

        UpdateCount();
    }

    public void SetLogs(List<SharedAdminLog> logs)
    {
        LogsContainer.RemoveAllChildren();
        UpdateCount(0, 0);
        AddLogs(logs);
    }

    public void UpdateCount(int? shown = null, int? total = null, int? round = null)
    {
        if (shown != null)
        {
            ShownLogs = shown.Value;
        }

        if (total != null)
        {
            TotalLogs = total.Value;
        }

        if (round != null)
        {
            RoundLogs = round.Value;
        }

        Count.Text = Loc.GetString(
            "admin-logs-count",
            ("showing", ShownLogs), ("total", TotalLogs), ("round", RoundLogs)
        );
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        TypeSearch.OnTextChanged -= TypeSearchChanged;
        PlayerSearch.OnTextChanged -= PlayerSearchChanged;
        LogSearch.OnTextChanged -= LogSearchChanged;

        SelectAllTypesButton.OnPressed -= SelectAllTypes;
        SelectNoTypesButton.OnPressed -= SelectNoTypes;

        IncludeNonPlayersButton.OnPressed -= IncludeNonPlayers;
        SelectAllPlayersButton.OnPressed -= SelectAllPlayers;
        SelectNoPlayersButton.OnPressed -= SelectNoPlayers;

        RoundSpinBox.IsValid = null;
        RoundSpinBox.ValueChanged -= RoundSpinBoxChanged;

        ResetRoundButton.OnPressed -= ResetRoundPressed;
    }
}
