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
                //TODO: read as bytes and compress on a thread.
                TgtApplyLoadout(c, resources, loadout);
            }
            catch { }

        }

        /// <summary>
        /// Sends the players inventory loadout in the order they last used.
        /// </summary>
        [TargetRpc]
        private void TgtApplyLoadout(NetworkConnection c, string unsortedInventory, string sortedInventory)
        {
            /* TODO: add ToOriginalType for Serializable variants so they can be modified easily.
             * then make use of the unset methods in original types. Add methods if they do not exist. */

            //TODO: convert linq lookups to for loops for quicker iteration.
            UnsortedInventory unsortedInv = JsonConvert.DeserializeObject<UnsortedInventory>(unsortedInventory);
            List<SerializableActiveBag> sortedInv = JsonConvert.DeserializeObject<List<SerializableActiveBag>>(sortedInventory);

            /* First check if unsortedInv contains all the bags used
             * in sortedInv. If sortedInv says a bag is used that the client
             * does not have then the bag is unset from sorted which will
             * cause the resources to be placed wherever available. */
            for (int i = 0; i < sortedInv.Count; i++)
            {
                SerializableActiveBag sab = sortedInv[i];
                int bagIndex = unsortedInv.Bags.FindIndex(x => x.UniqueId == sab.BagUniqueId);
                //Bag wasn't found. Unset bag id and update sortedinv.
                if (bagIndex == -1)
                {
                    sab.BagUniqueId = Bag.UNSET_UNIQUEID;
                    sortedInv[i] = sab;
                    continue;
                }

                /* If here the bag is found. Remove the bag from unsorted
                 * so it cannot be used twice. */
                unsortedInv.Bags.RemoveAt(bagIndex);
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
                    int index = unsortedInv.ResourceQuantities.FindIndex(x => x.ResourceId == fs.ResourceQuantity.ResourceId);
                    //Resource wasn't found, remove entry and update sortedInv.
                    if (index == -1)
                    {
                        fs.ResourceQuantity.ResourceId = ResourceQuantity.UNSET_RESOURCEID;
                        sortedInv[i].FilledSlots[z] = fs;
                    }
                }
            }

                //Add bags from unsorted.
                BagManager bm = base.NetworkManager.GetInstance<BagManager>();
            foreach (SerializableBag sb in unsortedInv.Bags)
            {
                Bag b = bm.GetBag(sb.UniqueId);
                AddBag(b);
            }


        }

        private string InventoryToJson()
        {
            string result = JsonConvert.SerializeObject(Bags.ToSerializable(), Formatting.Indented);
            return result;
        }

        private void Update()
        {
            //if (Time.frameCount % 500 == 0)
            //       SaveLoadout();
        }

        /// <summary>
        /// Saves the clients inventory loadout locally.
        /// </summary>
        [Client]
        private void SaveLoadout()
        {
            string s = InventoryToJson();
            if (s.Length > 200)
            {
                SaveInventoryUnsorted();
                string path = Path.Combine(Application.dataPath, SORTED_INVENTORY_FILENAME);
                try
                {
                    File.WriteAllText(path, s);
                }
                catch { }
            }

        }

        /// <summary>
        /// Saves current inventory resource quantities to the server database.
        /// </summary>
        [Server]
        private void SaveInventoryUnsorted()
        {
            List<SerializableBag> bags = CollectionCaches<SerializableBag>.RetrieveList();
            List<SerializableResourceQuantity> res = CollectionCaches<SerializableResourceQuantity>.RetrieveList();

            //Add all current resources to res.
            foreach (ActiveBag item in Bags)
            {
                bags.Add(item.Bag.ToSerializable());
                foreach (ResourceQuantity rq in item.Slots)
                {
                    if (!rq.IsUnset)
                        res.Add(rq.ToSerializable());
                }
            }

            string path = Path.Combine(Application.dataPath, UNSORTED_INVENTORY_FILENAME);
            UnsortedInventory unsortedInv = new UnsortedInventory(bags, res);
            string result = JsonConvert.SerializeObject(unsortedInv, Formatting.Indented);
            try
            {
                File.WriteAllText(path, result);
            }
            catch { }

            CollectionCaches<SerializableBag>.Store(bags);
            CollectionCaches<SerializableResourceQuantity>.Store(res);
        }
    }

}