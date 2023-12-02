using FishNet.Managing;
using FishNet.Object;
using GameKit.Core.Crafting;
using GameKit.Core.Inventories;
using System.Collections.Generic;

namespace GameKit.Core.Dependencies
{

    public class ClientInstance : NetworkBehaviour
    {
        #region Public.
        /// <summary>
        /// Called when OnStartServer or OnStopServer occurs.
        /// </summary>
        public static event ClientInstanceChangeDel OnClientInstanceChange;
        public delegate void ClientInstanceChangeDel(ClientInstance instance, ClientInstanceState state, bool asServer);
        /// <summary>
        /// Registers a delegate to call each time a ClientInstance is started or stopped.
        /// </summary>
        /// <param name="asServer">True if to invoke for the server, false to invoke for the client.</param>
        public static void OnClientInstanceChangeInvoke(ClientInstanceChangeDel del, bool asServer)
        {
            OnClientInstanceChange += del;
            //Side is not active so just register delegate but don't invoke currently spawned.
            if (!IsAnyActive(asServer))
                return;

            if (IsAnyActive(asServer))
            {
                foreach (ClientInstance item in Instances)
                {
                    if (item.IsSpawned)
                        del?.Invoke(item, ClientInstanceState.PreInitialize, asServer);
                }
                foreach (ClientInstance item in Instances)
                {
                    if (item.IsSpawned)
                        del?.Invoke(item, ClientInstanceState.PostInitialize, asServer);
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
            OnClientInstanceChange?.Invoke(this, ClientInstanceState.PreInitialize, true);
            OnClientInstanceChange?.Invoke(this, ClientInstanceState.PostInitialize, true);
        }

        public override void OnStartClient()
        {
            OnClientInstanceChange?.Invoke(this, ClientInstanceState.PreInitialize, false);
            if (base.IsOwner)
                Instance = this;
            OnClientInstanceChange?.Invoke(this, ClientInstanceState.PostInitialize, false);
        }

        public override void OnStopNetwork()
        {
            Instances.Remove(this);
        }
        public override void OnStopClient()
        {
            OnClientInstanceChange?.Invoke(this, ClientInstanceState.PreDeinitialize, false);
            if (base.IsOwner)
                Instance = null;
            OnClientInstanceChange?.Invoke(this, ClientInstanceState.PostDeinitialize, false);
        }

        public override void OnStopServer()
        {
            OnClientInstanceChange?.Invoke(this, ClientInstanceState.PreDeinitialize, true);
            OnClientInstanceChange?.Invoke(this, ClientInstanceState.PostDeinitialize, true);
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
                    if (manager.IsServerStarted)
                        return true;
            }
            //As client.
            else
            {
                foreach (NetworkManager manager in instances)
                    if (manager.IsClientStarted)
                        return true;
            }

            //Fall through for either.
            return false;
        }
    }


}