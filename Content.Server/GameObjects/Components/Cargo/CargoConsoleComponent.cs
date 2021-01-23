#nullable enable
using Content.Server.Cargo;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Cargo;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Prototypes.Cargo;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;
using System.Linq;

namespace Content.Server.GameObjects.Components.Cargo
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class CargoConsoleComponent : SharedCargoConsoleComponent, IActivate
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        [ViewVariables]
        public int Points = 1000;

        private CargoBankAccount? _bankAccount;

        [ViewVariables]
        public CargoBankAccount? BankAccount
        {
            get => _bankAccount;
            private set
            {
                if (_bankAccount == value)
                {
                    return;
                }

                if (_bankAccount != null)
                {
                    _bankAccount.OnBalanceChange -= UpdateUIState;
                }

                _bankAccount = value;

                if (value != null)
                {
                    value.OnBalanceChange += UpdateUIState;
                }

                UpdateUIState();
            }
        }

        private bool _requestOnly = false;

        private bool Powered => !Owner.TryGetComponent(out PowerReceiverComponent? receiver) || receiver.Powered;
        private CargoConsoleSystem _cargoConsoleSystem = default!;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(CargoConsoleUiKey.Key);

        public override void Initialize()
        {
            base.Initialize();

            Owner.EnsureComponentWarn(out GalacticMarketComponent _);
            Owner.EnsureComponentWarn(out CargoOrderDatabaseComponent _);

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnOnReceiveMessage;
            }

            _cargoConsoleSystem = EntitySystem.Get<CargoConsoleSystem>();
            BankAccount = _cargoConsoleSystem.StationAccount;
        }

        public override void OnRemove()
        {
            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage -= UserInterfaceOnOnReceiveMessage;
            }

            base.OnRemove();
        }

        /// <summary>
        ///    Reads data from YAML
        /// </summary>
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _requestOnly, "requestOnly", false);
        }

        private void UserInterfaceOnOnReceiveMessage(ServerBoundUserInterfaceMessage serverMsg)
        {
            if (!Owner.TryGetComponent(out CargoOrderDatabaseComponent? orders))
            {
                return;
            }

            var message = serverMsg.Message;
            if (!orders.ConnectedToDatabase)
                return;
            if (!Powered)
                return;
            switch (message)
            {
                case CargoConsoleAddOrderMessage msg:
                    {
                        if (msg.Amount <= 0 || _bankAccount == null)
                        {
                            break;
                        }

                        _cargoConsoleSystem.AddOrder(orders.Database.Id, msg.Requester, msg.Reason, msg.ProductId, msg.Amount, _bankAccount.Id);
                        break;
                    }
                case CargoConsoleRemoveOrderMessage msg:
                    {
                        _cargoConsoleSystem.RemoveOrder(orders.Database.Id, msg.OrderNumber);
                        break;
                    }
                case CargoConsoleApproveOrderMessage msg:
                    {
                        if (_requestOnly ||
                            !orders.Database.TryGetOrder(msg.OrderNumber, out var order) ||
                            _bankAccount == null)
                        {
                            break;
                        }

                        PrototypeManager.TryIndex(order.ProductId, out CargoProductPrototype product);
                        if (product == null!)
                            break;
                        var capacity = _cargoConsoleSystem.GetCapacity(orders.Database.Id);
                        if (capacity.CurrentCapacity == capacity.MaxCapacity)
                            break;
                        if (!_cargoConsoleSystem.ChangeBalance(_bankAccount.Id, (-product.PointCost) * order.Amount))
                            break;
                        _cargoConsoleSystem.ApproveOrder(orders.Database.Id, msg.OrderNumber);
                        UpdateUIState();
                        break;
                    }
                case CargoConsoleShuttleMessage _:
                    {
                        //var approvedOrders = _cargoOrderDataManager.RemoveAndGetApprovedFrom(orders.Database);
                        //orders.Database.ClearOrderCapacity();

                        // TODO replace with shuttle code
                        // TEMPORARY loop for spawning stuff on telepad (looks for a telepad adjacent to the console)
                        IEntity? cargoTelepad = null;
                        var indices = Owner.Transform.Coordinates.ToVector2i(Owner.EntityManager, _mapManager);
                        var offsets = new Vector2i[] { new Vector2i(0, 1), new Vector2i(1, 1), new Vector2i(1, 0), new Vector2i(1, -1),
                                                       new Vector2i(0, -1), new Vector2i(-1, -1), new Vector2i(-1, 0), new Vector2i(-1, 1), };
                        var adjacentEntities = new List<IEnumerable<IEntity>>(); //Probably better than IEnumerable.concat
                        foreach (var offset in offsets)
                        {
                            adjacentEntities.Add((indices+offset).GetEntitiesInTileFast(Owner.Transform.GridID));
                        }

                        foreach (var enumerator in adjacentEntities)
                        {
                            foreach (IEntity entity in enumerator)
                            {
                                if (entity.HasComponent<CargoTelepadComponent>() && entity.TryGetComponent<PowerReceiverComponent>(out var powerReceiver) && powerReceiver.Powered)
                                {
                                    cargoTelepad = entity;
                                    break;
                                }
                            }
                        }
                        if (cargoTelepad != null)
                        {
                            if (cargoTelepad.TryGetComponent<CargoTelepadComponent>(out var telepadComponent))
                            {
                                var approvedOrders = _cargoConsoleSystem.RemoveAndGetApprovedOrders(orders.Database.Id);
                                orders.Database.ClearOrderCapacity();
                                foreach (var order in approvedOrders)
                                {
                                    if (!PrototypeManager.TryIndex(order.ProductId, out CargoProductPrototype product))
                                        continue;
                                    for (var i = 0; i < order.Amount; i++)
                                    {
                                        telepadComponent.QueueTeleport(product);
                                    }
                                }
                            }
                        }
                        break;
                    }
            }
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent? actor))
            {
                return;
            }
            if (!Powered)
                return;

            UserInterface?.Open(actor.playerSession);
        }

        private void UpdateUIState()
        {
            if (_bankAccount == null || !Owner.IsValid())
            {
                return;
            }

            var id = _bankAccount.Id;
            var name = _bankAccount.Name;
            var balance = _bankAccount.Balance;
            var capacity = _cargoConsoleSystem.GetCapacity(id);
            UserInterface?.SetState(new CargoConsoleInterfaceState(_requestOnly, id, name, balance, capacity));
        }
    }
}
