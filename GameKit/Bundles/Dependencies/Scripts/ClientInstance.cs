using FishNet;
using FishNet.Managing;
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
        public delegate void ServerChangeDel(ClientInstance instance, ClientInstanceState state);
        /// <summary>
        /// Registers to OnServerChange and invokes immediately for all ClientInstances.
        /// </summary>
        public static void OnServerChangeInvoke(ServerChangeDel del)
        {
            OnServerChange += del;

            if (IsAnyActive(false))
            {
                foreach (ClientInstance item in Instances)
                {
                    if (item.IsServer)
                        del?.Invoke(item, ClientInstanceState.PreInitialize);
                }
                foreach (ClientInstance item in Instances)
                {
                    if (item.IsServer)
                        del?.Invoke(item, ClientInstanceState.PostInitialize);
                }
            }
        }
        /// <summary>
        /// Called when OnStartClient or OnStopClient occurs.
        /// </summary>
        public static event ClientChangeDel OnClientChange;
        public delegate void ClientChangeDel(ClientInstance instance, ClientInstanceState state);
        /// <summary>
        /// Registers to OnClientChange and invokes immediately for all ClientInstances.
        /// </summary>
        public static void OnClientChangeInvoke(ClientChangeDel del)
        {
            OnClientChange += del;

            if (IsAnyActive(false))
            {
                foreach (ClientInstance item in Instances)
                {
                    if (item.IsClient)
                        del?.Invoke(item, ClientInstanceState.PreInitialize);
                }
                foreach (ClientInstance item in Instances)
                {
                    if (item.IsClient)
                        del?.Invoke(item, ClientInstanceState.PostInitialize);
                }
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
            OnServerChange?.Invoke(this, ClientInstanceState.PreInitialize);
            OnServerChange?.Invoke(this, ClientInstanceState.PostInitialize);
        }

        public override void OnStartClient()
        {
            OnClientChange?.Invoke(this, ClientInstanceState.PreInitialize);
            if (base.IsOwner)
                Instance = this;
            OnClientChange?.Invoke(this, ClientInstanceState.PostInitialize);
        }

        public override void OnStopNetwork()
        {
            Instances.Remove(this);
        }
        public override void OnStopClient()
        {
            OnClientChange?.Invoke(this, ClientInstanceState.PreDeinitialize);
            if (base.IsOwner)
                Instance = null;
            OnClientChange?.Invoke(this, ClientInstanceState.PostDeinitialize);
        }

        public override void OnStopServer()
        {
            OnServerChange?.Invoke(this, ClientInstanceState.PreDeinitialize);
            OnServerChange?.Invoke(this, ClientInstanceState.PostDeinitialize);
        }

        /// <summary>
        /// Returns if any NetworkManager is active as server or client.
        /// </summary>
        /// <param name="asServer">True if checking for active server, false for active client.</param>
        /// <returns></returns>
        private static bool IsAnyActive(bool asServer)
        {
            IReadOnlyCollection<NetworkManager> instances = NetworkManager.Instances;
            //As server.
            if (asServer)
            {
                foreach (NetworkManager manager in instances)
                    if (manager.IsServer)
                        return true;
            }
            //As client.
            else
            {
                foreach (NetworkManager manager in instances)
                    if (manager.IsClient)
                        return true;
            }

            //Fall through for either.
            return false;
        }
    }


}