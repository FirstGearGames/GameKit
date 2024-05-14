using FishNet.Object;
using UnityEngine;
using FishNet.Connection;
using GameKit.Core.Resources;
using GameKit.Core.Inventories.Bags;
using FishNet.Managing;
using System.Collections.Generic;
using System.IO;
using GameKit.Dependencies.Utilities;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;

namespace GameKit.Core.Inventories
{

    public partial class CharacterInventory : NetworkBehaviour
    {

        private Inventory _inventory;

        /// <summary>
        /// Saves the clients sorted bagged inventory.
        /// </summary>
        [Client]
        private void SaveBaggedSorted_Client(bool sendToServer = false)
        {
            //todo: client can save locally, server cannot. but still use a better way to store.
            string s = ActiveBagsToJson();
            string path = Path.Combine(Application.dataPath, INVENTORY_BAGGED_SORTED_FILENAME);
            try
            {
                File.WriteAllText(path, s);
            }
            catch { }

            if (sendToServer && !base.IsServerInitialized)
            {
                List<SerializableActiveBag> sabs = ActiveBags.ValuesToList().ToSerializable();
                SvrSaveBaggedSorted(sabs);
            }
        }

        /// <summary>
        /// Applies character inventory using all resources for this category(baggedUnsorted), and how the player has sorted them.
        /// </summary>
        [Client] 
        public void ApplyInventory(List<SerializableActiveBag> unsorted, List<SerializableActiveBag> sorted)
        {
            ActiveBags.Clear();
            /* ResourceQuantities which are handled inside the users saved inventory
             * are removed from unsortedInventory. Any ResourceQuantities remaining in unsorted
             * inventory are added to whichever slots are available in the users inventory.
             * 
             * If a user doesn't have the bag entirely which is in their saved inventory
             * then it's skipped over. This will result in any skipped entries filling slots
             * as described above. */
            //Make resources into dictionary for quicker lookups.
            //Resource UniqueIds and quantity of each.
            Dictionary<uint, int> rqsDict = CollectionCaches<uint, int>.RetrieveDictionary();
            foreach (SerializableActiveBag item in unsorted)
            {
                foreach (FilledSlot fs in item.FilledSlots)
                {
                    uint id = fs.ResourceQuantity.UniqueId;
                    rqsDict.TryGetValue(id, out int currentQuantity);
                    currentQuantity += fs.ResourceQuantity.Quantity;
                    rqsDict[id] = currentQuantity;
                }
            }

            /* First check if unsorted contains all the bags used
           * in sortedInv. If sortedI says a bag is used that the client
           * does not have then the bag is unset from sorted which will
           * cause the resources to be placed wherever available. */
            for (int i = 0; i < sorted.Count; i++)
            {
                int bagIndex = GetUnsortedIndex(sorted[i].UniqueId);
                //Bag not found, remove bag from sortedInventory.
                if (bagIndex == -1)
                {
                    sorted.RemoveAt(i);
                    i--;
                }
                //Bag found, remove from unsorted so its not used twice.
                else
                {
                    unsorted.RemoveAt(bagIndex);
                }

                int GetUnsortedIndex(uint unsortedBagUniqueId)
                {
                    for (int i = 0; i < unsorted.Count; i++)
                    {
                        if (unsorted[i].UniqueId == unsortedBagUniqueId)
                            return i;
                    }

                    //Not found.
                    return -1;
                }
            }

            /* Check if unsorted contains the same resources as
             * sorted. This uses the same approach as above where
             * inventory items which do not exist in unsorted are removed
             * from sorted. */
            for (int i = 0; i < sorted.Count; i++)
            {
                for (int z = 0; z < sorted[i].FilledSlots.Count; z++)
                {
                    FilledSlot fs = sorted[i].FilledSlots[z];
                    rqsDict.TryGetValue(fs.ResourceQuantity.UniqueId, out int unsortedCount);
                    /* Subtract sortedCount from unsortedCount. If the value is negative
                     * then the result must be removed from unsortedCount. Additionally,
                     * remove the resourceId from rqsDict since it no longer has value. */
                    int quantityDifference = (unsortedCount - fs.ResourceQuantity.Quantity);
                    if (quantityDifference < 0)
                    {
                        fs.ResourceQuantity.Quantity += quantityDifference;
                        sorted[i].FilledSlots[z] = fs;
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
            foreach (SerializableActiveBag sab in sorted)
            {
                BagData bagData = bagManager.GetBagData(sab.BagDataUniqueId);
                //Fill slots.
                ResourceQuantity[] rqs = new ResourceQuantity[bagData.Space];
                foreach (FilledSlot item in sab.FilledSlots)
                {
                    if (item.ResourceQuantity.Quantity > 0)
                        rqs[item.Slot] = new ResourceQuantity(item.ResourceQuantity.UniqueId, item.ResourceQuantity.Quantity);
                }
                //Create active bag and add.
                ActiveBag ab = new ActiveBag(sab.UniqueId, bagData, sab.LayoutIndex, rqs);
                AddBag(ab, false);
            }

            //Add remaining bags from unsorted.
            foreach (SerializableActiveBag sab in unsorted)
            {
                BagData b = bagManager.GetBagData(sab.BagDataUniqueId);
                AddBag(b, sab.UniqueId, false);
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
            if (unsorted.Count > 0 || rqsDict.Count > 0)
                InventorySortedChanged();

            CollectionCaches<uint, int>.Store(rqsDict);
        }
        /// <summary>
        /// Uses serializable data to set inventory.
        /// </summary>
        /// <returns>True if sorted inventory was changed due to errors.</returns>
        private bool ApplyInventory_Client(List<SerializableActiveBag> baggedUnsorted, List<SerializableActiveBag> baggedSorted)
        {
            //ActiveBags.Clear();
            //HiddenResources.Clear();

            ////HiddenUnsorted can simply be set because no sorting is needed.
            //hiddenUnsorted.ToNativeReplace(HiddenResources);
             
            //List<SerializableResourceQuantity> activeBagsUnsorted = CollectionCaches<SerializableResourceQuantity>.RetrieveList();
            ///* Right now we only care about the bag category for player inventory,
            // * which is category 0. A
            ///* ResourceQuantities which are handled inside the users saved inventory
            // * are removed from unsortedInventory. Any ResourceQuantities remaining in unsorted
            // * inventory are added to whichever slots are available in the users inventory.
            // * 
            // * If a user doesn't have the bag entirely which is in their saved inventory
            // * then it's skipped over. This will result in any skipped entries filling slots
            // * as described above. */

            ////TODO: convert linq lookups to for loops for quicker iteration.

            ////Make resources into dictionary for quicker lookups.
            ////Resource UniqueIds and quantity of each.
            //Dictionary<uint, int> rqsDict = CollectionCaches<uint, int>.RetrieveDictionary();
            //foreach (SerializableResourceQuantity item in activeBagsUnsorted.ResourceQuantities)
            //    rqsDict[item.UniqueId] = item.Quantity;

            ///* First check if unsortedInv contains all the bags used
            // * in sortedInv. If sortedInv says a bag is used that the client
            // * does not have then the bag is unset from sorted which will
            // * cause the resources to be placed wherever available. */
            //for (int i = 0; i < sortedInv.Count; i++)
            //{
            //    int bagIndex = activeBagsUnsorted.Bags.FindIndex(x => x.UniqueId == sortedInv[i].BagUniqueId);
            //    //Bag not found, remove bag from sortedInventory.
            //    if (bagIndex == -1)
            //    {
            //        sortedInv.RemoveAt(i);
            //        i--;
            //    }
            //    //Bag found, remove from unsorted so its not used twice.
            //    else
            //    {
            //        activeBagsUnsorted.Bags.RemoveAt(bagIndex);
            //    }
            //}

            ///* Check if unsortedInv contains the same resources as
            // * sortedinv. This uses the same approach as above where
            // * inventory items which do not exist in unsorted are removed
            // * from sorted. */
            //for (int i = 0; i < sortedInv.Count; i++)
            //{
            //    for (int z = 0; z < sortedInv[i].FilledSlots.Count; z++)
            //    {
            //        SerializableActiveBag.FilledSlot fs = sortedInv[i].FilledSlots[z];
            //        rqsDict.TryGetValue(fs.ResourceQuantity.UniqueId, out int unsortedCount);
            //        /* Subtract sortedCount from unsortedCount. If the value is negative
            //         * then the result must be removed from unsortedCount. Additionally,
            //         * remove the resourceId from rqsDict since it no longer has value. */
            //        int quantityDifference = (unsortedCount - fs.ResourceQuantity.Quantity);
            //        if (quantityDifference < 0)
            //        {
            //            fs.ResourceQuantity.Quantity += quantityDifference;
            //            sortedInv[i].FilledSlots[z] = fs;
            //        }

            //        //If there is no more quantity left then remove from unsorted.
            //        if (quantityDifference <= 0)
            //            rqsDict.Remove(fs.ResourceQuantity.UniqueId);
            //        //Still some quantity left, update unsorted.
            //        else
            //            rqsDict[fs.ResourceQuantity.UniqueId] = quantityDifference;
            //    }
            //}


            //BagManager bagManager = base.NetworkManager.GetInstance<BagManager>();
            ////Add starting with sorted bags.
            //foreach (SerializableActiveBag sab in sortedInv)
            //{
            //    Bag bag = bagManager.GetBag(sab.BagUniqueId);
            //    //Fill slots.
            //    ResourceQuantity[] rqs = new ResourceQuantity[bag.Space];
            //    foreach (SerializableActiveBag.FilledSlot item in sab.FilledSlots)
            //    {
            //        if (item.ResourceQuantity.Quantity > 0)
            //            rqs[item.Slot] = new ResourceQuantity(item.ResourceQuantity.UniqueId, item.ResourceQuantity.Quantity);
            //    }
            //    //Create active bag and add.
            //    ActiveBag ab = new ActiveBag(bag, sab.Index, rqs);
            //    AddBag(ab, false);
            //}

            ////Add remaining bags from unsorted.
            //foreach (SerializableBag sb in activeBagsUnsorted.Bags)
            //{
            //    Bag b = bagManager.GetBag(sb.UniqueId);
            //    AddBag(b, false);
            //}

            ///* This builds a cache of resources currently in the inventory.
            // * Since ActiveBags were set without allowing rebuild to save perf
            // * it's called here after all bags are added. */
            //RebuildBaggedResources();
            ////Add remaining resources to wherever they fit.
            //foreach (KeyValuePair<uint, int> item in rqsDict)
            //    ModifiyResourceQuantity(item.Key, item.Value, false);

            ///* If there were unsorted added then save clients new
            // * layout after everything was added. */
            //if (activeBagsUnsorted.Bags.Count > 0 || rqsDict.Count > 0)
            //    InventorySortedChanged();

            //CollectionCaches<uint, int>.Store(rqsDict);
        }


        /// <summary>
        /// Adds a Bag to Inventory.
        /// </summary>
        [TargetRpc(ExcludeServer = true)]
        private void TgtAddBag(NetworkConnection c, SerializableActiveBag bag)
        {
            AddBag(bag);
        }

        /// <summary>
        /// Sends a resource change to the client.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="uniqueId">Resource being modified.</param>
        /// <param name="quantity">Quantity being added or removed.</param>
        [TargetRpc(ExcludeServer = true)]
        private void TargetModifyResourceQuantity(NetworkConnection c, uint uniqueId, int quantity)
        {
            ModifiyResourceQuantity(uniqueId, quantity);
        }

        /// <summary>
        /// Moves a resource from one slot to another.
        /// </summary>
        /// <param name="from">Information on where the resource is coming from.</param>
        /// <param name="to">Information on where the resource is going.</param>
        /// <param name="quantity">Quantity to move. If -1 the entire stack will move, if greater than 0 up to specified amount will move if target can accept.</param>
        /// <returns>True if the move was successful.</returns>
        [Client]
        public bool MoveResource(BagSlot from, BagSlot to, int quantity = -1)
        {
            if (!GetResourceQuantity(from, out ResourceQuantity fromRq))
                return false;
            if (!GetResourceQuantity(to, out ResourceQuantity toRq))
                return false;
            if (from.Equals(to))
                return false;

            if (quantity == 0)
            {
                base.NetworkManager.LogError($"Quantity of {quantity} cannot be moved. Value must be -1 to move an entire slot, or a value greater than 0 to partial move a slot.");
                return false;
            }

            const int defaultQuantity = -1;

            //If the to is empty just simply move.
            if (toRq.IsUnset)
            {
                //If a quantity is specified then move this amount.
                if (quantity != defaultQuantity)
                    MoveQuantity();
                else
                    SwapEntries();
            }
            //If different items in each slot they cannot be stacked.
            else if (fromRq.UniqueId != toRq.UniqueId)
            {
                /* If an amount is specified this would suggest a split.
                 * If the split amount is not the full amount of from then
                 * the operation fails. */
                if (quantity != defaultQuantity && quantity != fromRq.Quantity)
                    return false;
                else
                    SwapEntries();
            }
            //Same resource if here. Try to stack.
            else
            {
                MoveQuantity();
            }


            //   public delegate void BagSlotUpdatedDel(ActiveBag activeBag, int slotIndex, ResourceQuantity resource);
            //Invoke changes.
            OnBagSlotUpdated?.Invoke(from.ActiveBag, from.SlotIndex, from.ActiveBag.Slots[from.SlotIndex]);
            OnBagSlotUpdated?.Invoke(to.ActiveBag, to.SlotIndex, to.ActiveBag.Slots[to.SlotIndex]);
            SaveBaggedSorted_Client();

            return true;

            //Swaps the to and from entries.
            void SwapEntries()
            {
                from.ActiveBag.Slots[from.SlotIndex] = toRq;
                to.ActiveBag.Slots[to.SlotIndex] = fromRq;
            }

            void MoveQuantity()
            {
                //Since the same resource stack limit can be from either from or to.
                ResourceData rd = _resourceManager.GetResourceData(fromRq.UniqueId);
                int stackLimit = rd.StackLimit;

                //Move as many as possible over.
                int moveAmount;
                //If quantity is unset then set move amount to stack size.
                //Set the move amount to max possible amount to complete the stack, or from quantity.

                bool unsetQuantity = (quantity == defaultQuantity);
                /* If quantity is unset then the goal is to move as much
                 * as possible onto the To slot. If the To slot is at maximum
                 * stacks then swap entries. */
                if (unsetQuantity && toRq.Quantity == stackLimit)
                {
                    SwapEntries();
                }
                else
                {
                    /* If no quantity was specified then try to move entire From.
                     * This will result in stacking as many as possible while leaving
                     * remainders. */
                    if (unsetQuantity)
                        quantity = fromRq.Quantity;

                    /* Move whichever is less of availability on To stack,
                     * or specified quantity. */
                    moveAmount = Mathf.Min((stackLimit - toRq.Quantity), quantity);
                    //Update to quantities.
                    toRq.UpdateQuantity(toRq.Quantity + moveAmount);
                    fromRq.UpdateQuantity(fromRq.Quantity - moveAmount);
                    //If from is empty then unset.
                    if (fromRq.Quantity <= 0)
                        fromRq.MakeUnset();

                    //Apply changes.
                    to.ActiveBag.Slots[to.SlotIndex] = toRq;
                    from.ActiveBag.Slots[from.SlotIndex] = fromRq;
                }
            }
        }
    }

}