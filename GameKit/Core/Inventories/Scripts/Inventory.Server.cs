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
        /// Called when this spawns for a client.
        /// </summary>
        private void LoadInventoryFromDatabase(NetworkConnection c, bool sendToClient = true)
        {
            //TODO: this should be using a database, not locallly saved file.
            string resourcesPath = Path.Combine(Application.dataPath, BAGGED_INVENTORY_FILENAME);
            string loadoutPath = Path.Combine(Application.dataPath, SORTED_INVENTORY_FILENAME);
            if (!File.Exists(resourcesPath) || !File.Exists(loadoutPath))
            {
                foreach (BagData item in _defaultBags)
                {
                    BagData b = _bagManager.GetBagData(item.UniqueId);
                    AddBag(b, false);
                }

                SaveInventoryUnsorted_Server();
                Debug.Log($"Inventory json files did not exist. They were created with default bags.");
            }

            //Send loaded to client.
            try
            {
                //Load as text and let client deserialize.
                string unsortedResources = File.ReadAllText(resourcesPath);
                string sortedResources = File.ReadAllText(loadoutPath);

                SerializableUnsortedInventory ui = JsonConvert.DeserializeObject<SerializableUnsortedInventory>(unsortedResources);
                List<SerializableActiveBag> sabs = JsonConvert.DeserializeObject<List<SerializableActiveBag>>(sortedResources);

                //Add bags and slots on server.                    
                ApplyInventory(ui, sabs, sendToClient);
            }
            catch
            {
                Debug.LogError($"Failed to load json files for resources or loadout.");
            }
        }


        /// <summary>
        /// Saves changes to clients sorted inventory on the server.
        /// </summary>
        private void ServerSaveInventorySorted(List<SerializableActiveBag> sabs)
        {
            /* //TODO: realistically client should only call this occasionally and server
             * should add checks to make sure client is not calling this excessively. */
            SaveInventorySorted_Server(sabs);
        }


        /// <summary>
        /// Saves the clients inventory loadout on the server.
        /// </summary>
        [Server]
        private void SaveInventorySorted_Server(List<SerializableActiveBag> sabs)
        {
            //TODO: use a database instead.
            string s = JsonConvert.SerializeObject(sabs);
            string path = Path.Combine(Application.dataPath, SORTED_INVENTORY_FILENAME);
            try
            {
                File.WriteAllText(path, s);
            }
            catch { }
        }


        /// <summary>
        /// Saves current inventory resource quantities to the server database.
        /// </summary>
        [Server]
        private void SaveInventoryUnsorted_Server()
        {
            List<SerializableBagData> bags = CollectionCaches<SerializableBagData>.RetrieveList();
            //Resource UNiqueIds and quantity of each.
            Dictionary<uint, int> rqsDict = CollectionCaches<uint, int>.RetrieveDictionary();

            //Add all current resources to res.
            foreach (ActiveBag item in ActiveBags.Values)
            {
                bags.Add(item.BagData.ToSerializable());
                foreach (ResourceQuantity rq in item.Slots)
                {
                    if (!rq.IsUnset)
                    {
                        rqsDict.TryGetValue(rq.UniqueId, out int count);
                        count += rq.Quantity;
                        rqsDict[rq.UniqueId] = count;
                    }
                }
            }

            List<SerializableResourceQuantity> rqsLst = CollectionCaches<SerializableResourceQuantity>.RetrieveList();
            //Convert dictionary to ResourceQuantity list.
            foreach (KeyValuePair<uint, int> item in rqsDict)
                rqsLst.Add(new SerializableResourceQuantity(item.Key, item.Value));
            //Recycle dictionary.
            CollectionCaches<uint, int>.Store(rqsDict);

            //TODO: Use a database rather than json file.
            string path = Path.Combine(Application.dataPath, BAGGED_INVENTORY_FILENAME);
            SerializableUnsortedInventory unsortedInv = new SerializableUnsortedInventory(bags, rqsLst);
            string result = JsonConvert.SerializeObject(unsortedInv, Formatting.Indented);
            try
            {
                File.WriteAllText(path, result);
            }
            catch { }

            CollectionCaches<SerializableBagData>.Store(bags);
            CollectionCaches<SerializableResourceQuantity>.Store(rqsLst);
        }


        /// <summary>
        /// Uses serializable data to set inventory.
        /// </summary>
        private void ApplyInventory_Server(SerializableUnsortedInventory hiddenResources, List<SerializableActiveBag> activeBags)
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