using FishNet.Object;
using FishNet.Connection;
using GameKit.Core.Inventories.Bags;
using System.Collections.Generic;
using UnityEngine;
using GameKit.Dependencies.Utilities;
using GameKit.Core.Resources;
using GameKit.Core.Databases.LiteDb;

namespace GameKit.Core.Inventories
{

    public partial class InventoryBase : NetworkBehaviour
    {
        public override void OnSpawnServer(NetworkConnection connection)
        {
            LoadInventoryFromDatabase_Server(connection);
        }

        /// <summary>
        /// Called on the server when this spawns for a client.
        /// </summary>
        [Server]
        private void LoadInventoryFromDatabase_Server(NetworkConnection c, bool sendToClient = true)
        {
            SerializableInventoryDb inventoryDb = InventoryDbService.Instance.GetInventory((uint)c.ClientId, CategoryId);
            if (inventoryDb.IsDefault())
            {
                for (int i = 0; i < _defaultBags.Length; i++)
                {
                    BagData item = _defaultBags[i];
                    BagData b = _bagManager.GetBagData(item.UniqueId);
                    uint baseIndex = (InventoryConsts.UNSET_BAG_ID + 1);
                    AddBag(b, baseIndex + (uint)i, false);
                }

                inventoryDb = SaveAllInventory_Server();
                Debug.Log($"Inventory did not exist for {c.ToString()}. They were created with default bags. {_defaultBags.Length} bags were added.");
            }

            List<SerializableActiveBag> baggedUnsorted = inventoryDb.ActiveBags;
            List<SerializableResourceQuantity> hiddenUnsorted = inventoryDb.HiddenResources;

            //Add bags and slots on server.                    
            ApplyInventory_Server(baggedUnsorted, hiddenUnsorted);

            if (sendToClient)
            {
                List<SerializableActiveBag> baggedSorted = InventoryDbService.Instance.GetSortedInventory((uint)c.ClientId);
                TgtApplyInventory(base.Owner, baggedUnsorted, hiddenUnsorted, baggedSorted);
            }

            inventoryDb.ResetState();
        }

        /// <summary>
        /// Saves the clients inventory loadout on the server.
        /// </summary>
        [Server]
        private void SaveBaggedInventorySorted_Server(List<SerializableActiveBag> sabs)
        {
            InventoryDbService.Instance.SetSortedInventory((uint)base.Owner.ClientId, this, sabs);
        }

        /// <summary>
        /// Requests that the server saves sorted bags for the client.
        /// </summary>
        /// <param name="sabs"></param>
        [ServerRpc]
        protected void SvrSaveBaggedSorted(List<SerializableActiveBag> sabs)
        {
            SaveBaggedInventorySorted_Server(sabs);
        }

        /// <summary>
        /// Saves current inventory resource quantities to the database, returning the InventoryDb created.
        /// </summary>
        [Server]
        private SerializableInventoryDb SaveAllInventory_Server()
        {
            //TODO: there needs to be a diff save option.
            List<ActiveBag> activeBags = ResettableCollectionCaches<ActiveBag>.RetrieveList();
            ActiveBags.ValuesToList(ref activeBags);
            List<SerializableActiveBag> baggedUnsorted = activeBags.ToSerializable();
            List<SerializableResourceQuantity> hiddenUnsorted = CollectionCaches<SerializableResourceQuantity>.RetrieveList();
            foreach (KeyValuePair<uint, int> item in HiddenResources)
                hiddenUnsorted.Add(new SerializableResourceQuantity(item.Key, item.Value));

            SerializableInventoryDb result = new SerializableInventoryDb(baggedUnsorted, hiddenUnsorted);
            InventoryDbService.Instance.SetInventory((uint)base.Owner.ClientId, this, result);

            return result;
        }


        /// <summary>
        /// Uses serializable data to set inventory.
        /// </summary>
        private void ApplyInventory_Server(List<SerializableActiveBag> baggedResources, List<SerializableResourceQuantity> hiddenResources)
        {
            ActiveBags.Clear();
            HiddenResources.Clear();

            foreach (SerializableActiveBag item in baggedResources)
            {
                ActiveBag ab = item.ToNative(this, _bagManager);
                AddBag(ab, false);
            }

            foreach (SerializableResourceQuantity item in hiddenResources)
                item.ToNativeReplace(HiddenResources);

            /* This builds a cache of resources currently in the inventory.
             * Since ActiveBags were set without allowing rebuild to save perf
             * it's called here after all bags are added. */
            RebuildBaggedResources();
        }
    }

}
