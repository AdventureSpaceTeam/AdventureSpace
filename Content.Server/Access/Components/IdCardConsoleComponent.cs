using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Access.Systems;
using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Shared.Access;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Access.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedIdCardConsoleComponent))]
    public sealed class IdCardConsoleComponent : SharedIdCardConsoleComponent
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(IdCardConsoleUiKey.Key);
        [ViewVariables] private bool Powered => !IoCManager.Resolve<IEntityManager>().TryGetComponent(Owner, out ApcPowerReceiverComponent? receiver) || receiver.Powered;

        protected override void Initialize()
        {
            base.Initialize();

            Owner.EnsureComponentWarn<AccessReader>();
            Owner.EnsureComponentWarn<ServerUserInterfaceComponent>();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += OnUiReceiveMessage;
            }
        }

        private void OnUiReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            if (obj.Session.AttachedEntity == null)
            {
                return;
            }

            switch (obj.Message)
            {
                case IdButtonPressedMessage msg:
                    switch (msg.Button)
                    {
                        case UiButton.PrivilegedId:
                            HandleIdButton(obj.Session.AttachedEntity, PrivilegedIdSlot);
                            break;
                        case UiButton.TargetId:
                            HandleIdButton(obj.Session.AttachedEntity, TargetIdSlot);
                            break;
                    }
                    break;
                case WriteToTargetIdMessage msg:
                    TryWriteToTargetId(msg.FullName, msg.JobTitle, msg.AccessList);
                    UpdateUserInterface();
                    break;
            }
        }

        /// <summary>
        /// Returns true if there is an ID in <see cref="PrivilegedIdSlot"/> and said ID satisfies the requirements of <see cref="AccessReader"/>.
        /// </summary>
        private bool PrivilegedIdIsAuthorized()
        {
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(Owner, out AccessReader? reader))
            {
                return true;
            }

            var privilegedIdEntity = PrivilegedIdSlot.Item;
            var accessSystem = EntitySystem.Get<AccessReaderSystem>();
            return privilegedIdEntity != null && accessSystem.IsAllowed(reader, privilegedIdEntity);
        }

        /// <summary>
        /// Called when the "Submit" button in the UI gets pressed.
        /// Writes data passed from the UI into the ID stored in <see cref="TargetIdSlot"/>, if present.
        /// </summary>
        private void TryWriteToTargetId(string newFullName, string newJobTitle, List<string> newAccessList)
        {
            var targetIdEntity = TargetIdSlot.Item;
            if (targetIdEntity == null || !PrivilegedIdIsAuthorized())
                return;

            var cardSystem = EntitySystem.Get<IdCardSystem>();
            cardSystem.TryChangeFullName(targetIdEntity, newFullName);
            cardSystem.TryChangeJobTitle(targetIdEntity, newJobTitle);

            if (!newAccessList.TrueForAll(x => _prototypeManager.HasIndex<AccessLevelPrototype>(x)))
            {
                Logger.Warning("Tried to write unknown access tag.");
                return;
            }

            var accessSystem = EntitySystem.Get<AccessSystem>();
            accessSystem.TrySetTags(targetIdEntity, newAccessList);
        }

        /// <summary>
        /// Called when one of the insert/remove ID buttons gets pressed.
        /// </summary>
        private void HandleIdButton(IEntity user, ItemSlot slot)
        {
            if (slot.HasItem)
                EntitySystem.Get<ItemSlotsSystem>().TryEjectToHands(((IComponent) this).Owner, slot, user);
            else
                EntitySystem.Get<ItemSlotsSystem>().TryInsertFromHand(((IComponent) this).Owner, slot, user);
        }

        public void UpdateUserInterface()
        {
            var targetIdEntity = TargetIdSlot.Item;
            IdCardConsoleBoundUserInterfaceState newState;
            // this could be prettier
            if (targetIdEntity == null)
            {
                IEntity? tempQualifier = PrivilegedIdSlot.Item;
                newState = new IdCardConsoleBoundUserInterfaceState(
                    PrivilegedIdSlot.HasItem,
                    PrivilegedIdIsAuthorized(),
                    false,
                    null,
                    null,
                    null,
                    (tempQualifier != null ? IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(tempQualifier).EntityName : null) ?? string.Empty,
                    string.Empty);
            }
            else
            {
                var targetIdComponent = IoCManager.Resolve<IEntityManager>().GetComponent<IdCardComponent>(targetIdEntity);
                var targetAccessComponent = IoCManager.Resolve<IEntityManager>().GetComponent<AccessComponent>(targetIdEntity);
                var name = string.Empty;
                if(PrivilegedIdSlot.Item != null)
                    name = IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(PrivilegedIdSlot.Item).EntityName;
                newState = new IdCardConsoleBoundUserInterfaceState(
                    PrivilegedIdSlot.HasItem,
                    PrivilegedIdIsAuthorized(),
                    true,
                    targetIdComponent.FullName,
                    targetIdComponent.JobTitle,
                    targetAccessComponent.Tags.ToArray(),
                    name,
                    IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(targetIdEntity).EntityName);
            }
            UserInterface?.SetState(newState);
        }
    }
}
