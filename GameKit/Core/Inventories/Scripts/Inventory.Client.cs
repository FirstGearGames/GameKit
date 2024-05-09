using FishNet.Object;
using UnityEngine;
using FishNet.Connection;
using GameKit.Core.Resources;
using GameKit.Core.Inventories.Bags;
using FishNet.Managing;
using System.Collections.Generic;
using System.IO;
using GameKit.Dependencies.Utilities;

namespace GameKit.Core.Inventories
{

    public partial class Inventory : NetworkBehaviour
    {

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
        /// Sends the players inventory loadout in the order they last used.
        /// </summary>
        [TargetRpc(ExcludeServer = true)]
        private void TgtApplyInventory(NetworkConnection c, List<SerializableActiveBag> baggedUnsorted, List<SerializableResourceQuantity> hiddenUnsorted, List<SerializableActiveBag> baggedSorted)
        {
            //If local sorted save needed correcting then save again.
            if (ApplyInventory_Client(baggedUnsorted, hiddenUnsorted, baggedSorted))
                SaveBaggedSorted_Client(false);
        }

        /// <summary>
        /// Uses serializable data to set inventory.
        /// </summary>
        /// <returns>True if sorted inventory was changed due to errors.</returns>
        private bool ApplyInventory_Client(List<SerializableActiveBag> activeBags, List<SerializableResourceQuantity> allResources, List<SerializableActiveBag> sortedBags)
        {
            return true;
            //TODO: For server save types in a database rather than JSON.
            ActiveBags.Clear();
            HiddenResources.Clear();

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
            //foreach (SerializableResourceQuantity item in hiddenResources)
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

            //int rqsDictCount = rqsDict.Count;
            //CollectionCaches<uint, int>.Store(rqsDict);
            ///* If there were unsorted added then save clients new
            //* layout after everything was added. */
            //return (hiddenResources.Bags.Count > 0 || rqsDictCount > 0);
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