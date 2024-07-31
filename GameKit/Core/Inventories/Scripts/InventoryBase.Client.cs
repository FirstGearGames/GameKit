using FishNet.Object;
using UnityEngine;
using FishNet.Connection;
using GameKit.Core.Resources;
using GameKit.Core.Inventories.Bags;
using FishNet.Managing;
using System.Collections.Generic;
using GameKit.Dependencies.Utilities;
using GameKit.Core.Databases.LiteDb;

namespace GameKit.Core.Inventories
{
    public partial class InventoryBase : NetworkBehaviour
    {

        /// <summary>
        /// Saves the clients sorted bagged inventory.
        /// </summary>
        [Client]
        public void SaveBaggedSorted_Client(bool sendToServer)
        {
            List<SerializableActiveBag> sabs = ActiveBagsToSerializable();
            //Save locally.
            InventoryDbService.Instance.SetSortedInventory(this, sabs);

            if (sendToServer)
                SvrSaveBaggedSorted(sabs);

            CollectionCaches<SerializableActiveBag>.Store(sabs);
        }

        /// <summary>
        /// Sends the players inventory loadout in the order they last used.
        /// </summary>
        /// <param name="baggedUnsorted">Bagged resources.</param>        
        /// <param name="hiddenUnsorted">Hidden resources.</param>
        /// <param name="baggedUnsorted">User sorted bags for the Character.</param>
        [TargetRpc(ExcludeServer = true)]
        private void TgtApplyInventory(NetworkConnection c, List<SerializableActiveBag> baggedUnsorted, List<SerializableResourceQuantity> hiddenUnsorted, List<SerializableActiveBag> baggedSorted)
        {
            bool changed;
            RebuildBaggedResourcesDel rebuildDel;

            rebuildDel = new RebuildBaggedResourcesDel(RebuildBaggedResources);
            changed = ApplyInventory(baggedUnsorted, baggedSorted, rebuildDel);

            if (changed)
                SaveBaggedSorted_Client(false);
        }

        /// <summary>
        /// Applies a sorted inventory.
        /// Include data for the proper 
        /// </summary>
        /// <param name="activeBags">ActiveBags reference for the inventory.</param>
        /// <param name="baggedUnsorted">Unsorted ActiveBags received from the server.</param>
        /// <param name="baggedSorted">Sorted ActiveBags received from the server.</param>
        private bool ApplyInventory(List<SerializableActiveBag> baggedUnsorted, List<SerializableActiveBag> baggedSorted, RebuildBaggedResourcesDel rebuildBaggedResourcesDel)
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
            Dictionary<uint, int> resourceQuantitiesLookup = CollectionCaches<uint, int>.RetrieveDictionary();
            foreach (SerializableActiveBag item in baggedUnsorted)
            {
                foreach (SerializableFilledSlot fs in item.FilledSlots)
                {
                    uint id = fs.ResourceQuantity.UniqueId;
                    resourceQuantitiesLookup.TryGetValue(id, out int currentQuantity);
                    currentQuantity += fs.ResourceQuantity.Quantity;
                    resourceQuantitiesLookup[id] = currentQuantity;
                }
            }

            /* First check if unsorted contains all the bags used
           * in sortedInv. If sortedI says a bag is used that the client
           * does not have then the bag is unset from sorted which will
           * cause the resources to be placed wherever available. */
            for (int i = 0; i < baggedSorted.Count; i++)
            {
                int bagIndex = GetUnsortedIndex(baggedSorted[i].UniqueId);
                //Bag not found, remove bag from sortedInventory.
                if (bagIndex == -1)
                {
                    baggedSorted.RemoveAt(i);
                    i--;
                }
                //Bag found, remove from unsorted so its not used twice.
                else
                {
                    baggedUnsorted.RemoveAt(bagIndex);
                }

                int GetUnsortedIndex(uint unsortedBagUniqueId)
                {
                    for (int i = 0; i < baggedUnsorted.Count; i++)
                    {
                        if (baggedUnsorted[i].UniqueId == unsortedBagUniqueId)
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
            for (int i = 0; i < baggedSorted.Count; i++)
            {
                for (int z = 0; z < baggedSorted[i].FilledSlots.Count; z++)
                {
                    SerializableFilledSlot fs = baggedSorted[i].FilledSlots[z];
                    resourceQuantitiesLookup.TryGetValue((uint)fs.ResourceQuantity.UniqueId, out int unsortedCount);
                    /* Subtract sortedCount from unsortedCount. If the value is negative
                     * then the result must be removed from unsortedCount. Additionally,
                     * remove the resourceId from rqsDict since it no longer has value. */
                    int quantityDifference = (unsortedCount - fs.ResourceQuantity.Quantity);
                    if (quantityDifference < 0)
                    {
                        fs.ResourceQuantity.Quantity += quantityDifference;
                        baggedSorted[i].FilledSlots[z] = fs;
                    }

                    //If there is no more quantity left then remove from unsorted.
                    if (quantityDifference <= 0)
                        resourceQuantitiesLookup.Remove((uint)fs.ResourceQuantity.UniqueId);
                    //Still some quantity left, update unsorted.
                    else
                        resourceQuantitiesLookup[fs.ResourceQuantity.UniqueId] = quantityDifference;
                }
            }

            BagManager bagManager = base.NetworkManager.GetInstance<BagManager>();
            //Add starting with sorted bags.
            foreach (SerializableActiveBag sab in baggedSorted)
            {
                BagData bagData = bagManager.GetBagData(sab.BagDataUniqueId);
                //Fill slots.
                SerializableResourceQuantity[] rqs = new SerializableResourceQuantity[bagData.Space];
                foreach (SerializableFilledSlot item in sab.FilledSlots)
                {
                    if (item.ResourceQuantity.Quantity > 0)
                        rqs[item.Slot] = new SerializableResourceQuantity(item.ResourceQuantity.UniqueId, item.ResourceQuantity.Quantity);
                }
                //Create active bag and add.
                ActiveBag ab = new ActiveBag(sab.UniqueId, this, bagData, sab.LayoutIndex, rqs);
                AddBag(ab, false);
            }

            //Add remaining bags from unsorted.
            foreach (SerializableActiveBag sab in baggedUnsorted)
            {
                BagData b = bagManager.GetBagData(sab.BagDataUniqueId);
                AddBag(b, sab.UniqueId, false);
            }

            /* This builds a cache of resources currently in the inventory.
             * Since ActiveBags were set without allowing rebuild to save perf
             * it's called here after all bags are added. */
            rebuildBaggedResourcesDel?.Invoke();

            //Add remaining resources to wherever they fit.
            foreach (KeyValuePair<uint, int> item in resourceQuantitiesLookup)
                ModifyResourceQuantity(item.Key, item.Value, false);

            /* If there were unsorted added then save clients new
             * layout after everything was added. */
            bool sortedChanged = (baggedUnsorted.Count > 0 || resourceQuantitiesLookup.Count > 0);
            CollectionCaches<uint, int>.Store(resourceQuantitiesLookup);

            return sortedChanged;
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
            ModifyResourceQuantity(uniqueId, quantity);
        }

        /// <summary>
        /// Moves a resource from one slot to another.
        /// </summary>
        /// <param name="from">Information on where the resource is coming from.</param>
        /// <param name="to">Information on where the resource is going.</param>
        /// <param name="quantity">Quantity to move. If -1 the entire stack will move, if greater than 0 up to specified amount will move if target can accept.</param>
        /// <returns>True if the move was successful.</returns>
        [Client]
        public virtual bool MoveResource(BagSlot from, BagSlot to, int quantity = -1)
        {
            if (!GetResourceQuantity(from, out SerializableResourceQuantity fromRq))
                return false;
            if (!GetResourceQuantity(to, out SerializableResourceQuantity toRq))
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
            SaveBaggedSorted_Client(true);

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
                    toRq.Quantity += moveAmount;
                    fromRq.Quantity -= moveAmount;
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