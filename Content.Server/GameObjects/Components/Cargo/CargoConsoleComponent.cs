﻿using Content.Server.Cargo;
using Content.Server.GameObjects.Components.Power;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Cargo;
using Content.Shared.Prototypes.Cargo;
using JetBrains.Annotations;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Cargo
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class CargoConsoleComponent : SharedCargoConsoleComponent, IActivate
    {
#pragma warning disable 649
        [Dependency] private readonly ICargoOrderDataManager _cargoOrderDataManager;
#pragma warning restore 649

        [ViewVariables]
        public int Points = 1000;

        private BoundUserInterface _userInterface;

        [ViewVariables]
        public GalacticMarketComponent Market { get; private set; }
        [ViewVariables]
        public CargoOrderDatabaseComponent Orders { get; private set; }

        private CargoBankAccount _bankAccount;

        [ViewVariables]
        [CanBeNull]
        public CargoBankAccount BankAccount
        {
            get => _bankAccount;
            private set
            {
                if (_bankAccount == value)
                    return;
                if (_bankAccount != null)
                {
                    _bankAccount.OnBalanceChange -= OnBankAccountChange;
                }

                _bankAccount = value;
                if (value != null)
                {
                    _bankAccount.OnBalanceChange += OnBankAccountChange;
                }

                OnBankAccountChange();
            }
        }

        private void OnBankAccountChange()
        {
            SetState(_bankAccount.Id, _bankAccount.Name, _bankAccount.Balance);
        }

        private bool _requestOnly = false;

        private PowerDeviceComponent _powerDevice;
        private bool Powered => _powerDevice.Powered;
        private CargoConsoleSystem _cargoConsoleSystem;

        public override void Initialize()
        {
            base.Initialize();
            Market = Owner.GetComponent<GalacticMarketComponent>();
            Orders = Owner.GetComponent<CargoOrderDatabaseComponent>();
            _userInterface = Owner.GetComponent<ServerUserInterfaceComponent>().GetBoundUserInterface(CargoConsoleUiKey.Key);
            _userInterface.OnReceiveMessage += UserInterfaceOnOnReceiveMessage;
            _powerDevice = Owner.GetComponent<PowerDeviceComponent>();
            _cargoConsoleSystem = EntitySystem.Get<CargoConsoleSystem>();
            BankAccount = _cargoConsoleSystem.StationAccount;
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
            var message = serverMsg.Message;
            if (!Orders.ConnectedToDatabase)
                return;
            if (!Powered)
                return;
            switch (message)
            {
                case CargoConsoleAddOrderMessage msg:
                {
                    if (msg.Amount <= 0)
                        break;
                    _cargoOrderDataManager.AddOrder(Orders.Database.Id, msg.Requester, msg.Reason, msg.ProductId, msg.Amount, _bankAccount.Id);
                    break;
                }
                case CargoConsoleRemoveOrderMessage msg:
                {
                    _cargoOrderDataManager.RemoveOrder(Orders.Database.Id, msg.OrderNumber);
                    break;
                }
                case CargoConsoleApproveOrderMessage msg:
                {
                    if (_requestOnly ||
                        !Orders.Database.TryGetOrder(msg.OrderNumber, out var order))
                        break;
                    _prototypeManager.TryIndex(order.ProductId, out CargoProductPrototype product);
                    if (product == null)
                        break;
                    if (!_cargoConsoleSystem.ChangeBalance(_bankAccount.Id, (-product.PointCost) * order.Amount))
                        break;
                    _cargoOrderDataManager.ApproveOrder(Orders.Database.Id, msg.OrderNumber);
                    break;
                }
                case CargoConsoleShuttleMessage _:
                {
                    var approvedOrders = _cargoOrderDataManager.RemoveAndGetApprovedFrom(Orders.Database);
                    // TODO replace with shuttle code

                    // TEMPORARY loop for spawning stuff on top of console
                    foreach (var order in approvedOrders)
                    {
                        if (!_prototypeManager.TryIndex(order.ProductId, out CargoProductPrototype product))
                            continue;
                        for (var i = 0; i < order.Amount; i++)
                        {
                            Owner.EntityManager.SpawnEntity(product.Product, Owner.Transform.GridPosition);
                        }
                    }
                    break;
                }
            }
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent actor))
            {
                return;
            }
            if (!Powered)
                return;

            _userInterface.Open(actor.playerSession);
        }

        /// <summary>
        ///    Sync bank account information
        /// </summary>
        public void SetState(int id, string name, int balance)
        {
            _userInterface.SetState(new CargoConsoleInterfaceState(_requestOnly, id, name, balance));
        }
    }
}
