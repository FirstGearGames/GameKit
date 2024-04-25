using FishNet.Connection;
using FishNet.Object;
using GameKit.Core.Inventories.Bags;
using GameKit.Core.Resources;
using GameKit.Dependencies.Utilities;
using System.Collections.Generic;
using System.IO;
using Unity.Plastic.Newtonsoft.Json;
using UnityEngine;

namespace GameKit.Core.Inventories
{

    public partial class Inventory : NetworkBehaviour
    {
        /// <summary>
        /// Inventory without any sorting.
        /// </summary>
        private struct UnsortedInventory
        {
            /// <summary>
            /// Bags the client has.
            /// </summary>
            public List<SerializableBag> Bags;
            /// <summary>
            /// Resources across all bags the client has.
            /// </summary>
            public List<SerializableResourceQuantity> ResourceQuantities;

            public UnsortedInventory(List<SerializableBag> bags, List<SerializableResourceQuantity> resourceQuantities)
            {
                Bags = bags;
                ResourceQuantities = resourceQuantities;
            }
        }

        private const string UNSORTED_INVENTORY_FILENAME = "inventory_unsorted.json";
        private const string SORTED_INVENTORY_FILENAME = "inventory_sorted.json";

        /// <summary>
        /// Called when this spawns for a client.
        /// </summary>
        private void OnSpawnServer_Loadout(NetworkConnection c)
        {
            //return;
            string resourcesPath = Path.Combine(Application.dataPath, UNSORTED_INVENTORY_FILENAME);
            string loadoutPath = Path.Combine(Application.dataPath, SORTED_INVENTORY_FILENAME);
            if (!File.Exists(resourcesPath) || !File.Exists(loadoutPath))
            {
                Debug.Log($"Inventory json does not exist. Adding default bags.");
                BagManager bm = base.NetworkManager.GetInstance<BagManager>();
                foreach (BagData item in _defaultBags)
                {
                    BagData b = bm.GetBag(item.UniqueId);
                    AddBag(b, true);
                }
            }
            else
            {
                try
                {
                    //Load as text and let client deserialize.
                    string resources = File.ReadAllText(resourcesPath);
                    string loadout = File.ReadAllText(loadoutPath);

                    UnsortedInventory ui = JsonConvert.DeserializeObject<UnsortedInventory>(resources);
                    List<SerializableActiveBag> sab = JsonConvert.DeserializeObject<List<SerializableActiveBag>>(loadout);
                    //TODO: Save types in a database rather than JSON.
                    TgtApplyLoadout(c, ui, sab);
                }
                catch
                {
                    Debug.LogError($"Failed to load json files for resources or loadout.");
                }
            }
        }


        /// <summary>
        /// Sends the players inventory loadout in the order they last used.
        /// </summary>
        [TargetRpc]
        private void TgtApplyLoadout(NetworkConnection c, UnsortedInventory unsortedInv, List<SerializableActiveBag> sortedInv)
        {
            /* ResourceQuantities which are handled inside the users saved inventory
             * are removed from unsortedInventory. Any ResourceQuantities remaining in unsorted
             * inventory are added to whichever slots are available in the users inventory.
             * 
             * If a user doesn't have the bag entirely which is in their saved inventory
             * then it's skipped over. This will result in any skipped entries filling slots
             * as described above. */

            //TODO: convert linq lookups to for loops for quicker iteration.

            //Make resources into dictionary for quicker lookups.
            //Resource UniqueIds and quantity of each.
            Dictionary<uint, int> rqsDict = CollectionCaches<uint, int>.RetrieveDictionary();
            foreach (SerializableResourceQuantity item in unsortedInv.ResourceQuantities)
                rqsDict[item.UniqueId] = item.Quantity;

            /* First check if unsortedInv contains all the bags used
             * in sortedInv. If sortedInv says a bag is used that the client
             * does not have then the bag is unset from sorted which will
             * cause the resources to be placed wherever available. */
            for (int i = 0; i < sortedInv.Count; i++)
            {
                int bagIndex = unsortedInv.Bags.FindIndex(x => x.UniqueId == sortedInv[i].BagUniqueId);
                //Bag not found, remove bag from sortedInventory.
                if (bagIndex == -1)
                {
                    sortedInv.RemoveAt(i);
                    i--;
                }
                //Bag found, remove from unsorted so its not used twice.
                else
                {
                    unsortedInv.Bags.RemoveAt(bagIndex);
                }
            }

            /* Check if unsortedInv contains the same resources as
             * sortedinv. This uses the same approach as above where
             * inventory items which do not exist in unsorted are removed
             * from sorted. */
            for (int i = 0; i < sortedInv.Count; i++)
            {
                for (int z = 0; z < sortedInv[i].FilledSlots.Count; z++)
                {
                    SerializableActiveBag.FilledSlot fs = sortedInv[i].FilledSlots[z];
                    rqsDict.TryGetValue(fs.ResourceQuantity.UniqueId, out int unsortedCount);
                    /* Subtract sortedCount from unsortedCount. If the value is negative
                     * then the result must be removed from unsortedCount. Additionally,
                     * remove the resourceId from rqsDict since it no longer has value. */
                    int quantityDifference = (unsortedCount - fs.ResourceQuantity.Quantity);
                    if (quantityDifference < 0)
                    {
                        fs.ResourceQuantity.Quantity += quantityDifference;
                        sortedInv[i].FilledSlots[z] = fs;
                    }

                    //If there is no more quantity left then remove from unsorted.
                    if (quantityDifference <= 0)
                        rqsDict.Remove(fs.ResourceQuantity.UniqueId);
                    //Still some quantity left, update unsorted.
                    else
                        rqsDict[fs.ResourceQuantity.UniqueId] = quantityDifference;
                }
            }


            BagManager bagManager = base.NetworkManager.GetInstance<BagManager>();
            //Add starting with sorted bags.
            foreach (SerializableActiveBag sab in sortedInv)
            {
                BagData bag = bagManager.GetBag(sab.BagUniqueId);
                //Fill slots.
                ResourceQuantity[] rqs = new ResourceQuantity[bag.Space];
                foreach (SerializableActiveBag.FilledSlot item in sab.FilledSlots)
                {
                    if (item.ResourceQuantity.Quantity > 0)
                        rqs[item.Slot] = new ResourceQuantity(item.ResourceQuantity.UniqueId, item.ResourceQuantity.Quantity);
                }
                //Create active bag and add.
                ActiveBag ab = new ActiveBag(bag, sab.Index, rqs);
                AddBag(ab, false);
            }

            //Add remaining bags from unsorted.
            foreach (SerializableBag sb in unsortedInv.Bags)
            {
                BagData b = bagManager.GetBag(sb.UniqueId);
                AddBag(b, false);
            }

            /* This builds a cache of resources currently in the inventory.
             * Since ActiveBags were set without allowing rebuild to save perf
             * it's called here after all bags are added. */
            RebuildBaggedResources();
            //Add remaining resources to wherever they fit.
            foreach (KeyValuePair<uint, int> item in rqsDict)
                ModifiyResourceQuantity(item.Key, item.Value, false);

            /* If there were unsorted added then save clients new
             * layout after everything was added. */
            if (unsortedInv.Bags.Count > 0 || rqsDict.Count > 0)
                InventorySortedChanged();

            CollectionCaches<uint, int>.Store(rqsDict);
        }

        private string InventoryToJson()
        {
            string result = JsonConvert.SerializeObject(Bags.ToSerializable(), Formatting.Indented);
            return result;
        }

        /// <summary>
        /// Called whenever the InventoryCanvas view is changed by the user.
        /// This could be modifying bag or item occupancy and order.
        /// </summary>
        /// //TODO: Only needs to be called when the client manually moves resources.
        /// //TODO: Adding resources on the server should be calling SaveInventoryUnsorted but does not.
        public void InventorySortedChanged()
        {
            SaveInventorySorted();
        }

        /// <summary>
        /// Saves the clients inventory loadout locally.
        /// </summary>
        [Client]
        private void SaveInventorySorted()
        {
            string s = InventoryToJson();
            SaveInventoryUnsorted();
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
        private void SaveInventoryUnsorted()
        {
            List<SerializableBag> bags = CollectionCaches<SerializableBag>.RetrieveList();
            //Resource UNiqueIds and quantity of each.
            Dictionary<uint, int> rqsDict = CollectionCaches<uint, int>.RetrieveDictionary();

            //Add all current resources to res.
            foreach (ActiveBag item in Bags)
            {
                bags.Add(item.Bag.ToSerializable());
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

            string path = Path.Combine(Application.dataPath, UNSORTED_INVENTORY_FILENAME);
            UnsortedInventory unsortedInv = new UnsortedInventory(bags, rqsLst);
            string result = JsonConvert.SerializeObject(unsortedInv, Formatting.Indented);
            try
            {
                File.WriteAllText(path, result);
            }
            catch { }

            CollectionCaches<SerializableBag>.Store(bags);
            CollectionCaches<SerializableResourceQuantity>.Store(rqsLst);
        }
    }

}