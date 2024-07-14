using FishNet.Object;
using UnityEngine;
using GameKit.Core.Resources;
using FishNet.Managing;

namespace GameKit.Core.Inventories
{
    public partial class Inventory : NetworkBehaviour
    {
        public delegate void RebuildBaggedResourcesDel(ushort categoryId);

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