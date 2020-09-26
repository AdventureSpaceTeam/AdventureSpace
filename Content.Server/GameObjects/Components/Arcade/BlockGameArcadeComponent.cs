﻿#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Utility;
using Content.Shared.Arcade;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Random;

namespace Content.Server.GameObjects.Components.Arcade
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class BlockGameArcadeComponent : Component, IActivate
    {
        [Dependency] private IRobustRandom _random = null!;

        public override string Name => "BlockGameArcade";
        public override uint? NetID => ContentNetIDs.BLOCKGAME_ARCADE;
        private bool Powered => !Owner.TryGetComponent(out PowerReceiverComponent? receiver) || receiver.Powered;
        private BoundUserInterface? UserInterface => Owner.GetUIOrNull(BlockGameUiKey.Key);

        private BlockGame _game = null!;

        private IPlayerSession? _player;
        private List<IPlayerSession> _spectators = new List<IPlayerSession>();

        public void Activate(ActivateEventArgs eventArgs)
        {
            if(!eventArgs.User.TryGetComponent(out IActorComponent? actor))
            {
                return;
            }
            if (!Powered)
            {
                return;
            }
            if(!ActionBlockerSystem.CanInteract(Owner)) return;

            UserInterface?.Toggle(actor.playerSession);
            RegisterPlayerSession(actor.playerSession);
        }

        private void RegisterPlayerSession(IPlayerSession session)
        {
            if (_player == null) _player = session;
            else _spectators.Add(session);

            UpdatePlayerStatus(session);
            _game.UpdateNewPlayerUI(session);
        }

        private void DeactivePlayer(IPlayerSession session)
        {
            if (_player != session) return;

            var temp = _player;
            _player = null;
            if (_spectators.Count != 0)
            {
                _player = _spectators[0];
                _spectators.Remove(_player);
                UpdatePlayerStatus(_player);
            }
            _spectators.Add(temp);

            UpdatePlayerStatus(temp);
        }

        private void UnRegisterPlayerSession(IPlayerSession session)
        {
            if (_player == session)
            {
                DeactivePlayer(_player);
            }
            else
            {
                _spectators.Remove(session);
                UpdatePlayerStatus(session);
            }
        }

        private void UpdatePlayerStatus(IPlayerSession session)
        {
            UserInterface?.SendMessage(new BlockGameMessages.BlockGameUserStatusMessage(_player == session), session);
        }

        public override void Initialize()
        {
            base.Initialize();
            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnOnReceiveMessage;
            }
            _game = new BlockGame(this);
        }

        private void UserInterfaceOnOnReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            if (obj.Message is BlockGameMessages.BlockGameUserUnregisterMessage unregisterMessage)
            {
                UnRegisterPlayerSession(obj.Session);
                return;
            }
            if (obj.Session != _player) return;

            if (!ActionBlockerSystem.CanInteract(Owner))
            {
                DeactivePlayer(obj.Session);
            }

            if (!(obj.Message is BlockGameMessages.BlockGamePlayerActionMessage message)) return;
            if (message.PlayerAction == BlockGamePlayerAction.NewGame)
            {
                if(_game.Started) _game = new BlockGame(this);
                _game.StartGame();
            }
            else
            {
                _game.ProcessInput(message.PlayerAction);
            }
        }

        public void DoGameTick(float frameTime)
        {
            _game.GameTick(frameTime);
        }

        private class BlockGame
        {
            //note: field is 10(0 -> 9) wide and 20(0 -> 19) high

            private BlockGameArcadeComponent _component;

            private List<BlockGameBlock> _field = new List<BlockGameBlock>();

            private BlockGamePiece _currentPiece;

            private BlockGamePiece _nextPiece
            {
                get => _internalNextPiece;
                set
                {
                    _internalNextPiece = value;
                    SendNextPieceUpdate();
                }
            }
            private BlockGamePiece _internalNextPiece;

            private void SendNextPieceUpdate()
            {
                _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameVisualUpdateMessage(_nextPiece.BlocksForPreview(), BlockGameMessages.BlockGameVisualType.NextBlock));
            }

            private void SendNextPieceUpdate(IPlayerSession session)
            {
                _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameVisualUpdateMessage(_nextPiece.BlocksForPreview(), BlockGameMessages.BlockGameVisualType.NextBlock), session);
            }

            private bool _holdBlock = false;
            private BlockGamePiece? _heldPiece
            {
                get => _internalHeldPiece;
                set
                {
                    _internalHeldPiece = value;
                    SendHoldPieceUpdate();
                }
            }

            private BlockGamePiece? _internalHeldPiece = null;

            private void SendHoldPieceUpdate()
            {
                if(_heldPiece.HasValue) _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameVisualUpdateMessage(_heldPiece.Value.BlocksForPreview(), BlockGameMessages.BlockGameVisualType.HoldBlock));
                else _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameVisualUpdateMessage(new BlockGameBlock[0], BlockGameMessages.BlockGameVisualType.HoldBlock));
            }

            private void SendHoldPieceUpdate(IPlayerSession session)
            {
                if(_heldPiece.HasValue) _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameVisualUpdateMessage(_heldPiece.Value.BlocksForPreview(), BlockGameMessages.BlockGameVisualType.HoldBlock), session);
                else _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameVisualUpdateMessage(new BlockGameBlock[0], BlockGameMessages.BlockGameVisualType.HoldBlock), session);
            }

            private Vector2i _currentPiecePosition;
            private BlockGamePieceRotation _currentRotation;
            private float _softDropOverride = 0.1f;

            private float Speed => !_softDropPressed
                ? -0.03f * Level + 1
                : _softDropOverride;

            private float _pressCheckSpeed = 0.08f;

            private bool _running;
            public bool Paused => !(_running && _started);
            private bool _started;
            public bool Started => _started;
            private bool _gameOver;

            private bool _leftPressed;
            private bool _rightPressed;
            private bool _softDropPressed;

            private int Points
            {
                get => _internalPoints;
                set
                {
                    if (_internalPoints == value) return;
                    _internalPoints = value;
                    SendPointsUpdate();
                }
            }
            private int _internalPoints;

            private BlockGameSystem.HighScorePlacement? _highScorePlacement = null;

            private void SendPointsUpdate()
            {
                _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameScoreUpdateMessage(Points));
            }

            private void SendPointsUpdate(IPlayerSession session)
            {
                _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameScoreUpdateMessage(Points));
            }

            public int Level
            {
                get => _level;
                set
                {
                    _level = value;
                    SendLevelUpdate();
                }
            }
            private int _level = 0;
            private void SendLevelUpdate()
            {
                _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameLevelUpdateMessage(Level));
            }

            private void SendLevelUpdate(IPlayerSession session)
            {
                _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameLevelUpdateMessage(Level));
            }

            private int ClearedLines
            {
                get => _clearedLines;
                set
                {
                    _clearedLines = value;

                    if (_clearedLines < LevelRequirement) return;

                    _clearedLines -= LevelRequirement;
                    Level++;
                }
            }

            private int _clearedLines = 0;
            private int LevelRequirement => Math.Min(100, Math.Max(Level * 10 - 50, 10));

            public BlockGame(BlockGameArcadeComponent component)
            {
                _component = component;
                _internalNextPiece = BlockGamePiece.GetRandom(_component._random);
            }

            private void SendHighscoreUpdate()
            {
                var entitySystem = EntitySystem.Get<BlockGameSystem>();
                _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameHighScoreUpdateMessage(entitySystem.GetLocalHighscores(), entitySystem.GetGlobalHighscores()));
            }

            private void SendHighscoreUpdate(IPlayerSession session)
            {
                var entitySystem = EntitySystem.Get<BlockGameSystem>();
                _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameHighScoreUpdateMessage(entitySystem.GetLocalHighscores(), entitySystem.GetGlobalHighscores()), session);
            }

            public void StartGame()
            {
                InitializeNewBlock();

                _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameSetScreenMessage(BlockGameMessages.BlockGameScreen.Game));

                FullUpdate();

                _running = true;
                _started = true;
            }

            private void FullUpdate()
            {
                UpdateAllFieldUI();
                SendHoldPieceUpdate();
                SendNextPieceUpdate();
                SendPointsUpdate();
                SendHighscoreUpdate();
                SendLevelUpdate();
            }

            private void FullUpdate(IPlayerSession session)
            {
                UpdateFieldUI(session);
                SendPointsUpdate(session);
                SendNextPieceUpdate(session);
                SendHoldPieceUpdate(session);
                SendHighscoreUpdate(session);
                SendLevelUpdate(session);
            }

            public void GameTick(float frameTime)
            {
                if (!_running) return;

                InputTick(frameTime);

                FieldTick(frameTime);
            }

            private float _accumulatedLeftPressTime;
            private float _accumulatedRightPressTime;
            private void InputTick(float frameTime)
            {
                bool anythingChanged = false;
                if (_leftPressed)
                {
                    _accumulatedLeftPressTime += frameTime;

                    if (_accumulatedLeftPressTime >= _pressCheckSpeed)
                    {

                        if (_currentPiece.Positions(_currentPiecePosition.AddToX(-1), _currentRotation)
                            .All(MoveCheck))
                        {
                            _currentPiecePosition = _currentPiecePosition.AddToX(-1);
                            anythingChanged = true;
                        }

                        _accumulatedLeftPressTime -= _pressCheckSpeed;
                    }
                }

                if (_rightPressed)
                {
                    _accumulatedRightPressTime += frameTime;

                    if (_accumulatedRightPressTime >= _pressCheckSpeed)
                    {
                        if (_currentPiece.Positions(_currentPiecePosition.AddToX(1), _currentRotation)
                            .All(MoveCheck))
                        {
                            _currentPiecePosition = _currentPiecePosition.AddToX(1);
                            anythingChanged = true;
                        }

                        _accumulatedRightPressTime -= _pressCheckSpeed;
                    }
                }

                if(anythingChanged) UpdateAllFieldUI();
            }

            private float _accumulatedFieldFrameTime;
            private void FieldTick(float frameTime)
            {
                _accumulatedFieldFrameTime += frameTime;

                var checkTime = Speed;

                if (_accumulatedFieldFrameTime < checkTime) return;

                if(_softDropPressed) AddPoints(1);

                InternalFieldTick();

                _accumulatedFieldFrameTime -= checkTime;
            }

            private void InternalFieldTick()
            {
                if (_currentPiece.Positions(_currentPiecePosition.AddToY(1), _currentRotation)
                    .All(DropCheck))
                {
                    _currentPiecePosition = _currentPiecePosition.AddToY(1);
                }
                else
                {
                    var blocks = _currentPiece.Blocks(_currentPiecePosition, _currentRotation);
                    _field.AddRange(blocks);

                    //check loose conditions
                    if (IsGameOver)
                    {
                        InvokeGameover();
                        return;
                    }

                    InitializeNewBlock();
                }

                CheckField();

                UpdateAllFieldUI();
            }

            private void CheckField()
            {
                int pointsToAdd = 0;
                int consecutiveLines = 0;
                int clearedLines = 0;
                for (int y = 0; y < 20; y++)
                {
                    if (CheckLine(y))
                    {
                        //line was cleared
                        y--;
                        consecutiveLines++;
                        clearedLines++;
                    }
                    else if(consecutiveLines != 0)
                    {
                        var mod = consecutiveLines switch
                        {
                            1 => 40,
                            2 => 100,
                            3 => 300,
                            4 => 1200,
                            _ => 0
                        };
                        pointsToAdd += mod * (_level + 1);
                    }
                }

                ClearedLines += clearedLines;
                AddPoints(pointsToAdd);
            }

            private bool CheckLine(int y)
            {
                for (var x = 0; x < 10; x++)
                {
                    if (!_field.Any(b => b.Position.X == x && b.Position.Y == y)) return false;
                }

                //clear line
                _field.RemoveAll(b => b.Position.Y == y);
                //move everything down
                FillLine(y);

                return true;
            }

            private void AddPoints(int amount)
            {
                if (amount == 0) return;

                Points += amount;
            }

            private void FillLine(int y)
            {
                for (int c_y = y; c_y > 0; c_y--)
                {
                    for (int j = 0; j < _field.Count; j++)
                    {
                        if(_field[j].Position.Y != c_y-1) continue;

                        _field[j] = new BlockGameBlock(_field[j].Position.AddToY(1), _field[j].GameBlockColor);
                    }
                }
            }

            private void InitializeNewBlock()
            {
                InitializeNewBlock(_nextPiece);
                _nextPiece = BlockGamePiece.GetRandom(_component._random);
                _holdBlock = false;

                _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameVisualUpdateMessage(_nextPiece.BlocksForPreview(), BlockGameMessages.BlockGameVisualType.NextBlock));
            }

            private void InitializeNewBlock(BlockGamePiece piece)
            {
                _currentPiecePosition = new Vector2i(5,0);

                _currentRotation = BlockGamePieceRotation.North;

                _currentPiece = piece;
                UpdateAllFieldUI();
            }

            private bool LowerBoundCheck(Vector2i position) => position.Y < 20;
            private bool BorderCheck(Vector2i position) => position.X >= 0 && position.X < 10;
            private bool ClearCheck(Vector2i position) => _field.All(block => !position.Equals(block.Position));

            private bool DropCheck(Vector2i position) => LowerBoundCheck(position) && ClearCheck(position);
            private bool MoveCheck(Vector2i position) => BorderCheck(position) && ClearCheck(position);
            private bool RotateCheck(Vector2i position) => BorderCheck(position) && LowerBoundCheck(position) && ClearCheck(position);

            public void ProcessInput(BlockGamePlayerAction action)
            {
                switch (action)
                {
                    case BlockGamePlayerAction.StartLeft:
                        _leftPressed = true;
                        break;
                    case BlockGamePlayerAction.EndLeft:
                        _leftPressed = false;
                        break;
                    case BlockGamePlayerAction.StartRight:
                        _rightPressed = true;
                        break;
                    case BlockGamePlayerAction.EndRight:
                        _rightPressed = false;
                        break;
                    case BlockGamePlayerAction.Rotate:
                        TrySetRotation(Next(_currentRotation, false));
                        break;
                    case BlockGamePlayerAction.CounterRotate:
                        TrySetRotation(Next(_currentRotation, true));
                        break;
                    case BlockGamePlayerAction.SoftdropStart:
                        _softDropPressed = true;
                        break;
                    case BlockGamePlayerAction.SoftdropEnd:
                        _softDropPressed = false;
                        break;
                    case BlockGamePlayerAction.Harddrop:
                        PerformHarddrop();
                        break;
                    case BlockGamePlayerAction.Pause:
                        _running = false;
                        _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameSetScreenMessage(BlockGameMessages.BlockGameScreen.Pause));
                        break;
                    case BlockGamePlayerAction.Unpause:
                        if (!_gameOver)
                        {
                            _running = true;
                            _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameSetScreenMessage(BlockGameMessages.BlockGameScreen.Game));
                        }
                        break;
                    case BlockGamePlayerAction.Hold:
                        HoldPiece();
                        break;
                    case BlockGamePlayerAction.ShowHighscores:
                        _running = false;
                        _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameSetScreenMessage(BlockGameMessages.BlockGameScreen.Highscores));
                        break;
                }
            }

            private void TrySetRotation(BlockGamePieceRotation rotation)
            {
                if(!_running) return;

                if (!_currentPiece.CanSpin) return;

                if (!_currentPiece.Positions(_currentPiecePosition, rotation)
                    .All(RotateCheck))
                {
                    return;
                }

                _currentRotation = rotation;
                UpdateAllFieldUI();
            }

            private void HoldPiece()
            {
                if (!_running) return;

                if (_holdBlock) return;

                var tempHeld = _heldPiece;
                _heldPiece = _currentPiece;
                _holdBlock = true;

                if (!tempHeld.HasValue)
                {
                    InitializeNewBlock();
                    return;
                }

                InitializeNewBlock(tempHeld.Value);
            }

            private void PerformHarddrop()
            {
                int spacesDropped = 0;
                while (_currentPiece.Positions(_currentPiecePosition.AddToY(1), _currentRotation)
                    .All(DropCheck))
                {
                    _currentPiecePosition = _currentPiecePosition.AddToY(1);
                    spacesDropped++;
                }
                AddPoints(spacesDropped * 2);

                InternalFieldTick();
            }

            public void UpdateAllFieldUI()
            {
                if (!_started) return;

                var computedField = ComputeField();
                _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameVisualUpdateMessage(computedField.ToArray(), BlockGameMessages.BlockGameVisualType.GameField));
            }

            public void UpdateFieldUI(IPlayerSession session)
            {
                if (!_started) return;

                var computedField = ComputeField();
                _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameVisualUpdateMessage(computedField.ToArray(), BlockGameMessages.BlockGameVisualType.GameField), session);
            }

            private bool IsGameOver => _field.Any(block => block.Position.Y == 0);
            private void InvokeGameover()
            {
                _running = false;
                _gameOver = true;

                if (_component._player?.AttachedEntity != null)
                {
                    var blockGameSystem = EntitySystem.Get<BlockGameSystem>();

                    _highScorePlacement = blockGameSystem.RegisterHighScore(_component._player.AttachedEntity.Name, Points);
                    SendHighscoreUpdate();
                }
                _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameGameOverScreenMessage(Points, _highScorePlacement?.LocalPlacement, _highScorePlacement?.GlobalPlacement));
            }

            public void UpdateNewPlayerUI(IPlayerSession session)
            {
                if (_gameOver)
                {
                    _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameGameOverScreenMessage(Points, _highScorePlacement?.LocalPlacement, _highScorePlacement?.GlobalPlacement), session);
                }
                else
                {
                    if (Paused)
                    {
                        _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameSetScreenMessage(BlockGameMessages.BlockGameScreen.Pause, Started), session);
                    }
                    else
                    {
                        _component.UserInterface?.SendMessage(new BlockGameMessages.BlockGameSetScreenMessage(BlockGameMessages.BlockGameScreen.Game, Started), session);
                    }
                }

                FullUpdate(session);
            }

            public List<BlockGameBlock> ComputeField()
            {
                var result = new List<BlockGameBlock>();
                result.AddRange(_field);
                result.AddRange(_currentPiece.Blocks(_currentPiecePosition, _currentRotation));
                return result;
            }

            private enum BlockGamePieceType
            {
                I,
                L,
                LInverted,
                S,
                SInverted,
                T,
                O
            }

            private enum BlockGamePieceRotation
            {
                North,
                East,
                South,
                West
            }

            private static BlockGamePieceRotation Next(BlockGamePieceRotation rotation, bool inverted)
            {
                return rotation switch
                {
                    BlockGamePieceRotation.North => inverted ? BlockGamePieceRotation.West : BlockGamePieceRotation.East,
                    BlockGamePieceRotation.East => inverted ? BlockGamePieceRotation.North : BlockGamePieceRotation.South,
                    BlockGamePieceRotation.South => inverted ? BlockGamePieceRotation.East : BlockGamePieceRotation.West,
                    BlockGamePieceRotation.West => inverted ? BlockGamePieceRotation.South : BlockGamePieceRotation.North,
                    _ => throw new ArgumentOutOfRangeException(nameof(rotation), rotation, null)
                };
            }

            private struct BlockGamePiece
            {
                public Vector2i[] Offsets;
                private BlockGameBlock.BlockGameBlockColor _gameBlockColor;
                public bool CanSpin;

                public Vector2i[] Positions(Vector2i center,
                    BlockGamePieceRotation rotation)
                {
                    return RotatedOffsets(rotation).Select(v => center + v).ToArray();
                }

                private Vector2i[] RotatedOffsets(BlockGamePieceRotation rotation)
                {
                    Vector2i[] rotatedOffsets = (Vector2i[])Offsets.Clone();
                    //until i find a better algo
                    var amount = rotation switch
                    {
                        BlockGamePieceRotation.North => 0,
                        BlockGamePieceRotation.East => 1,
                        BlockGamePieceRotation.South => 2,
                        BlockGamePieceRotation.West => 3,
                        _ => 0
                    };

                    for (var i = 0; i < amount; i++)
                    {
                        for (var j = 0; j < rotatedOffsets.Length; j++)
                        {
                            rotatedOffsets[j] = rotatedOffsets[j].Rotate90DegreesAsOffset();
                        }
                    }

                    return rotatedOffsets;
                }

                public BlockGameBlock[] Blocks(Vector2i center,
                    BlockGamePieceRotation rotation)
                {
                    var positions = Positions(center, rotation);
                    var result = new BlockGameBlock[positions.Length];
                    var i = 0;
                    foreach (var position in positions)
                    {
                        result[i++] = position.ToBlockGameBlock(_gameBlockColor);
                    }

                    return result;
                }

                public BlockGameBlock[] BlocksForPreview()
                {
                    var xOffset = 0;
                    var yOffset = 0;
                    foreach (var offset in Offsets)
                    {
                        if (offset.X < xOffset) xOffset = offset.X;
                        if (offset.Y < yOffset) yOffset = offset.Y;
                    }

                    return Blocks(new Vector2i(-xOffset, -yOffset), BlockGamePieceRotation.North);
                }

                public static BlockGamePiece GetRandom(IRobustRandom random)
                {
                    var pieces = (BlockGamePieceType[])Enum.GetValues(typeof(BlockGamePieceType));
                    var choice = random.Pick(pieces);
                    return GetPiece(choice);
                }

                public static BlockGamePiece GetPiece(BlockGamePieceType type)
                {
                    //switch statement, hardcoded offsets
                    return type switch
                    {
                        BlockGamePieceType.I => new BlockGamePiece
                        {
                            Offsets = new[]
                            {
                                new Vector2i(0, -1), new Vector2i(0, 0), new Vector2i(0, 1), new Vector2i(0, 2),
                            },
                            _gameBlockColor = BlockGameBlock.BlockGameBlockColor.LightBlue,
                            CanSpin = true
                        },
                        BlockGamePieceType.L => new BlockGamePiece
                        {
                            Offsets = new[]
                            {
                                new Vector2i(0, -1), new Vector2i(0, 0), new Vector2i(0, 1), new Vector2i(1, 1),
                            },
                            _gameBlockColor = BlockGameBlock.BlockGameBlockColor.Orange,
                            CanSpin = true
                        },
                        BlockGamePieceType.LInverted => new BlockGamePiece
                        {
                            Offsets = new[]
                            {
                                new Vector2i(0, -1), new Vector2i(0, 0), new Vector2i(-1, 1),
                                new Vector2i(0, 1),
                            },
                            _gameBlockColor = BlockGameBlock.BlockGameBlockColor.Blue,
                            CanSpin = true
                        },
                        BlockGamePieceType.S => new BlockGamePiece
                        {
                            Offsets = new[]
                            {
                                new Vector2i(0, -1), new Vector2i(1, -1), new Vector2i(-1, 0),
                                new Vector2i(0, 0),
                            },
                            _gameBlockColor = BlockGameBlock.BlockGameBlockColor.Green,
                            CanSpin = true
                        },
                        BlockGamePieceType.SInverted => new BlockGamePiece
                        {
                            Offsets = new[]
                            {
                                new Vector2i(-1, -1), new Vector2i(0, -1), new Vector2i(0, 0),
                                new Vector2i(1, 0),
                            },
                            _gameBlockColor = BlockGameBlock.BlockGameBlockColor.Red,
                            CanSpin = true
                        },
                        BlockGamePieceType.T => new BlockGamePiece
                        {
                            Offsets = new[]
                            {
                                new Vector2i(0, -1),
                                new Vector2i(-1, 0), new Vector2i(0, 0), new Vector2i(1, 0),
                            },
                            _gameBlockColor = BlockGameBlock.BlockGameBlockColor.Purple,
                            CanSpin = true
                        },
                        BlockGamePieceType.O => new BlockGamePiece
                        {
                            Offsets = new[]
                            {
                                new Vector2i(0, -1), new Vector2i(1, -1), new Vector2i(0, 0),
                                new Vector2i(1, 0),
                            },
                            _gameBlockColor = BlockGameBlock.BlockGameBlockColor.Yellow,
                            CanSpin = false
                        },
                        _ => new BlockGamePiece {Offsets = new[] {new Vector2i(0, 0)}}
                    };
                }
            }
        }
    }
}
