using System.Collections.Generic;
using Robust.Shared.GameObjects.Systems;
using Content.Shared.Prototypes.Cargo;
using Content.Shared.GameTicking;
using Content.Server.Cargo;
using Content.Server.GameObjects.Components.Cargo;

namespace Content.Server.GameObjects.EntitySystems
{
    public class CargoConsoleSystem : EntitySystem, IResettingEntitySystem
    {
        /// <summary>
        /// How much time to wait (in seconds) before increasing bank accounts balance.
        /// </summary>
        private const float Delay = 10f;
        /// <summary>
        /// How many points to give to every bank account every <see cref="Delay"/> seconds.
        /// </summary>
        private const int PointIncrease = 10;

        /// <summary>
        /// Keeps track of how much time has elapsed since last balance increase.
        /// </summary>
        private float _timer;
        /// <summary>
        /// Stores all bank accounts.
        /// </summary>
        private readonly Dictionary<int, CargoBankAccount> _accountsDict = new();

        private readonly Dictionary<int, CargoOrderDatabase> _databasesDict = new();
        /// <summary>
        /// Used to assign IDs to bank accounts. Incremental counter.
        /// </summary>
        private int _accountIndex = 0;
        /// <summary>
        /// Enumeration of all bank accounts.
        /// </summary>
        public IEnumerable<CargoBankAccount> BankAccounts => _accountsDict.Values;
        /// <summary>
        /// The station's bank account.
        /// </summary>
        public CargoBankAccount StationAccount => GetBankAccount(0);

        public CargoOrderDatabase StationOrderDatabase => GetOrderDatabase(0);

        public override void Initialize()
        {
            CreateBankAccount("Orbital Monitor IV Station", 100000);
            CreateOrderDatabase(0);
        }

        public override void Update(float frameTime)
        {
            _timer += frameTime;
            if (_timer < Delay)
            {
                return;
            }

            _timer -= Delay;
            foreach (var account in BankAccounts)
            {
                account.Balance += PointIncrease;
            }
        }

        public void Reset()
        {
            _accountsDict.Clear();
            _databasesDict.Clear();
            _timer = 0;
            _accountIndex = 0;
            Initialize();
        }

        /// <summary>
        /// Creates a new bank account.
        /// </summary>
        public void CreateBankAccount(string name, int balance)
        {
            var account = new CargoBankAccount(_accountIndex, name, balance);
            _accountsDict.Add(_accountIndex, account);
            _accountIndex += 1;
        }

        public void CreateOrderDatabase(int id)
        {
            _databasesDict.Add(id, new CargoOrderDatabase(id));
        }

        /// <summary>
        /// Returns the bank account associated with the given ID.
        /// </summary>
        public CargoBankAccount GetBankAccount(int id)
        {
            return _accountsDict[id];
        }

        public CargoOrderDatabase GetOrderDatabase(int id)
        {
            return _databasesDict[id];
        }

        /// <summary>
        /// Returns whether the account exists, eventually passing the account in the out parameter.
        /// </summary>
        public bool TryGetBankAccount(int id, out CargoBankAccount account)
        {
            return _accountsDict.TryGetValue(id, out account);
        }

        public bool TryGetOrderDatabase(int id, out CargoOrderDatabase database)
        {
            return _databasesDict.TryGetValue(id, out database);
        }

        /// <summary>
        /// Attempts to change the given account's balance.
        /// Returns false if there's no account associated with the given ID
        /// or if the balance would end up being negative.
        /// </summary>
        public bool ChangeBalance(int id, int amount)
        {
            if (!TryGetBankAccount(id, out var account))
            {
                return false;
            }

            if (account.Balance + amount < 0)
            {
                return false;
            }

            account.Balance += amount;
            return true;
        }

        public bool AddOrder(int id, string requester, string reason, string productId, int amount, int payingAccountId)
        {
            if (amount < 1 || !TryGetOrderDatabase(id, out var database))
                return false;
            database.AddOrder(requester, reason, productId, amount, payingAccountId);
            SyncComponentsWithId(id);
            return true;
        }

        public bool RemoveOrder(int id, int orderNumber)
        {
            if (!TryGetOrderDatabase(id, out var database))
                return false;
            database.RemoveOrder(orderNumber);
            SyncComponentsWithId(id);
            return true;
        }

        public bool ApproveOrder(int id, int orderNumber)
        {
            if (!TryGetOrderDatabase(id, out var database))
                return false;
            database.ApproveOrder(orderNumber);
            SyncComponentsWithId(id);
            return true;
        }

        public List<CargoOrderData> RemoveAndGetApprovedOrders(int id)
        {
            if (!TryGetOrderDatabase(id, out var database))
                return new List<CargoOrderData>();
            var approvedOrders = database.SpliceApproved();
            SyncComponentsWithId(id);
            return approvedOrders;
        }

        public (int CurrentCapacity, int MaxCapacity) GetCapacity(int id)
        {
            if (!TryGetOrderDatabase(id, out var database))
                return (0,0);
            return (database.CurrentOrderSize, database.MaxOrderSize);
        }

        private void SyncComponentsWithId(int id)
        {
            foreach (var comp in ComponentManager.EntityQuery<CargoOrderDatabaseComponent>())
            {
                if (!comp.ConnectedToDatabase || comp.Database.Id != id)
                    continue;
                comp.Dirty();
            }
        }
    }
}
