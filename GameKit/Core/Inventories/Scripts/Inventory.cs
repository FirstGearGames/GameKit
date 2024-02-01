using FishNet.Object;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Connection;
using GameKit.Dependencies.Utilities;
using GameKit.Core.Crafting;
using GameKit.Core.Resources;
using GameKit.Core.Inventories.Bags;

namespace GameKit.Core.Inventories
{

    public partial class Inventory : NetworkBehaviour
    {
        #region Public.
        /// <summary>
        /// Called after bags are added or removed.
        /// </summary>
        public event BagsChangedDel OnBagsChannged;
        public delegate void BagsChangedDel(bool added, ActiveBag bag);
        /// <summary>
        /// Called when resources cannot be added due to a full inventory.
        /// </summary>
        public event InventoryFullDel OnInventoryFull;
        public delegate void InventoryFullDel(IEnumerable<ResourceData> resourcesNotAdded);
        /// <summary>
        /// Called when multiple resources have updated.
        /// </summary>
        public event BulkResourcesUpdatedDel OnBulkResourcesUpdated;
        public delegate void BulkResourcesUpdatedDel();
        /// <summary>
        /// Called when a single resource is updated.
        /// </summary>
        public event ResourceUpdatedDel OnResourceUpdated;
        public delegate void ResourceUpdatedDel(ResourceQuantity resourceQuantity);
        /// <summary>
        /// Called when inventory slots change with new, removed, or additionally stacked items.
        /// </summary>
        public event BagSlotUpdatedDel OnBagSlotUpdated;
        public delegate void BagSlotUpdatedDel(int bagIndex, int slotIndex, ResourceQuantity resource);
        /// <summary>
        /// Quantities of each resource.
        /// Key: the resource UniqueId.
        /// Value: quantity of the resource.
        /// </summary>
        [HideInInspector]
        public Dictionary<uint, int> ResourceQuantities = new Dictionary<uint, int>();
        /// <summary>
        /// Maximum space of all bags.
        /// </summary>        
        public int MaximumSlots
        {
            get
            {
                int total = 0;
                for (int i = 0; i < Bags.Count; i++)
                    total += Bags[i].MaximumSlots;

                return total;
            }
        }
        /// <summary>
        /// Used space over all bags.
        /// </summary>
        public int UsedSlots
        {
            get
            {
                int total = 0;
                for (int i = 0; i < Bags.Count; i++)
                    total += Bags[i].UsedSlots;

                return total;
            }
        }
        /// <summary>
        /// Space available over all bags.
        /// </summary>
        public int AvailableSlots
        {
            get
            {
                int total = 0;
                for (int i = 0; i < Bags.Count; i++)
                    total += Bags[i].AvailableSlots;

                return total;
            }
        }
        /// <summary>
        /// All active bags for this inventory.
        /// </summary>
        public List<ActiveBag> Bags { get; private set; } = new List<ActiveBag>();
        /// <summary>
        /// Resource UniqueIds and bag slots they occupy.
        /// </summary>
        public Dictionary<uint, List<BagSlot>> BaggedResources { get; private set; } = new();
        #endregion

        #region Serialized.
        /// <summary>
        /// Default bags to add.
        /// </summary>
        [Tooltip("Default bags to add.")]
        [SerializeField]
        private Bag[] _defaultBags = new Bag[0];
        #endregion

        #region Private.
        /// <summary>
        /// ResourceManager to use.
        /// </summary>
        private ResourceManager _resourceManager;
        /// <summary>
        /// Resources which are hidden from the player.
        /// Key: ResourceId.
        /// Value: Quantity.
        /// </summary>
        private Dictionary<uint, int> _hiddenResources = new Dictionary<uint, int>();
        /// <summary>
        /// Resources associated with quests which are hidden from the palyer.
        /// Key: QuestId.
        /// </summary>
        private Dictionary<uint, ResourceQuantity> _hiddenQuestResources = new Dictionary<uint, ResourceQuantity>();
        #endregion

        private void Awake()
        {
            InitializeOnce();
        }

        private void InitializeOnce()
        {
            Crafter crafter = GetComponent<Crafter>();
            crafter.OnCraftingResult += Crafter_OnCraftingResult;
        }

        public override void OnStartNetwork()
        {
            _resourceManager = base.NetworkManager.GetInstance<ResourceManager>();
        }

        public override void OnStartServer()
        {
            OnStartServer_Loadout();
        }

        public override void OnSpawnServer(NetworkConnection connection)
        {
            OnSpawnServer_Loadout(connection);
        }

        /// <summary>
        /// Called after receiving a crafting result.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="result"></param>
        /// <param name="asServer"></param>
        private void Crafter_OnCraftingResult(IRecipe r, CraftingResult result, bool asServer)
        {
            //Only update if as server, or as client and not host. This prevents double updating as host.
            bool canUpdateResources = (asServer || (!asServer && !base.IsHost));
            if (canUpdateResources && result == CraftingResult.Completed)
                UpdateResourcesFromRecipe(r, false);
        }

        /// <summary>
        /// Adds a Bag to Inventory.
        /// </summary>
        /// <param name="bag">Adds an ActiveBag for bag with no entries.</param>
        /// <param name="rebuildBaggedResources">True to rebuild cached bagged resources.</param>
        public void AddBag(Bag bag, bool rebuildBaggedResources)
        {
            ActiveBag ab = new ActiveBag(bag);
            ab.SetIndex(Bags.Count);
            AddBag(ab, rebuildBaggedResources);
        }

        /// <summary>
        /// Adds a Bag to Inventory.
        /// </summary>
        /// <param name="activeBag">ActiveBag information to add.</param>
        /// <param name="rebuildBaggedResources">True to rebuild cached bagged resources.</param>
        public void AddBag(ActiveBag activeBag, bool rebuildBaggedResources)
        {
            Bags.Insert(activeBag.Index, activeBag);
            OnBagsChannged?.Invoke(true, activeBag);
        }


        /// <summary>
        /// Returns the held quantity of a resource.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public int GetResourceQuantity(uint uniqueId)
        {
            int result;
            ResourceQuantities.TryGetValue(uniqueId, out result);
            return result;
        }

        //TODO: Is SendToClient needed???
        /// <summary>
        /// Adds or removes a resource quantity. Values can be negative to subtract quantity.
        /// </summary>
        /// <param name="uniqueId">Resource being modified.</param>
        /// <param name="quantity">Number of items to remove or add.</param>
        /// <param name="sendToClient">True to send the changes to the client.</param>
        /// <returns>Quantity which could not be added or removed due to space limitations or missing resources.</returns>
        public int ModifiyResourceQuantity(uint uniqueId, int quantity, bool sendToClient = true)
        {
            if (quantity == 0)
                return 0;

            if (quantity < 0)
                return RemoveResourceQuantity(uniqueId, (uint)(quantity * -1), sendToClient);
            else
                return AddResourceQuantity(uniqueId, (uint)quantity, sendToClient);
        }

        /// <summary>
        /// Removes a resource from the first available stacks regardless of the stack quantity.
        /// </summary>
        /// <param name="uniqueId">Resource to remove.</param>
        /// <param name="quantity">Amount to remove. Value must be positive.</param>
        /// <returns>Quantity which could not be removed due to missing resources.</returns>
        private int RemoveResourceQuantity(uint uniqueId, uint qPositive, bool sendToClient)
        {
            int quantity = (int)qPositive;

            int current;
            //Does not exist and removing, nothing further to be done.
            if (!ResourceQuantities.TryGetValue(uniqueId, out current))
                return 0;

            List<BagSlot> baggedResources;
            //If none in bags then return full quantity as what was unable to be removed.
            if (!BaggedResources.TryGetValue(uniqueId, out baggedResources))
                return quantity;

            int removed = 0;
            for (int bagResourceIndex = 0; bagResourceIndex < baggedResources.Count; bagResourceIndex++)
            {
                BagSlot br = baggedResources[bagResourceIndex];

                ActiveBag bag = Bags[br.BagIndex];
                int slotCount = bag.Slots[br.SlotIndex].Quantity;
                int removeCount = Mathf.Min(quantity, slotCount);

                //If remove count is the same as slot count then just unset the slot.
                if (removeCount == slotCount)
                {
                    bag.Slots[br.SlotIndex].MakeUnset();
                    //Remove entry from resources.
                    baggedResources.RemoveAt(bagResourceIndex);
                    bagResourceIndex--;
                }
                //Otherwise remove value.
                else
                {
                    bag.Slots[br.SlotIndex].Quantity -= removeCount;
                }

                InvokeBagSlotUpdated(br);

                //Remove from quantity.
                quantity -= removeCount;
                //If all was removed then break.
                if (quantity == 0)
                    break;

                //Invokes that a bag slot was updated for the current bag index and bagged resource.
                void InvokeBagSlotUpdated(BagSlot br)
                {
                    int bagIndex = br.BagIndex;
                    int slotIndex = br.SlotIndex;
                    ActiveBag brBag = Bags[bagIndex];
                    OnBagSlotUpdated?.Invoke(bagIndex, slotIndex, brBag.Slots[slotIndex]);
                }
            }

            int resourceCount = GetResourceCount(baggedResources, uniqueId);
            //If none left then remove.
            if (resourceCount == 0)
                ResourceQuantities.Remove(uniqueId);
            //Otherwise update.
            else
                ResourceQuantities[uniqueId] = resourceCount;

            if (sendToClient && base.IsServerInitialized && !base.Owner.IsLocalClient && removed > 0)
                TargetModifyResourceQuantity(base.Owner, uniqueId, (int)-removed);

            CompleteResourceQuantityChange(uniqueId, resourceCount);
            return (quantity - removed);
        }

        /// <summary>
        /// Adds a resource quantity to the first available existing stacks, or slots if no stacks are available.
        /// </summary>
        /// <param name="uniqueId">Resource to add.</param>
        /// <param name="qPositive">Quantity of resources to add.</param>
        /// <returns>Quantity which could not be added due to no available space.</returns>
        private int AddResourceQuantity(uint uniqueId, uint qPositive, bool sendToClient)
        {
            int quantity = (int)qPositive;

            ResourceData rd = _resourceManager.GetResourceData(uniqueId);
            int stackLimit = rd.StackLimit;
            List<BagSlot> baggedResources;
            //If none are bagged yet.
            if (!BaggedResources.TryGetValue(uniqueId, out baggedResources))
            {
                /* If there's no available slots then
                 * none can be bagged. Return full quantity
                 * as not added. */
                if (AvailableSlots == 0)
                    return quantity;

                //Otherwise add new bagged resources because at least one will be added.
                baggedResources = new List<BagSlot>();
                BaggedResources.Add(uniqueId, baggedResources);
            }

            //Check if can be added to existing stacks.
            for (int i = 0; i < baggedResources.Count; i++)
            {
                BagSlot br = baggedResources[i];
                ActiveBag bag = Bags[br.BagIndex];
                //Number currently in the slot.
                int slotCount = bag.Slots[br.SlotIndex].Quantity;
                //How many more can be added to this slot.
                int availableCount = (stackLimit - slotCount);

                int addCount = Mathf.Min(availableCount, quantity);
                //If can add onto the stack.
                if (addCount > 0)
                {
                    bag.Slots[br.SlotIndex].Quantity += addCount;
                    quantity -= addCount;
                    OnBagSlotUpdated?.Invoke(br.BagIndex, br.SlotIndex, bag.Slots[br.SlotIndex]);
                }

                //If all was added then break.
                if (quantity == 0)
                    break;
            }

            //If quantity remains then try to add to remaining slots.
            if (quantity > 0)
            {
                for (int bagIndex = 0; bagIndex < Bags.Count; bagIndex++)
                {
                    ActiveBag bag = Bags[bagIndex];

                    int slotsCount = bag.MaximumSlots;
                    for (int slotIndex = 0; slotIndex < slotsCount; slotIndex++)
                    {
                        //Already has an item.
                        if (!bag.Slots[slotIndex].IsUnset)
                            continue;

                        int addCount = Mathf.Min(stackLimit, quantity);
                        bag.Slots[slotIndex].Update(uniqueId, addCount);
                        quantity -= addCount;
                        //Since filling an empty slot add it to bagged resources.
                        baggedResources.Add(new BagSlot(bagIndex, slotIndex));
                        OnBagSlotUpdated?.Invoke(bagIndex, slotIndex, bag.Slots[slotIndex]);

                        //If no more quantity, exit.
                        if (quantity == 0)
                            break;
                    }

                    //If no more quantity, exit.
                    if (quantity == 0)
                        break;
                }
            }

            int resourceCount = GetResourceCount(baggedResources, uniqueId);
            ResourceQuantities[uniqueId] = resourceCount;
            CompleteResourceQuantityChange(uniqueId, resourceCount);

            /* Only send to update inventory
             * if the owner of this is not the clientHost.
             * IsLocalClient would return false if server
             * only because client is obviously not
             * running as server only. */
            uint added = (qPositive - (uint)quantity);
            if (sendToClient && base.IsServerInitialized && !base.Owner.IsLocalClient && added > 0)
                TargetModifyResourceQuantity(base.Owner, uniqueId, (int)added);

            return quantity;
        }

        /// <summary>
        /// Sends a resource change to the client.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="uniqueId">Resource being modified.</param>
        /// <param name="quantity">Quantity being added or removed.</param>
        [TargetRpc]
        private void TargetModifyResourceQuantity(NetworkConnection c, uint uniqueId, int quantity)
        {
            ModifiyResourceQuantity(uniqueId, quantity);
        }

        /// <summary>
        /// Completes the process of a resource quantity being updated.
        /// </summary>
        /// <param name="uniqueId">Resource changed.</param>
        /// <param name="currentQuantity">Current quantity of the resource after the change.</param>
        private void CompleteResourceQuantityChange(uint uniqueId, int currentQuantity)
        {
            ResourceQuantity rq = new ResourceQuantity(uniqueId, (int)currentQuantity);
            OnResourceUpdated?.Invoke(rq);
        }

        /// <summary>
        /// Gets the number of resources using BaggedResource data.
        /// </summary>
        /// <param name="uniqueId">If not unset only quantities of this value are returned.</param>
        private int GetResourceCount(List<BagSlot> resources, uint uniqueId = ResourceConsts.UNSET_RESOURCE_ID)
        {
            bool uniqueIdSpecified = (uniqueId != ResourceConsts.UNSET_RESOURCE_ID);
            //Loop through bagged resources again to get remaining.
            int count = 0;
            foreach (BagSlot br in resources)
            {
                ActiveBag bag = Bags[br.BagIndex];
                int slotCount = bag.Slots[br.SlotIndex].Quantity;

                //Is not the same as UniqueId, skip entry.
                if (uniqueIdSpecified)
                {
                    uint uId = bag.Slots[br.SlotIndex].UniqueId;
                    if (uId != uniqueId)
                        continue;
                }

                count += slotCount;
            }

            count = (int)Mathf.Max(0, count);
            return count;
        }


        /// <summary>
        /// Updates inventory resources using a recipe.
        /// This removes required resources while adding created.
        /// </summary>
        private void UpdateResourcesFromRecipe(IRecipe r, bool sendToClient = true)
        {
            //Remove needed resources first so space used is removed.
            foreach (ResourceQuantity rq in r.GetRequiredResources())
                ModifiyResourceQuantity(rq.UniqueId, -rq.Quantity);

            ResourceQuantity recipeResult = r.GetResult();
            ModifiyResourceQuantity(recipeResult.UniqueId, recipeResult.Quantity, sendToClient);
            OnBulkResourcesUpdated?.Invoke();
        }


        /// <summary>
        /// Iterates all bags and rebuilds bagged resources.
        /// </summary>
        private void RebuildBaggedResources()
        {
            BaggedResources.Clear();

            for (int i = 0; i < Bags.Count; i++)
            {
                ActiveBag bag = Bags[i];
                for (int z = 0; z < bag.Slots.Length; z++)
                {
                    ResourceQuantity rq = bag.Slots[z];
                    //Do not need to add if unset.
                    if (rq.IsUnset)
                        continue;

                    //Debug.Log($"Bag {i}, Slot {z}, ResourceId {rq.ResourceId}");
                    List<BagSlot> resources;
                    if (!BaggedResources.TryGetValue(rq.UniqueId, out resources))
                    {
                        resources = CollectionCaches<BagSlot>.RetrieveList();
                        BaggedResources.Add(rq.UniqueId, resources);
                    }

                    resources.Add(new BagSlot(i, z));
                }
            }
        }

        /// <summary>
        /// Outputs resource quantities of an ActiveBagResource.
        /// </summary>
        /// <param name="abr">ActiveBagResource to get quantities for.</param>
        /// <returns>True if the return was successful.</returns>
        private bool GetResourceQuantity(BagSlot abr, out ResourceQuantity rq)
        {
            return GetResourceQuantity(abr.BagIndex, abr.SlotIndex, out rq);
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

            //Invoke changes.
            OnBagSlotUpdated?.Invoke(from.BagIndex, from.SlotIndex, Bags[from.BagIndex].Slots[from.SlotIndex]);
            OnBagSlotUpdated?.Invoke(to.BagIndex, to.SlotIndex, Bags[to.BagIndex].Slots[to.SlotIndex]);
            InventorySortedChanged();

            return true;

            //Swaps the to and from entries.
            void SwapEntries()
            {
                Bags[from.BagIndex].Slots[from.SlotIndex] = toRq;
                Bags[to.BagIndex].Slots[to.SlotIndex] = fromRq;
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
                    Bags[to.BagIndex].Slots[to.SlotIndex] = toRq;
                    Bags[from.BagIndex].Slots[from.SlotIndex] = fromRq;
                }
            }
        }


        /// <summary>
        /// Returns if a slot exists.
        /// </summary>
        /// <param name="bagIndex">Bag index to check.</param>
        /// <param name="slotIndex">Slot index to check.</param>
        /// <returns></returns>
        private bool IsValidBagSlot(int bagIndex, int slotIndex)
        {
            if (bagIndex < 0 || bagIndex >= Bags.Count)
                return false;
            if (slotIndex < 0 || slotIndex >= Bags[bagIndex].Slots.Length)
                return false;

            //All conditions pass.
            return true;
        }

        /// <summary>
        /// Gets a ResourceQuantity using a bag and slot index.
        /// </summary>
        /// <param name="bagIndex">Bag index.</param>
        /// <param name="slotIndex">Slot index.</param>
        /// <param name="rq">ResourceQuantity found at indexes. This may be default if the slot is not occupied.</param>
        /// <returns>True if the bag and slot index was valid.</returns>
        public bool GetResourceQuantity(int bagIndex, int slotIndex, out ResourceQuantity rq)
        {
            //Invalid information.
            if (!IsValidBagSlot(bagIndex, slotIndex))
            {
                rq = default;
                return false;
            }
            //Valid.
            else
            {
                rq = Bags[bagIndex].Slots[slotIndex];
                return true;
            }
        }
    }

}