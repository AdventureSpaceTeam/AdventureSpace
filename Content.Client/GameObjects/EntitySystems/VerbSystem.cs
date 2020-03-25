﻿using System;
using System.Collections.Generic;
using System.Reflection;
using Content.Client.State;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.EntitySystemMessages;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Interfaces.Input;
using Robust.Client.Interfaces.State;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public sealed class VerbSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IStateManager _stateManager;
        [Dependency] private readonly IEntityManager _entityManager;
        [Dependency] private readonly IPlayerManager _playerManager;
        [Dependency] private readonly IInputManager _inputManager;
#pragma warning restore 649

        private VerbPopup _currentPopup;
        private EntityUid _currentEntity;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<VerbSystemMessages.VerbsResponseMessage>(FillEntityPopup);

            IoCManager.InjectDependencies(this);

            var input = EntitySystemManager.GetEntitySystem<InputSystem>();
            input.BindMap.BindFunction(ContentKeyFunctions.OpenContextMenu,
                new PointerInputCmdHandler(OnOpenContextMenu));
        }

        public void OpenContextMenu(IEntity entity, ScreenCoordinates screenCoordinates)
        {
            if (_currentPopup != null)
            {
                CloseContextMenu();
            }

            _currentEntity = entity.Uid;
            _currentPopup = new VerbPopup();
            _currentPopup.UserInterfaceManager.ModalRoot.AddChild(_currentPopup);
            _currentPopup.OnPopupHide += CloseContextMenu;

            _currentPopup.List.AddChild(new Label {Text = "Waiting on Server..."});
            RaiseNetworkEvent(new VerbSystemMessages.RequestVerbsMessage(_currentEntity));

            var size = _currentPopup.List.CombinedMinimumSize;
            var box = UIBox2.FromDimensions(screenCoordinates.Position, size);
            _currentPopup.Open(box);
        }

        private bool OnOpenContextMenu(in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            if (_currentPopup != null)
            {
                CloseContextMenu();
                return true;
            }

            if (!(_stateManager.CurrentState is GameScreenBase gameScreen))
            {
                return false;
            }

            var entities = gameScreen.GetEntitiesUnderPosition(args.Coordinates);

            if (entities.Count == 0)
            {
                return false;
            }

            _currentPopup = new VerbPopup();
            _currentPopup.OnPopupHide += CloseContextMenu;
            foreach (var entity in entities)
            {
                var button = new Button {Text = entity.Name};
                _currentPopup.List.AddChild(button);
                button.OnPressed += _ => OnContextButtonPressed(entity);
            }

            _currentPopup.UserInterfaceManager.ModalRoot.AddChild(_currentPopup);

            var size = _currentPopup.List.CombinedMinimumSize;
            var box = UIBox2.FromDimensions(args.ScreenCoordinates.Position, size);
            _currentPopup.Open(box);

            return true;
        }

        private void OnContextButtonPressed(IEntity entity)
        {
            OpenContextMenu(entity, new ScreenCoordinates(_inputManager.MouseScreenPosition));
        }

        private void FillEntityPopup(VerbSystemMessages.VerbsResponseMessage msg)
        {
            if (_currentEntity != msg.Entity || !_entityManager.TryGetEntity(_currentEntity, out var entity))
            {
                return;
            }

            DebugTools.AssertNotNull(_currentPopup);

            var buttons = new Dictionary<string, List<Button>>();

            var vBox = _currentPopup.List;
            vBox.DisposeAllChildren();
            foreach (var data in msg.Verbs)
            {
                var button = new Button {Text = data.Text, Disabled = !data.Available};
                if (data.Available)
                {
                    button.OnPressed += _ =>
                    {
                        RaiseNetworkEvent(new VerbSystemMessages.UseVerbMessage(_currentEntity, data.Key));
                        CloseContextMenu();
                    };
                }

                if(!buttons.ContainsKey(data.Category))
                    buttons[data.Category] = new List<Button>();

                buttons[data.Category].Add(button);
            }

            var user = GetUserEntity();
            //Get verbs, component dependent.
            foreach (var (component, verb) in VerbUtility.GetVerbs(entity))
            {
                if (verb.RequireInteractionRange && !VerbUtility.InVerbUseRange(user, entity))
                    continue;

                var disabled = verb.GetVisibility(user, component) != VerbVisibility.Visible;
                var category = verb.GetCategory(user, component);


                if(!buttons.ContainsKey(category))
                    buttons[category] = new List<Button>();

                buttons[category].Add(CreateVerbButton(verb.GetText(user, component), disabled, verb.ToString(),
                    entity.ToString(), () => verb.Activate(user, component)));
            }
            //Get global verbs. Visible for all entities regardless of their components.
            foreach (var globalVerb in VerbUtility.GetGlobalVerbs(Assembly.GetExecutingAssembly()))
            {
                if (globalVerb.RequireInteractionRange && !VerbUtility.InVerbUseRange(user, entity))
                    continue;

                var disabled = globalVerb.GetVisibility(user, entity) != VerbVisibility.Visible;
                var category = globalVerb.GetCategory(user, entity);

                if(!buttons.ContainsKey(category))
                    buttons[category] = new List<Button>();

                buttons[category].Add(CreateVerbButton(globalVerb.GetText(user, entity), disabled, globalVerb.ToString(),
                    entity.ToString(), () => globalVerb.Activate(user, entity)));
            }

            if (buttons.Count > 0)
            {
                foreach (var (category, verbs) in buttons)
                {
                    if (string.IsNullOrEmpty(category))
                        continue;

                    vBox.AddChild(CreateCategoryButton(category, verbs));
                }

                if (buttons.ContainsKey(""))
                {
                    buttons[""].Sort((a, b) => string.Compare(a.Text, b.Text, StringComparison.Ordinal));

                    foreach (var verb in buttons[""])
                    {
                        vBox.AddChild(verb);
                    }
                }
            }
            else
            {
                var panel = new PanelContainer();
                panel.AddChild(new Label {Text = "No verbs!"});
                vBox.AddChild(panel);
            }
        }

        private Button CreateVerbButton(string text, bool disabled, string verbName, string ownerName, Action action)
        {
            var button = new Button
            {
                Text = text,
                Disabled = disabled
            };
            if (!disabled)
            {
                button.OnPressed += _ =>
                {
                    CloseContextMenu();
                    try
                    {
                        action.Invoke();
                    }
                    catch (Exception e)
                    {
                        Logger.ErrorS("verb", "Exception in verb {0} on {1}:\n{2}", verbName, ownerName, e);
                    }
                };
            }
            return button;
        }

        private Button CreateCategoryButton(string text, List<Button> verbButtons)
        {
            verbButtons.Sort((a, b) => string.Compare(a.Text, b.Text, StringComparison.Ordinal));

            var button = new Button
            {
                Text = $"{text}...",
            };
            button.OnPressed += _ =>
                {
                    _currentPopup.List.DisposeAllChildren();
                    foreach (var verb in verbButtons)
                    {
                        _currentPopup.List.AddChild(verb);
                    }
                };
            return button;
        }

        private void CloseContextMenu()
        {
            _currentPopup?.Dispose();
            _currentPopup = null;
            _currentEntity = EntityUid.Invalid;
        }

        private IEntity GetUserEntity()
        {
            return _playerManager.LocalPlayer.ControlledEntity;
        }

        private sealed class VerbPopup : Popup
        {
            public VBoxContainer List { get; }

            public VerbPopup()
            {
                AddChild(List = new VBoxContainer());
            }
        }
    }
}
