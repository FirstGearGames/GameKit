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
            //TODO: this should be using a database, not locallly saved file.
            string baggedUnsortedPath = Path.Combine(Application.dataPath, INVENTORY_BAGGED_UNSORTED_FILENAME);
            string hiddenUnsortedPath = Path.Combine(Application.dataPath, INVENTORY_HIDDEN_UNSORTED_FILENAME);
            if (!File.Exists(baggedUnsortedPath) || !File.Exists(hiddenUnsortedPath))
            {
                foreach (BagData item in _defaultBags)
                {
                    BagData b = _bagManager.GetBagData(item.UniqueId);
                    AddBag(b, InventoryConsts.UNSET_BAG_ID, false);
                }

                SaveAllInventory_Server();
                Debug.Log($"Inventory json files did not exist. They were created with default bags.");
            }

            //Send loaded to client.
            try
            {
                //Load as text and let client deserialize.
                string baggedUnsortedTxt = File.ReadAllText(baggedUnsortedPath);
                string hiddenUnsortedTxt = File.ReadAllText(hiddenUnsortedPath);

                List<SerializableActiveBag> baggedUnsorted = JsonConvert.DeserializeObject<List<SerializableActiveBag>>(baggedUnsortedTxt);
                List<SerializableResourceQuantity> hiddenUnsorted = JsonConvert.DeserializeObject<List<SerializableResourceQuantity>>(hiddenUnsortedTxt);

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
            }
            catch
            {
                Debug.LogError($"Failed to load json files for resources or loadout.");
            }
        }

        /// <summary>
        /// Saves the clients inventory loadout on the server.
        /// </summary>
        [Server]
        private void SaveBaggedInventorySorted_Server(List<SerializableActiveBag> sabs)
        {
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
        /// Saves current inventory resource quantities to the server database.
        /// </summary>
        [Server]
        private void SaveAllInventory_Server()
        {
            List<SerializableActiveBag> baggedUnsorted = ActiveBags.ValuesToList().ToSerializable();
            List<SerializableResourceQuantity> hiddenUnsorted = CollectionCaches<SerializableResourceQuantity>.RetrieveList();
            foreach (KeyValuePair<uint, int> item in HiddenResources)
                hiddenUnsorted.Add(new SerializableResourceQuantity(item.Key, item.Value));

            //TODO: Use a database rather than json file. save only diff when resources are added.
            string baggedPath = Path.Combine(Application.dataPath, INVENTORY_BAGGED_UNSORTED_FILENAME);
            string hiddenPath = Path.Combine(Application.dataPath, INVENTORY_HIDDEN_UNSORTED_FILENAME);

            try
            {
                string result;

                result = JsonConvert.SerializeObject(baggedUnsorted, Formatting.Indented);
                File.WriteAllText(baggedPath, result);

                result = JsonConvert.SerializeObject(hiddenUnsorted, Formatting.Indented);
                File.WriteAllText(hiddenPath, result);
            }
            catch { }

            CollectionCaches<SerializableResourceQuantity>.Store(hiddenUnsorted);
        }


        /// <summary>
        /// Uses serializable data to set inventory.
        /// </summary>
        private void ApplyInventory_Server(List<SerializableActiveBag> sabs, List<SerializableResourceQuantity> allResources)
        {
            //ActiveBags.Clear();
            //HiddenResources.Clear();

            //foreach (SerializableActiveBag item in activeBags)
            //{
            //    ActiveBag ab = item.ToNative(_bagManager);
            //    AddBag(ab, false);
            //}
            ///* The server builds activeBags on the fly filling slots
            // * as it can.
            // * 
            // * Since unsortedInventory contains only each item once, and the
            // * quantity to that item, its safe to fill slots as items are read
            // * without having to go back to see if there was a partial slot filled.
            // * 
            // * The client ApplyInventory does check for partial slots because they
            // * can move things around their inventory, and split stacks. */

            //foreach (SerializableBagData item in hiddenResources.Bags)
            //{
            //    BagData bd = item.ToNative(_bagManager);
            //    AddBag(bd, false);
            //}

            //List<ResourceQuantity> rqs = hiddenResources.ResourceQuantities.ToNative();
            //int bagIndex = 0;
            //int bagSlot = 0;
            //foreach (ResourceQuantity item in rqs)
            //{
            //    ResourceData rd = _resourceManager.GetResourceData(item.UniqueId);
            //    //Non-baggable are easy enough.
            //    if (!rd.IsBaggable)
            //    { 
            //        HiddenResources.Add(item.UniqueId, item.Quantity);
            //    }

            //}


            ///* ResourceQuantities which are handled inside the users saved inventory
            //* are removed from unsortedInventory. Any ResourceQuantities remaining in unsorted
            //* inventory are added to whichever slots are available in the users inventory.
            //* 
            //* If a user doesn't have the bag entirely which is in their saved inventory
            //* then it's skipped over. This will result in any skipped entries filling slots
            //* as described above. */

            ////TODO: convert linq lookups to for loops for quicker iteration.

            ////Make resources into dictionary for quicker lookups.
            ////Resource UniqueIds and quantity of each.
            //Dictionary<uint, int> rqsDict = CollectionCaches<uint, int>.RetrieveDictionary();
            //foreach (SerializableResourceQuantity item in hiddenResources.ResourceQuantities)
            //    rqsDict[item.UniqueId] = item.Quantity;

            ///* First check if unsortedInv contains all the bags used
            // * in sortedInv. If sortedInv says a bag is used that the client
            // * does not have then the bag is unset from sorted which will
            // * cause the resources to be placed wherever available. */
            //for (int i = 0; i < activeBags.Count; i++)
            //{
            //    int bagIndex = hiddenResources.Bags.FindIndex(x => x.UniqueId == activeBags[i].BagDataUniqueId);
            //    //Bag not found, remove bag from sortedInventory.
            //    if (bagIndex == -1)
            //    {
            //        activeBags.RemoveAt(i);
            //        i--;
            //    }
            //    //Bag found, remove from unsorted so its not used twice.
            //    else
            //    {
            //        hiddenResources.Bags.RemoveAt(bagIndex);
            //    }
            //}

            ///* Check if unsortedInv contains the same resources as
            // * sortedinv. This uses the same approach as above where
            // * inventory items which do not exist in unsorted are removed
            // * from sorted. */
            //for (int i = 0; i < activeBags.Count; i++)
            //{
            //    for (int z = 0; z < activeBags[i].FilledSlots.Count; z++)
            //    {
            //        FilledSlot fs = activeBags[i].FilledSlots[z];
            //        rqsDict.TryGetValue(fs.ResourceQuantity.UniqueId, out int unsortedCount);
            //        /* Subtract sortedCount from unsortedCount. If the value is negative
            //         * then the result must be removed from unsortedCount. Additionally,
            //         * remove the resourceId from rqsDict since it no longer has value. */
            //        int quantityDifference = (unsortedCount - fs.ResourceQuantity.Quantity);
            //        if (quantityDifference < 0)
            //        {
            //            fs.ResourceQuantity.Quantity += quantityDifference;
            //            activeBags[i].FilledSlots[z] = fs;
            //        }

            //        //If there is no more quantity left then remove from unsorted.
            //        if (quantityDifference <= 0)
            //            rqsDict.Remove(fs.ResourceQuantity.UniqueId);
            //        //Still some quantity left, update unsorted.
            //        else
            //            rqsDict[fs.ResourceQuantity.UniqueId] = quantityDifference;
            //    }
            //}

            ////Add starting with sorted bags.
            //foreach (SerializableActiveBag sab in activeBags)
            //{
            //    ActiveBag ab = sab.ToNative(_bagManager);
            //    AddBag(ab);
            //}

            ////Add remaining bags from unsorted.
            //foreach (SerializableBagData sb in hiddenResources.Bags)
            //{
            //    BagData b = _bagManager.GetBagData(sb.UniqueId);
            //    AddBag(b);
            //}

            ///* This builds a cache of resources currently in the inventory.
            // * Since ActiveBags were set without allowing rebuild to save perf
            // * it's called here after all bags are added. */
            //RebuildBaggedResources();
            ////Add remaining resources to wherever they fit.
            //foreach (KeyValuePair<uint, int> item in rqsDict)
            //    ModifiyResourceQuantity(item.Key, item.Value, false);


            //if (sendToClient)
            //    TgtApplyInventory(base.Owner, hiddenResources, activeBags);

            //int rqsDictCount = rqsDict.Count;
            //CollectionCaches<uint, int>.Store(rqsDict);
            ///* If there were unsorted added then save clients new
            //* layout after everything was added. */
            //return (hiddenResources.Bags.Count > 0 || rqsDictCount > 0);
        }
    }

}