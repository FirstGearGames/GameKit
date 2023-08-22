using FishNet.Object;
using GameKit.Crafting;
using GameKit.Inventories;
using System.Collections.Generic;

namespace GameKit.Bundles
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
        /// Registers to OnServerChange and invokes immediately for all ClientInstances.
        /// </summary>
        public static void OnServerChangeInvoke(ServerChangeDel del)
        {
            OnServerChange += del;

            foreach (ClientInstance item in Instances)
            {
                if (item.IsServer)
                    del?.Invoke(item, true);
            }
        }
        /// <summary>
        /// Called when OnStartClient or OnStopClient occurs.
        /// </summary>
        public static event ClientChangeDel OnClientChange;
        public delegate void ClientChangeDel(ClientInstance instance, bool started);
        /// <summary>
        /// Registers to OnClientChange and invokes immediately for all ClientInstances.
        /// </summary>
        public static void OnClientChangeInvoke(ClientChangeDel del)
        {
            OnClientChange += del;
            
            foreach (ClientInstance item in Instances)
            {
                if (item.IsClient)
                    del?.Invoke(item, true);
            }
        }
        /// <summary>
        /// Instance for the local client.
        /// </summary>
        public static ClientInstance Instance { get; private set; }
        /// <summary>
        /// All instances of this class.
        /// </summary>
        public static HashSet<ClientInstance> Instances { get; private set; } = new HashSet<ClientInstance>();
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

        public override void OnStartNetwork()
        {
            Instances.Add(this);
        }

        public override void OnStartServer()
        {
            OnServerChange?.Invoke(this, true);
        }

        public override void OnStartClient()
        {
            if (base.IsOwner)
                Instance = this;
            OnClientChange?.Invoke(this, true);
        }

        public override void OnStopNetwork()
        {
            Instances.Remove(this);
        }
        public override void OnStopClient()
        {
            OnClientChange?.Invoke(this, false);
            if (base.IsOwner)
                Instance = null;
        }

        public override void OnStopServer()
        {
            OnServerChange?.Invoke(this, false);
        }
    }


}