﻿#nullable enable
using Content.Server.GameObjects.Components.Atmos;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces.GameObjects.Components;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.ComponentDependencies;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using static Content.Shared.GameObjects.Components.Inventory.EquipmentSlotDefines;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public sealed class MagbootsComponent : SharedMagbootsComponent, IUnequipped, IEquipped, IUse, IActivate
    {
        [ComponentDependency] private ItemComponent? _item = null;
        [ComponentDependency] private ItemActionsComponent? _itemActions = null;
        [ComponentDependency] private SpriteComponent? _sprite = null;
        private bool _on;

        [ViewVariables]
        public override bool On
        {
            get => _on;
            set
            {
                _on = value;

                UpdateContainer();
                _itemActions?.Toggle(ItemActionType.ToggleMagboots, On);
                if (_item != null)
                    _item.EquippedPrefix = On ? "on" : null;
                _sprite?.LayerSetState(0, On ? "icon-on" : "icon");
                OnChanged();
                Dirty();
            }
        }

        public void Toggle(IEntity user)
        {
            On = !On;
        }

        void IUnequipped.Unequipped(UnequippedEventArgs eventArgs)
        {
            if (On && eventArgs.Slot == Slots.SHOES)
            {
                if (eventArgs.User.TryGetComponent(out MovedByPressureComponent? movedByPressure))
                {
                    movedByPressure.Enabled = true;
                }

                if (eventArgs.User.TryGetComponent(out ServerAlertsComponent? alerts))
                {
                    alerts.ClearAlert(AlertType.Magboots);
                }
            }
        }

        void IEquipped.Equipped(EquippedEventArgs eventArgs)
        {
            UpdateContainer();
        }

        private void UpdateContainer()
        {
            if (!Owner.TryGetContainer(out var container))
                return;

            if (container.Owner.TryGetComponent(out InventoryComponent? inventoryComponent)
                && inventoryComponent.GetSlotItem(Slots.SHOES)?.Owner == Owner)
            {
                if (container.Owner.TryGetComponent(out MovedByPressureComponent? movedByPressure))
                {
                    movedByPressure.Enabled = false;
                }

                if (container.Owner.TryGetComponent(out ServerAlertsComponent? alerts))
                {
                    if (On)
                    {
                        alerts.ShowAlert(AlertType.Magboots);
                    }
                    else
                    {
                        alerts.ClearAlert(AlertType.Magboots);
                    }
                }
            }
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            Toggle(eventArgs.User);
            return true;
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            Toggle(eventArgs.User);
        }

        public override ComponentState GetComponentState()
        {
            return new MagbootsComponentState(On);
        }

        [UsedImplicitly]
        public sealed class ToggleMagbootsVerb : Verb<MagbootsComponent>
        {
            protected override void GetData(IEntity user, MagbootsComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("Toggle Magboots");
            }

            protected override void Activate(IEntity user, MagbootsComponent component)
            {
                component.Toggle(user);
            }
        }
    }

    [UsedImplicitly]
    public sealed class ToggleMagbootsAction : IToggleItemAction
    {
        void IExposeData.ExposeData(ObjectSerializer serializer) { }

        public bool DoToggleAction(ToggleItemActionEventArgs args)
        {
            if (!args.Item.TryGetComponent<MagbootsComponent>(out var magboots))
                return false;

            magboots.Toggle(args.Performer);
            return true;
        }
    }
}
