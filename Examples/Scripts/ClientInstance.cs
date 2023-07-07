using GameKit.Inventories;
using GameKit.Crafting;
using FishNet.Object;

namespace GameKit.Examples
{

    public class ClientInstance : NetworkBehaviour
    {
        #region Public.
        /// <summary>
        /// Called when OnStartServer or OnStopServer occurs.
        /// </summary>
        public static event ServerChangeDel OnServerChange;
        public delegate void ServerChangeDel(ClientInstance instance, bool started);
        /// <summary>
        /// Called when OnStartClient or OnStopClient occurs.
        /// </summary>
        public static event ClientChangedDel OnClientChange;
        public delegate void ClientChangedDel(ClientInstance instance, bool started);
        /// <summary>
        /// Instance for the local client.
        /// </summary>
        public static ClientInstance Instance { get; private set; }
        /// <summary>
        /// Inventory for this cliet.
        /// </summary>
        public Inventory Inventory { get; private set; }
        /// <summary>
        /// Crafter for this cliet.
        /// </summary>
        public Crafter Crafter { get; private set; }
        #endregion

        private void Awake()
        {
            Inventory = GetComponent<Inventory>();
            Crafter = GetComponent<Crafter>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            OnServerChange?.Invoke(this, true);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (base.IsOwner)
                Instance = this;
            OnClientChange?.Invoke(this, true);
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            OnClientChange?.Invoke(this, false);
            if (base.IsOwner)
                Instance = null;
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            OnServerChange?.Invoke(this, false);
        }
    }


}