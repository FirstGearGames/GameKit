using FishNet.Object;
using UnityEngine;
using FishNet.Connection;
using GameKit.Core.Resources;
using GameKit.Core.Inventories.Bags;
using FishNet.Managing;
using System.Collections.Generic;
using GameKit.Core.Crafting;
using GameKit.Core.Crafting.Canvases;
using GameKit.Dependencies.Utilities;
using GameKit.Core.Databases.LiteDb;
using GameKit.Core.Inventories.Canvases;

namespace GameKit.Core.Inventories
{
    public partial class InventoryBase : NetworkBehaviour
    {
        #region Public.
        /// <summary>
        /// Called when a move attempt is processed.
        /// This invokes after bag slot update events.
        /// </summary>
        public event OnMoveDel OnMove;
        public delegate void OnMoveDel(BagSlot from, BagSlot to, bool moved);
        #endregion

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
        [TargetRpc]
        private void TgtApplyInventory(NetworkConnection c, List<SerializableActiveBag> baggedUnsorted, List<SerializableResourceQuantity> hiddenUnsorted, List<SerializableActiveBag> baggedSorted)
        {
            CraftingCanvas.Instance.RefreshAvailableRecipes();
            
            /* After updating crafting UI exit method if also server since
             * server already applied the inventory. */
            if (base.IsServerStarted)
                return;
            
            ApplyInventory_Client(baggedUnsorted, hiddenUnsorted, baggedSorted);
        }

        /// <summary>
        /// Applies inventory for a client.
        /// </summary>
        private void ApplyInventory_Client(List<SerializableActiveBag> baggedUnsorted, List<SerializableResourceQuantity> hiddenUnsorted, List<SerializableActiveBag> baggedSorted)
        {
            //If sorted does not exist then populate as new collection.
            if (baggedSorted == null) baggedSorted = new();
            
            RebuildBaggedResourcesDel rebuildDel = new(RebuildBaggedResources);
            bool changed = ApplyInventory_Client(baggedUnsorted, hiddenUnsorted, baggedSorted, rebuildDel);

            if (changed)
                SaveBaggedSorted_Client(false);
        }

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// Applies a sorted inventory.
        /// Include data for the proper 
        /// </summary>
        /// <param name="activeBags">ActiveBags reference for the inventory.</param>
        /// <param name="baggedUnsorted">Unsorted ActiveBags received from the server.</param>
        /// <param name="baggedSorted">Sorted ActiveBags received from the server.</param>
        private bool ApplyInventory_Client(List<SerializableActiveBag> baggedUnsorted, List<SerializableResourceQuantity> hiddenUnsorted, List<SerializableActiveBag> baggedSorted, RebuildBaggedResourcesDel rebuildBaggedResourcesDel)
        {
            ActiveBags.Clear();
            HiddenResources.Clear();

            foreach (SerializableResourceQuantity item in hiddenUnsorted)
                item.ToNativeReplace(HiddenResources);

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
                ModifyResourceQuantity(item.Key, item.Value, sendToClient: false);

            /* If there were unsorted added then save clients new
             * layout after everything was added. */
            bool sortedChanged = (baggedUnsorted.Count > 0 || resourceQuantitiesLookup.Count > 0);
            CollectionCaches<uint, int>.Store(resourceQuantitiesLookup);
            
            /* It's really important to call this at the end
             * after everything is in the inventory because calling
             * ModifyResourceQuantity also modifies ResourceQuantities.
             *
             * We could simply trust that it was done correctly through modify calls,
             * and it should be, but rebuilding at the end is much safer. */
            ApplyResourceQuantities(baggedUnsorted, hiddenUnsorted);
            
            return sortedChanged;
        }

        /// <summary>
        /// Adds a Bag to Inventory.
        /// </summary>
        [TargetRpc(ExcludeServer = true)]
        private void TgtAddBag(NetworkConnection c, SerializableActiveBag bag)
        {
            AddBag(bag, sendToClient: false);
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
            ModifyResourceQuantity(uniqueId, quantity, sendToClient: false);
        }

        /// <summary>
        /// Moves a resource from one slot to another.
        /// </summary>
        /// <param name="from">Information on where the resource is coming from.</param>
        /// <param name="to">Information on where the resource is going.</param>
        /// <param name="quantity">Quantity to move. If -1 the entire stack will move, if greater than 0 up to specified amount will move if target can accept.</param>
        /// <returns>True if the move was successful.</returns>
        [Client]
        public virtual bool MoveResource(BagSlot from, BagSlot to, int quantity = InventoryCanvasBase.UNSET_ENTRY_MOVE_QUANTITY)
        {
            if (!GetResourceQuantity(from, out SerializableResourceQuantity fromRq))
                return InvokeMoveResult(passed: false);
            if (!GetResourceQuantity(to, out SerializableResourceQuantity toRq))
                return InvokeMoveResult(passed: false);
            if (from.Equals(to))
                return InvokeMoveResult(passed: false);

            if (quantity == 0)
            {
                base.NetworkManager.LogError($"Quantity of {quantity} cannot be moved. Value must be -1 to move an entire slot, or a value greater than 0 to partial move a slot.");
                return InvokeMoveResult(passed: false);
            }
            //If quantity is not specified then set it to quantity on from.
            else if (quantity == InventoryCanvasBase.UNSET_ENTRY_MOVE_QUANTITY)
            {
                quantity = fromRq.Quantity;
            }

            //Since the same resource stack limit can be from either from or to.
            ResourceData fromRd = _resourceManager.GetResourceData(fromRq.UniqueId);
            ResourceData toRd = _resourceManager.GetResourceData(toRq.UniqueId);

            //If the to is empty just simply move.
            if (toRq.IsUnset)
            {
                MoveQuantity();
            }
            /* If different items in each slot they cannot be stacked.
             * Check if stacking is possible, and if not then swap entries. */
            else if (fromRq.UniqueId != toRq.UniqueId)
            {
                /* If an amount is specified this would suggest a split.
                 * If the split amount is not the full amount of from then
                 * the operation fails. */
                if (quantity != fromRq.Quantity)
                    return InvokeMoveResult(passed: false);
                else
                    SwapEntries();
            }
            //Same resource if here. Try to stack.
            else
            {
                //If either stack is already full then swap, otherwise move.
                if (quantity == fromRd.StackLimit || toRq.Quantity == toRd.StackLimit)
                    SwapEntries();
                else
                    MoveQuantity();
            }

            //Invoke changes.
            OnBagSlotUpdated?.Invoke(from.ActiveBag, from.SlotIndex, from.ActiveBag.Slots[from.SlotIndex]);
            OnBagSlotUpdated?.Invoke(to.ActiveBag, to.SlotIndex, to.ActiveBag.Slots[to.SlotIndex]);

            InventoryBase fromInventoryBase = from.ActiveBag.InventoryBase;
            InventoryBase toInventoryBase = to.ActiveBag.InventoryBase;

            //Save sorted.
            fromInventoryBase.SaveBaggedSorted_Client(true);
            if (fromInventoryBase.CategoryId != toInventoryBase.CategoryId)
                toInventoryBase.SaveBaggedSorted_Client(true);

            return InvokeMoveResult(passed: true);

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

                /* Be it moving all or some, the toRq uniqueId will
                 * become the from Id. */
                toRq.UniqueId = fromRq.UniqueId;

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

            bool InvokeMoveResult(bool passed)
            {
                if (OnMove != null)
                    OnMove.Invoke(from, to, passed);

                return passed;
            }
        }
    }
}