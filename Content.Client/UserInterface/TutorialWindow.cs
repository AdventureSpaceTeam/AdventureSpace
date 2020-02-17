﻿using Robust.Client.Graphics;
using Robust.Client.Interfaces.Input;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using static Content.Shared.Input.ContentKeyFunctions;
using static Robust.Shared.Input.EngineKeyFunctions;

namespace Content.Client.UserInterface
{
    public sealed class TutorialWindow : SS14Window
    {
        private readonly int _headerFontSize = 14;
        private VBoxContainer VBox { get; }

        private const string IntroContents = @"Hi and welcome to Space Station 14! This tutorial will assume that you know a bit about how SS13 plays. It's mostly intended to lay out the controls and their differences from SS13.
";
        private const string GameplayContents = @"Some notes on gameplay. To talk in OOC, prefix your chat message with \[ or /ooc. Death is currently show as a black circle around the player. You can respawn via the respawn button in the sandbox menu. Instead of intents, we have ""combat mode"". Check controls above for its keybind. You can't attack anybody with it off, so no more hitting yourself with your own crowbar.
";
        private const string FeedbackContents = @"If you have any feedback, questions, bug reports, etc..., do not be afraid to tell us! You can ask on Discord or heck, just write it in OOC! We'll catch it.";

        protected override Vector2? CustomSize => (520, 580);

        public TutorialWindow()
        {
            Title = "The Tutorial!";

            //Get section header font
            var cache = IoCManager.Resolve<IResourceCache>();
            var inputManager = IoCManager.Resolve<IInputManager>();
            Font headerFont = new VectorFont(cache.GetResource<FontResource>("/Nano/NotoSans/NotoSans-Regular.ttf"), _headerFontSize);

            var scrollContainer = new ScrollContainer();
            scrollContainer.AddChild(VBox = new VBoxContainer());
            Contents.AddChild(scrollContainer);

            //Intro
            VBox.AddChild(new Label{FontOverride = headerFont, Text = "Intro"});
            AddFormattedText(IntroContents);

            string Key(BoundKeyFunction func)
            {
                return FormattedMessage.EscapeText(inputManager.GetKeyFunctionButtonString(func));
            }

            //Controls
            VBox.AddChild(new Label{FontOverride = headerFont, Text = "Controls"});

            // Moved this down here so that Rider shows which args correspond to which format spot.
            AddFormattedText(Loc.GetString(@"Movement: [color=#a4885c]{0} {1} {2} {3}[/color]
Switch hands: [color=#a4885c]{4}[/color]
Use held item: [color=#a4885c]{5}[/color]
Drop held item: [color=#a4885c]{6}[/color]
Open inventory: [color=#a4885c]{7}[/color]
Open character window: [color=#a4885c]{8}[/color]
Open crafting window: [color=#a4885c]{9}[/color]
Focus chat: [color=#a4885c]{10}[/color]
Use targeted entity: [color=#a4885c]{11}[/color]
Throw held item: [color=#a4885c]{12}[/color]
Examine entity: [color=#a4885c]{13}[/color]
Open entity context menu: [color=#a4885c]{14}[/color]
Toggle combat mode: [color=#a4885c]{15}[/color]
Toggle console: [color=#a4885c]{16}[/color]
Toggle UI: [color=#a4885c]{17}[/color]
Toggle debug overlay: [color=#a4885c]{18}[/color]
Toggle entity spawner: [color=#a4885c]{19}[/color]
Toggle tile spawner: [color=#a4885c]{20}[/color]
Toggle sandbox window: [color=#a4885c]{21}[/color]
",
                Key(MoveUp), Key(MoveLeft), Key(MoveDown), Key(MoveRight),
                Key(SwapHands),
                Key(ActivateItemInHand),
                Key(Drop),
                Key(OpenInventoryMenu),
                Key(OpenCharacterMenu),
                Key(OpenCraftingMenu),
                Key(FocusChat),
                Key(ActivateItemInWorld),
                Key(ThrowItemInHand),
                Key(ExamineEntity),
                Key(OpenContextMenu),
                Key(ToggleCombatMode),
                Key(ShowDebugConsole),
                Key(HideUI),
                Key(ShowDebugMonitors),
                Key(OpenEntitySpawnWindow),
                Key(OpenTileSpawnWindow),
                Key(OpenSandboxWindow)));

            //Gameplay
            VBox.AddChild(new Label { FontOverride = headerFont, Text = "Gameplay" });
            AddFormattedText(GameplayContents);

            //Feedback
            VBox.AddChild(new Label { FontOverride = headerFont, Text = "Feedback" });
            AddFormattedText(FeedbackContents);
        }

        private void AddFormattedText(string text)
        {
            if(VBox == null)
                return;

            var introLabel = new RichTextLabel();
            var introMessage = new FormattedMessage();
            introMessage.AddMarkup(text);
            introLabel.SetMessage(introMessage);
            VBox.AddChild(introLabel);
        }
    }
}
