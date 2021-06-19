#nullable enable
using System.Threading.Tasks;
using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Server.Paper;
using Content.Server.Standing;
using Content.Server.Storage.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Body.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Morgue;
using Content.Shared.Notification.Managers;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Morgue.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(EntityStorageComponent))]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IStorageComponent))]
    public class BodyBagEntityStorageComponent : EntityStorageComponent, IExamine, IInteractUsing
    {
        public override string Name => "BodyBagEntityStorage";

        [ViewVariables]
        [ComponentDependency] private readonly AppearanceComponent? _appearance = null;

        [ViewVariables] public ContainerSlot? LabelContainer { get; private set; }

        public override void Initialize()
        {
            base.Initialize();
            _appearance?.SetData(BodyBagVisuals.Label, false);
            LabelContainer = ContainerHelpers.EnsureContainer<ContainerSlot>(Owner, "body_bag_label", out _);
        }

        protected override bool AddToContents(IEntity entity)
        {
            if (entity.HasComponent<SharedBodyComponent>() && !EntitySystem.Get<StandingStateSystem>().IsDown(entity)) return false;
            return base.AddToContents(entity);
        }

        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (inDetailsRange)
            {
                if (LabelContainer?.ContainedEntity != null && LabelContainer.ContainedEntity.TryGetComponent<PaperComponent>(out var paper))
                {
                    message.AddText(Loc.GetString("The label reads: {0}", paper.Content));
                }
            }
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (LabelContainer == null) return false;

            if (LabelContainer.ContainedEntity != null)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("There's already a label attached."));
                return false;
            }

            var handsComponent = eventArgs.User.GetComponent<IHandsComponent>();
            if (!handsComponent.Drop(eventArgs.Using, LabelContainer))
            {
                return false;
            }

            _appearance?.SetData(BodyBagVisuals.Label, true);

            Owner.PopupMessage(eventArgs.User, Loc.GetString("You attach {0:theName} to the body bag.", eventArgs.Using));
            return true;
        }

        public void RemoveLabel(IEntity user)
        {
            if (LabelContainer == null) return;

            var ent = LabelContainer.ContainedEntity;
            if(ent is null)
                return;

            if (user.TryGetComponent(out HandsComponent? hands))
            {
                hands.PutInHandOrDrop(ent.GetComponent<ItemComponent>());
                _appearance?.SetData(BodyBagVisuals.Label, false);
            }
            else if (LabelContainer.Remove(ent))
            {
                ent.Transform.Coordinates = Owner.Transform.Coordinates;
                _appearance?.SetData(BodyBagVisuals.Label, false);
            }
        }


        [Verb]
        private sealed class RemoveLabelVerb : Verb<BodyBagEntityStorageComponent>
        {
            protected override void GetData(IEntity user, BodyBagEntityStorageComponent component, VerbData data)
            {
                if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user) || component.LabelContainer?.ContainedEntity == null)
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("Remove label");
            }

            /// <inheritdoc />
            protected override void Activate(IEntity user, BodyBagEntityStorageComponent component)
            {
                component.RemoveLabel(user);
            }
        }
    }
}
