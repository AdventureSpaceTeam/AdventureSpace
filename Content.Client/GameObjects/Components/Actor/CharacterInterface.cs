﻿using System.Collections.Generic;
using System.Linq;
using Content.Client.GameObjects.Components.Mobs;
using Content.Client.UserInterface;
using Content.Shared.Input;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.Input;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;

namespace Content.Client.GameObjects.Components.Actor
{
    /// <summary>
    /// A semi-abstract component which gets added to entities upon attachment and collects all character
    /// user interfaces into a single window and keybind for the user
    /// </summary>
    [RegisterComponent]
    public class CharacterInterface : Component
    {
        public override string Name => "Character Interface Component";

        [Dependency]
#pragma warning disable 649
        private readonly IGameHud _gameHud;
#pragma warning restore 649

        /// <summary>
        ///     Window to hold each of the character interfaces
        /// </summary>
        /// <remarks>
        ///     Null if it would otherwise be empty.
        /// </remarks>
        public SS14Window Window { get; private set; }

        /// <summary>
        /// Create the window with all character UIs and bind it to a keypress
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            //Use all the character ui interfaced components to create the character window
            var uiComponents = Owner.GetAllComponents<ICharacterUI>().ToList();
            if (uiComponents.Count == 0)
            {
                return;
            }

            Window = new CharacterWindow(uiComponents);
            Window.OnClose += () => _gameHud.CharacterButtonDown = false;
        }

        /// <summary>
        /// Dispose of window and the keypress binding
        /// </summary>
        public override void OnRemove()
        {
            base.OnRemove();

            Window?.Dispose();
            Window = null;

            var inputMgr = IoCManager.Resolve<IInputManager>();
            inputMgr.SetInputCommand(ContentKeyFunctions.OpenCharacterMenu, null);
        }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null,
            IComponent component = null)
        {
            base.HandleMessage(message, netChannel, component);

            switch (message)
            {
                case PlayerAttachedMsg _:
                    if (Window != null)
                    {
                        _gameHud.CharacterButtonVisible = true;
                        _gameHud.CharacterButtonToggled = b =>
                        {
                            if (b)
                            {
                                Window.Open();
                            }
                            else
                            {
                                Window.Close();
                            }
                        };
                    }

                    break;

                case PlayerDetachedMsg _:
                    if (Window != null)
                    {
                        _gameHud.CharacterButtonVisible = false;
                        Window.Close();
                    }

                    break;
            }
        }

        /// <summary>
        /// A window that collects and shows all the individual character user interfaces
        /// </summary>
        public class CharacterWindow : SS14Window
        {
            private readonly VBoxContainer _contentsVBox;

            public CharacterWindow(List<ICharacterUI> windowComponents)
            {
                Title = "Character";

                _contentsVBox = new VBoxContainer();
                Contents.AddChild(_contentsVBox);

                windowComponents.Sort((a, b) => ((int) a.Priority).CompareTo((int) b.Priority));
                foreach (var element in windowComponents)
                {
                    _contentsVBox.AddChild(element.Scene);
                }
            }
        }
    }

    /// <summary>
    /// Determines ordering of the character user interface, small values come sooner
    /// </summary>
    public enum UIPriority
    {
        First = 0,
        Info = 5,
        Species = 100,
        Last = 99999
    }
}
