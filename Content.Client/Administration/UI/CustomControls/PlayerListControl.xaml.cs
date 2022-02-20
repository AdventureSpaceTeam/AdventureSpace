﻿using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Administration;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Administration.UI.CustomControls
{
    [GenerateTypedNameReferences]
    public sealed partial class PlayerListControl : BoxContainer
    {
        private readonly AdminSystem _adminSystem;

        public event Action<PlayerInfo?>? OnSelectionChanged;

        public Action<PlayerInfo, ItemList.Item>? DecoratePlayer;
        public Comparison<PlayerInfo>? Comparison;

        public PlayerListControl()
        {
            _adminSystem = EntitySystem.Get<AdminSystem>();
            IoCManager.InjectDependencies(this);
            RobustXamlLoader.Load(this);
            // Fill the Option data
            PopulateList();
            PlayerItemList.OnItemSelected += PlayerItemListOnOnItemSelected;
            PlayerItemList.OnItemDeselected += PlayerItemListOnOnItemDeselected;
            FilterLineEdit.OnTextChanged += FilterLineEditOnOnTextEntered;
            _adminSystem.PlayerListChanged += PopulateList;

        }

        private void FilterLineEditOnOnTextEntered(LineEdit.LineEditEventArgs obj)
        {
            PopulateList();
        }

        private void PlayerItemListOnOnItemSelected(ItemList.ItemListSelectedEventArgs obj)
        {
            var selectedPlayer = (PlayerInfo) obj.ItemList[obj.ItemIndex].Metadata!;
            OnSelectionChanged?.Invoke(selectedPlayer);
        }

        private void PlayerItemListOnOnItemDeselected(ItemList.ItemListDeselectedEventArgs obj)
        {
            OnSelectionChanged?.Invoke(null);
        }

        public void RefreshDecorators()
        {
            foreach (var item in PlayerItemList)
            {
                DecoratePlayer?.Invoke((PlayerInfo) item.Metadata!, item);
            }
        }

        public void Sort()
        {
            if(Comparison != null)
                PlayerItemList.Sort((a, b) => Comparison((PlayerInfo) a.Metadata!, (PlayerInfo) b.Metadata!));
        }

        private void PopulateList(IReadOnlyList<PlayerInfo> _ = null!)
        {
            PlayerItemList.Clear();

            foreach (var info in _adminSystem.PlayerList)
            {
                var displayName = $"{info.CharacterName} ({info.Username})";
                if (!string.IsNullOrEmpty(FilterLineEdit.Text) &&
                    !displayName.ToLowerInvariant().Contains(FilterLineEdit.Text.Trim().ToLowerInvariant()))
                {
                    continue;
                }

                var item = new ItemList.Item(PlayerItemList)
                {
                    Metadata = info,
                    Text = displayName
                };
                DecoratePlayer?.Invoke(info, item);
                PlayerItemList.Add(item);
            }

            Sort();
        }
    }
}
