using Content.Client.ContextMenu.UI;
using Content.Shared.Verbs;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Client.Verbs.UI
{
    /// <summary>
    ///     Slight extension of <see cref="ContextMenuElement"/> that uses a SpriteSpecifier for it's icon and provides
    ///     constructors that take verbs or verb categories.
    /// </summary>
    public sealed partial class VerbMenuElement : ContextMenuElement
    {
        public const string StyleClassVerbMenuConfirmationTexture = "verbMenuConfirmationTexture";

        public const float VerbTooltipDelay = 0.5f;

        // Setters to provide access to children generated by XAML.
        public bool IconVisible { set => Icon.Visible = value; }
        public bool TextVisible { set => Label.Visible = value; }

        // Top quality variable naming
        public readonly Verb? Verb;

        public VerbMenuElement(Verb verb) : base(verb.Text)
        {
            ToolTip = verb.Message;
            TooltipDelay = VerbTooltipDelay;
            Disabled = verb.Disabled;
            Verb = verb;

            Label.SetOnlyStyleClass(verb.TextStyleClass);

            if (verb.ConfirmationPopup)
            {
                ExpansionIndicator.SetOnlyStyleClass(StyleClassVerbMenuConfirmationTexture);
                ExpansionIndicator.Visible = true;
            }

            var entManager = IoCManager.Resolve<IEntityManager>();

            if (verb.Icon == null && verb.IconEntity != null)
            {
                var spriteView = new SpriteView()
                {
                    OverrideDirection = Direction.South,
                    Sprite = entManager.GetComponentOrNull<ISpriteComponent>(verb.IconEntity.Value)
                };

                Icon.AddChild(spriteView);
                return;
            }

            Icon.AddChild(new TextureRect()
            {
                Texture = verb.Icon != null ? entManager.System<SpriteSystem>().Frame0(verb.Icon) : null,
                Stretch = TextureRect.StretchMode.KeepAspectCentered
            });
        }

        public VerbMenuElement(VerbCategory category, string styleClass) : base(category.Text)
        {
            Label.SetOnlyStyleClass(styleClass);

            Icon.AddChild(new TextureRect()
            {
                Texture = category.Icon != null ? IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SpriteSystem>().Frame0(category.Icon) : null,
                Stretch = TextureRect.StretchMode.KeepAspectCentered
            });
        }
    }
}
