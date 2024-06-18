using FishNet.Object;
using FishNet.Connection;
using GameKit.Core.Crafting;
using System.IO;
using GameKit.Core.Inventories.Bags;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using GameKit.Dependencies.Utilities;
using GameKit.Core.Resources;
using GameKit.Core.Databases.LiteDb;

namespace GameKit.Core.Inventories
{

    public partial class Inventory : NetworkBehaviour
    {

        public override void OnStartServer()
        {
            Crafter crafter = GetComponent<Crafter>();
            crafter.OnCraftingResult += Crafter_OnCraftingResult;
        }

        public override void OnSpawnServer(NetworkConnection connection)
        {
            LoadInventoryFromDatabase(connection);
        }

        /// <summary>
        /// Called on the server when this spawns for a client.
        /// </summary>
        [Server]
        private void LoadInventoryFromDatabase(NetworkConnection c, bool sendToClient = true)
        {
            SerializableInventoryDb inventoryDb = InventoryDbService.Instance.GetInventory((uint)c.ClientId);
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
                Debug.Log($"Inventory did not exist for {c.ToString()}. They were created with default bags.");
            }

            //Send loaded to client.
            //try
            //{
            //Load as text and let client deserialize.
            //string baggedUnsortedTxt = File.ReadAllText(baggedUnsortedPath);
            //string hiddenUnsortedTxt = File.ReadAllText(hiddenUnsortedPath);

            //List<SerializableActiveBag> baggedUnsorted = JsonConvert.DeserializeObject<List<SerializableActiveBag>>(baggedUnsortedTxt);
            //List<SerializableResourceQuantity> hiddenUnsorted = JsonConvert.DeserializeObject<List<SerializableResourceQuantity>>(hiddenUnsortedTxt);

            List<SerializableActiveBag> baggedUnsorted = inventoryDb.ActiveBags;
            List<SerializableResourceQuantity> hiddenUnsorted = inventoryDb.HiddenResources;

            //Add bags and slots on server.                    
            ApplyInventory_Server(baggedUnsorted, hiddenUnsorted);

            if (sendToClient)
            {
                string baggedSortedPath = Path.Combine(Application.dataPath, INVENTORY_BAGGED_SORTED_FILENAME);
                List<SerializableActiveBag> baggedSorted;
                if (File.Exists(baggedSortedPath))
                {
                    string baggedSortedTxt = File.ReadAllText(baggedSortedPath);
                    baggedSorted = JsonConvert.DeserializeObject<List<SerializableActiveBag>>(baggedSortedTxt);
                }
                else
                {
                    baggedSorted = new();
                }

                TgtApplyInventory(base.Owner, baggedUnsorted, hiddenUnsorted, baggedSorted);
            }

            //For GC management.
            inventoryDb.ResetState();
            //}
            //catch
            //{
            //    Debug.LogError($"Failed to load json files for resources or loadout.");
            //}
        }

        /// <summary>
        /// Saves the clients inventory loadout on the server.
        /// </summary>
        [Server]
        private void SaveBaggedInventorySorted_Server(List<SerializableActiveBag> sabs)
        {
            return;
            //todo: save to a database. throttle save frequency. optimize by only sending changed bags.
            string s = JsonConvert.SerializeObject(sabs);
            string path = Path.Combine(Application.dataPath, INVENTORY_BAGGED_SORTED_FILENAME);
            try
            {
                File.WriteAllText(path, s);
            }
            catch { }
        }

        /// <summary>
        /// Requests that the server saves sorted bags for the client.
        /// </summary>
        /// <param name="sabs"></param>
        [ServerRpc]
        private void SvrSaveBaggedSorted(List<SerializableActiveBag> sabs)
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
            InventoryDbService.Instance.SetInventory((uint)base.Owner.ClientId, result);
            ////TODO: Use a database rather than json file. save only diff when resources are added.
            //string baggedPath = Path.Combine(Application.dataPath, INVENTORY_BAGGED_UNSORTED_FILENAME);
            //string hiddenPath = Path.Combine(Application.dataPath, INVENTORY_HIDDEN_UNSORTED_FILENAME);

            //try
            //{
            //    string result;

            //    result = JsonConvert.SerializeObject(baggedUnsorted, Formatting.Indented);
            //    File.WriteAllText(baggedPath, result);

            //    result = JsonConvert.SerializeObject(hiddenUnsorted, Formatting.Indented);
            //    File.WriteAllText(hiddenPath, result);
            //}
            //catch { }

            //CollectionCaches<SerializableResourceQuantity>.Store(hiddenUnsorted);
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
                ActiveBag ab = item.ToNative(_bagManager);
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