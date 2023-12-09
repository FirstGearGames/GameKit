using FishNet.Connection;
using FishNet.Object;
using FishNet.Serializing;
using GameKit.Core.CraftingAndInventories.Inventories.Canvases;
using GameKit.Core.Inventories.Bags;
using GameKit.Core.Resources;
using GameKit.Dependencies.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
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
        /// Called when the server spawns this object.
        /// </summary>
        private void OnStartServer_Loadout()
        {
            //BagManager bm = base.NetworkManager.GetInstance<BagManager>();
            //foreach (Bag item in _defaultBags)
            //{
            //    Bag b = bm.GetBag(item.UniqueId);
            //    AddBag(b);
            //}
        }

        private UnsortedInventory GetRandomUnsortedInventory(int bagCount, int resourceCount)
        {
            UnsortedInventory ui = new UnsortedInventory();
            ui.Bags = new List<SerializableBag>();
            for (int i = 0; i < bagCount; i++)
            {
                int randomA = UnityEngine.Random.Range(0, 2000);
                SerializableBag sb = new SerializableBag(randomA);
                ui.Bags.Add(sb);
            }

            ui.ResourceQuantities = new List<SerializableResourceQuantity>();
            for (int i = 0; i < resourceCount; i++)
            {
                int randomA = UnityEngine.Random.Range(0, 10000);
                int randomB = UnityEngine.Random.Range(0, 2000);

                SerializableResourceQuantity rq = new SerializableResourceQuantity(randomA, randomB);
                ui.ResourceQuantities.Add(rq);
            }

            return ui;
        }



        private List<SerializableActiveBag> GetRandomSerializableActiveBags(int bagCount, int resourceCount)
        {
            List<SerializableActiveBag> results = new List<SerializableActiveBag>();
            for (int x = 0; x < bagCount; x++)
            {

                int randomD = UnityEngine.Random.Range(0, 2000);
                int randomE = UnityEngine.Random.Range(0, 2000);

                SerializableActiveBag sab = new SerializableActiveBag(randomD, randomE);
                sab.FilledSlots = new List<SerializableActiveBag.FilledSlot>();

                for (int i = 0; i < resourceCount; i++)
                {
                    int randomA = UnityEngine.Random.Range(0, 2000);
                    int randomB = UnityEngine.Random.Range(0, 2000);
                    int randomC = UnityEngine.Random.Range(0, 100);
                    SerializableResourceQuantity srq = new SerializableResourceQuantity(randomA, randomB);
                    SerializableActiveBag.FilledSlot fs = new SerializableActiveBag.FilledSlot(randomC, srq);
                    sab.FilledSlots.Add(fs);
                }
                results.Add(sab);
            }

            return results;
        }



        /// <summary>
        /// Called when this spawns for a client.
        /// </summary>
        private void OnSpawnServer_Loadout(NetworkConnection c)
        {
            string resourcesPath = Path.Combine(Application.dataPath, UNSORTED_INVENTORY_FILENAME);
            string loadoutPath = Path.Combine(Application.dataPath, SORTED_INVENTORY_FILENAME);
            if (!File.Exists(resourcesPath) || !File.Exists(loadoutPath))
            {
                Debug.LogWarning($"One or more paths missing.");
                return;
            }
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
            catch { }

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
            //ResourceIds and quantity of each.
            Dictionary<int, int> rqsDict = CollectionCaches<int, int>.RetrieveDictionary();
            foreach (SerializableResourceQuantity item in unsortedInv.ResourceQuantities)
                rqsDict[item.ResourceId] = item.Quantity;

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
                    rqsDict.TryGetValue(fs.ResourceQuantity.ResourceId, out int unsortedCount);
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
                        rqsDict.Remove(fs.ResourceQuantity.ResourceId);
                    //Still some quantity left, update unsorted.
                    else
                        rqsDict[fs.ResourceQuantity.ResourceId] = quantityDifference;
                }
            }


            BagManager bagManager = base.NetworkManager.GetInstance<BagManager>();
            //Add starting with sorted bags.
            foreach (SerializableActiveBag sab in sortedInv)
            {
                Bag bag = bagManager.GetBag(sab.BagUniqueId);
                //Fill slots.
                ResourceQuantity[] rqs = new ResourceQuantity[bag.Space];
                foreach (SerializableActiveBag.FilledSlot item in sab.FilledSlots)
                {
                    if (item.ResourceQuantity.Quantity > 0)
                        rqs[item.Slot] = new ResourceQuantity(item.ResourceQuantity.ResourceId, item.ResourceQuantity.Quantity);
                }
                //Create active bag and add.
                ActiveBag ab = new ActiveBag(bag, sab.Index, rqs);
                AddBag(ab, false);
            }

            //Add remaining bags from unsorted.
            foreach (SerializableBag sb in unsortedInv.Bags)
            {
                Bag b = bagManager.GetBag(sb.UniqueId);
                AddBag(b, false);
            }

            /* This builds a cache of resources currently in the inventory.
             * Since ActiveBags were set without allowing rebuild to save perf
             * it's called here after all bags are added. */
            RebuildBaggedResources();
            //Add remaining resources to wherever they fit.
            foreach (KeyValuePair<int, int> item in rqsDict)
                ModifiyResourceQuantity(item.Key, item.Value, false);

            /* If there were unsorted added then save clients new
             * layout after everything was added. */
            if (unsortedInv.Bags.Count > 0 || rqsDict.Count > 0)
                LoadoutManuallyChanged();

            CollectionCaches<int, int>.Store(rqsDict);
        }

        private string InventoryToJson()
        {
            string result = JsonConvert.SerializeObject(Bags.ToSerializable(), Formatting.Indented);
            return result;
        }

        public static byte[] CompressGZip(byte[] bytes)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(memoryStream, System.IO.Compression.CompressionLevel.Optimal))
                {
                    gzipStream.Write(bytes, 0, bytes.Length);
                }
                return memoryStream.ToArray();
            }
        }

        public static byte[] DecompressGZip(byte[] bytes)
        {
            using (var memoryStream = new MemoryStream(bytes))
            {

                using (var outputStream = new MemoryStream())
                {
                    using (var decompressStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                    {
                        decompressStream.CopyTo(outputStream);
                    }
                    return outputStream.ToArray();
                }
            }
        }
        /// <summary>
        /// Called whenever the InventoryCanvas view is changed by the user.
        /// This could be modifying bag or item occupancy and order.
        /// </summary>
        public void LoadoutManuallyChanged()
        {
            SaveLoadout();
        }

        /// <summary>
        /// Saves the clients inventory loadout locally.
        /// </summary>
        [Client]
        private void SaveLoadout()
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
        public static byte[] CompressBrotli(byte[] bytes)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var brotliStream = new BrotliStream(memoryStream, System.IO.Compression.CompressionLevel.Optimal))
                {
                    brotliStream.Write(bytes, 0, bytes.Length);
                }
                return memoryStream.ToArray();
            }
        }
        public static byte[] DecompressBrotli(byte[] bytes)
        {
            using (var memoryStream = new MemoryStream(bytes))
            {
                using (var outputStream = new MemoryStream())
                {
                    using (var decompressStream = new BrotliStream(memoryStream, CompressionMode.Decompress))
                    {
                        decompressStream.CopyTo(outputStream);
                    }
                    return outputStream.ToArray();
                }
            }
        }
        /// <summary>
        /// Saves current inventory resource quantities to the server database.
        /// </summary>
        [Server]
        private void SaveInventoryUnsorted()
        {
            List<SerializableBag> bags = CollectionCaches<SerializableBag>.RetrieveList();
            //ResourceIds and quantity of each.
            Dictionary<int, int> rqsDict = CollectionCaches<int, int>.RetrieveDictionary();

            //Add all current resources to res.
            foreach (ActiveBag item in Bags)
            {
                bags.Add(item.Bag.ToSerializable());
                foreach (ResourceQuantity rq in item.Slots)
                {
                    if (!rq.IsUnset)
                    {
                        rqsDict.TryGetValue(rq.ResourceId, out int count);
                        count += rq.Quantity;
                        rqsDict[rq.ResourceId] = count;
                    }
                }
            }

            List<SerializableResourceQuantity> rqsLst = CollectionCaches<SerializableResourceQuantity>.RetrieveList();
            //Convert dictionary to ResourceQuantity list.
            foreach (KeyValuePair<int, int> item in rqsDict)
                rqsLst.Add(new SerializableResourceQuantity(item.Key, item.Value));
            //Recycle dictionary.
            CollectionCaches<int, int>.Store(rqsDict);

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