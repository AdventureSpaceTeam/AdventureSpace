﻿using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Items;
using Robust.Client.Graphics;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Renderable;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects
{
    [RegisterComponent]
    public class ItemComponent : Component
    {
        public override string Name => "Item";
        public override uint? NetID => ContentNetIDs.ITEM;

        [ViewVariables] protected ResourcePath RsiPath;

        private string _equippedPrefix;

        [ViewVariables(VVAccess.ReadWrite)]
        public string EquippedPrefix
        {
            get => _equippedPrefix;
            set
            {
                _equippedPrefix = value;
                if (!ContainerHelpers.TryGetContainer(Owner, out IContainer container)) return;
                if(container.Owner.TryGetComponent(out HandsComponent hands))
                    hands.RefreshInHands();
            }
        }

        public (RSI rsi, RSI.StateId stateId)? GetInHandStateInfo(string hand)
        {
            if (RsiPath == null)
            {
                return null;
            }

            var rsi = GetRSI();
            var stateId = EquippedPrefix != null ? $"{EquippedPrefix}-inhand-{hand}" : $"inhand-{hand}";
            if (rsi.TryGetState(stateId, out _))
            {
                return (rsi, stateId);
            }

            return null;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataFieldCached(ref RsiPath, "sprite", null);
            serializer.DataFieldCached(ref _equippedPrefix, "HeldPrefix", null);
        }

        protected RSI GetRSI()
        {
            var resourceCache = IoCManager.Resolve<IResourceCache>();
            return resourceCache.GetResource<RSIResource>(SharedSpriteComponent.TextureRoot / RsiPath).RSI;
        }

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            if(curState == null)
                return;

            var itemComponentState = (ItemComponentState)curState;
            EquippedPrefix = itemComponentState.EquippedPrefix;
        }
    }
}
