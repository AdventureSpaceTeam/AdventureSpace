﻿using System;
using Content.Client.Arcade;
using Content.Shared.Arcade;
using Content.Shared.GameObjects.Components.Arcade;
using JetBrains.Annotations;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Shared.GameObjects.Components.UserInterface;

namespace Content.Client.GameObjects.Components.Arcade
{
    public class BlockGameBoundUserInterface : BoundUserInterface
    {
        private BlockGameMenu _menu;

        public BlockGameBoundUserInterface([NotNull] ClientUserInterfaceComponent owner, [NotNull] object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _menu = new BlockGameMenu(this);
            _menu.OnClose += () => SendMessage(new BlockGameMessages.BlockGameUserUnregisterMessage());
            _menu.OnClose += Close;
            _menu.OpenCentered();
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            switch (message)
            {
                case BlockGameMessages.BlockGameVisualUpdateMessage updateMessage:
                    switch (updateMessage.GameVisualType)
                    {
                        case BlockGameMessages.BlockGameVisualType.GameField:
                            _menu?.UpdateBlocks(updateMessage.Blocks);
                            break;
                        case BlockGameMessages.BlockGameVisualType.HoldBlock:
                            _menu?.UpdateHeldBlock(updateMessage.Blocks);
                            break;
                        case BlockGameMessages.BlockGameVisualType.NextBlock:
                            _menu?.UpdateNextBlock(updateMessage.Blocks);
                            break;
                    }
                    break;
                case BlockGameMessages.BlockGameScoreUpdateMessage scoreUpdate:
                    _menu?.UpdatePoints(scoreUpdate.Points);
                    break;
                case BlockGameMessages.BlockGameUserStatusMessage userMessage:
                    _menu?.SetUsability(userMessage.IsPlayer);
                    break;
                case BlockGameMessages.BlockGameSetScreenMessage statusMessage:
                    if (statusMessage.isStarted) _menu?.SetStarted();
                    _menu?.SetScreen(statusMessage.Screen);
                    if (statusMessage is BlockGameMessages.BlockGameGameOverScreenMessage gameOverScreenMessage)
                        _menu?.SetGameoverInfo(gameOverScreenMessage.FinalScore, gameOverScreenMessage.LocalPlacement, gameOverScreenMessage.GlobalPlacement);
                    break;
                case BlockGameMessages.BlockGameHighScoreUpdateMessage highScoreUpdateMessage:
                    _menu?.UpdateHighscores(highScoreUpdateMessage.LocalHighscores,
                        highScoreUpdateMessage.GlobalHighscores);
                    break;
                case BlockGameMessages.BlockGameLevelUpdateMessage levelUpdateMessage:
                    _menu?.UpdateLevel(levelUpdateMessage.Level);
                    break;
            }
        }

        public void SendAction(BlockGamePlayerAction action)
        {
            SendMessage(new BlockGameMessages.BlockGamePlayerActionMessage(action));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if(!disposing) { return; }
            _menu?.Dispose();
        }
    }
}
