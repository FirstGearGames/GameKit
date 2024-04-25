using FishNet.Object;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Connection;
using GameKit.Dependencies.Utilities;
using GameKit.Core.Crafting;
using GameKit.Core.Resources;
using GameKit.Core.Inventories.Bags;
using FishNet.Managing;

namespace GameKit.Core.Inventories
{

    public partial class Inventory : NetworkBehaviour
    {
        #region Public.
        /// <summary>
        /// Called after bags are added or removed.
        /// </summary>
        public event BagsChangedDel OnBagsChanged;
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
        /// <summary>
        /// Resource UniqueIds and the number of the resource.
        /// These resources are not shown in the players bags but can be used to add hidden tokens or currencies.
        /// </summary>
        public Dictionary<uint, int> HiddenResources { get; private set; } = new();
        #endregion

        #region Serialized.
        /// <summary>
        /// Default bags to add.
        /// </summary>
        [Tooltip("Default bags to add.")]
        [SerializeField]
        private BagData[] _defaultBags = new BagData[0];
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
        private void Crafter_OnCraftingResult(RecipeData r, CraftingResult result, bool asServer)
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
        public void AddBag(BagData bag, bool rebuildBaggedResources)
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
            OnBagsChanged?.Invoke(true, activeBag);
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

            if (quantity > 0)
                return AddResourceQuantity(uniqueId, (uint)quantity, sendToClient);
            else
                return RemoveResourceQuantity(uniqueId, (uint)(quantity * -1), sendToClient);
        }

        /// <summary>
        /// Adds a resource quantity to the first available existing stacks, or slots if no stacks are available.
        /// </summary>
        /// <param name="uniqueId">Resource to add.</param>
        /// <param name="qPositive">Quantity of resources to add.</param>
        /// <returns>Quantity which could not be added due to no available space.</returns>
        private int AddResourceQuantity(uint uniqueId, uint qPositive, bool sendToClient)
        {
            ResourceData rd = _resourceManager.GetResourceData(uniqueId);
            //Amount which was allowed to be added.
            int added;
            if (rd.IsBaggable)
                added = AddBaggedResource();
            else
                added = AddHiddenResource();

            if (added > 0)
            {
                //Try to get current count.
                ResourceQuantities.TryGetValue(uniqueId, out int currentAdded);
                int newQuantity = (added + currentAdded);
                ResourceQuantities[uniqueId] = newQuantity;
                CompleteResourceQuantityChange(uniqueId, newQuantity);

                /* Only send to update inventory
                 * if the owner of this is not the clientHost.
                 * IsLocalClient would return false if server
                 * only because client is obviously not
                 * running as server only. */
                if (sendToClient && base.IsServerInitialized && !base.Owner.IsLocalClient)
                    TargetModifyResourceQuantity(base.Owner, uniqueId, added);
            }

            //Return how many were not added.
            return ((int)qPositive - added);

            //Returns added.
            int AddBaggedResource()
            {
                int stackLimit = rd.StackLimit;
                int thisAdded = 0;
                int quantityRemaining = (int)qPositive;
                List<BagSlot> baggedResources;
                //If none are bagged yet.
                if (!BaggedResources.TryGetValue(rd.UniqueId, out baggedResources))
                {
                    /* If there's no available slots then
                     * none can be bagged. Return full quantity
                     * as not added. */
                    if (AvailableSlots == 0)
                        return 0;
                    //Otherwise add new bagged resources because at least one will be added.
                    baggedResources = new List<BagSlot>();
                    BaggedResources.Add(rd.UniqueId, baggedResources);
                }

                //Check if can be added to existing stacks.
                for (int i = 0; i < baggedResources.Count; i++)
                {
                    BagSlot bagSlot = baggedResources[i];
                    ActiveBag bag = Bags[bagSlot.BagIndex];
                    //Number currently in the slot.
                    int slotCount = bag.Slots[bagSlot.SlotIndex].Quantity;
                    //How many more can be added to this slot.
                    int availableCount = (stackLimit - slotCount);

                    int addCount = Mathf.Min(availableCount, quantityRemaining);
                    //If can add onto the stack.
                    if (addCount > 0)
                    {
                        quantityRemaining -= addCount;
                        bag.Slots[bagSlot.SlotIndex].Quantity += addCount;
                        thisAdded += addCount;
                        InvokeBagSlotUpdated(bagSlot);
                    }

                    //If all was added then break.
                    if (quantityRemaining == 0)
                        break;
                }

                //If quantity remains then try to add to empty bag slots.
                if (quantityRemaining > 0)
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

                            int addCount = Mathf.Min(stackLimit, quantityRemaining);
                            thisAdded += addCount;
                            quantityRemaining -= addCount;
                            bag.Slots[slotIndex].Update(uniqueId, addCount);
                            //Since filling an empty slot add it to bagged resources.
                            BagSlot bs = new BagSlot(bagIndex, slotIndex);
                            baggedResources.Add(bs);
                            InvokeBagSlotUpdated(bs);

                            //If no more quantity, exit.
                            if (quantityRemaining == 0)
                                break;
                        }

                        //If no more quantity, exit.
                        if (quantityRemaining == 0)
                            break;
                    }
                }

                return thisAdded;
            }

            //Adds resource and returns amount added.
            int AddHiddenResource()
            {
                ResourceQuantities.TryGetValue(uniqueId, out int currentlyAdded);

                int availableCount = (rd.QuantityLimit - currentlyAdded);
                int addCount = Mathf.Min(availableCount, (int)qPositive);
                HiddenResources[uniqueId] = (addCount + currentlyAdded);

                return addCount;
            }

        }

        /// <summary>
        /// Removes a resource from the first available stacks regardless of the stack quantity.
        /// </summary>
        /// <param name="uniqueId">Resource to remove.</param>
        /// <returns>Quantity which could not be removed due to missing resources.</returns>
        private int RemoveResourceQuantity(uint uniqueId, uint qPositive, bool sendToClient)
        {
            int currentlyAdded;
            //None exist, return none removed.
            if (!ResourceQuantities.TryGetValue(uniqueId, out currentlyAdded))
                return (int)qPositive;

            ResourceData rd = _resourceManager.GetResourceData(uniqueId);

            int removed;
            if (rd.IsBaggable)
                removed = RemoveBaggedResource();
            else
                removed = RemoveHiddenResource();

            if (removed > 0)
            {
                int newQuantity = (currentlyAdded - removed);
                //If no more exist then remove key from dictionary.
                if (newQuantity == 0)
                    ResourceQuantities.Remove(uniqueId);
                else
                    ResourceQuantities[uniqueId] = newQuantity;

                CompleteResourceQuantityChange(uniqueId, newQuantity);

                /* Only send to update inventory
                 * if the owner of this is not the clientHost.
                 * IsLocalClient would return false if server
                 * only because client is obviously not
                 * running as server only. */
                if (sendToClient && base.IsServerInitialized && !base.Owner.IsLocalClient)
                    TargetModifyResourceQuantity(base.Owner, uniqueId, -removed);
            }

            return ((int)qPositive - removed);

            //Returns removed count.
            int RemoveBaggedResource()
            {
                int thisRemoved = 0;
                int quantityRemaining = (int)qPositive;

                List<BagSlot> baggedResources;
                /* We already checked quantities at the beginning though so this
                * should always exist. */
                BaggedResources.TryGetValue(uniqueId, out baggedResources);

                for (int bagResourceIndex = 0; bagResourceIndex < baggedResources.Count; bagResourceIndex++)
                {
                    BagSlot bagSlot = baggedResources[bagResourceIndex];

                    ActiveBag bag = Bags[bagSlot.BagIndex];
                    int slotCount = bag.Slots[bagSlot.SlotIndex].Quantity;
                    int removeCount = Mathf.Min(quantityRemaining, slotCount);
                    //If quantity can be removed.
                    if (removeCount > 0)
                    {
                        thisRemoved += removeCount;
                        //If remove count is the same as slot count then just unset the slot.
                        if (removeCount == slotCount)
                        {
                            bag.Slots[bagSlot.SlotIndex].MakeUnset();
                            //Remove entry from resources.
                            baggedResources.RemoveAt(bagResourceIndex);
                            bagResourceIndex--;
                        }
                        //Otherwise remove value.
                        else
                        {
                            bag.Slots[bagSlot.SlotIndex].Quantity -= removeCount;
                        }
                        quantityRemaining -= removeCount;
                    }

                    InvokeBagSlotUpdated(bagSlot);

                    //If all was removed then break.
                    if (quantityRemaining == 0)
                        break;
                }

                return thisRemoved;
            }

            //Adds resource and returns amount added.
            int RemoveHiddenResource()
            {
                int thisRemove = Mathf.Min(currentlyAdded, (int)qPositive);

                if (thisRemove > 0)
                    HiddenResources[uniqueId] = (currentlyAdded - thisRemove);

                return thisRemove;
            }

        }

        /// <summary>
        /// Invokes that a bag slot was updated for the supplied bagSlot.
        /// </summary>
        private void InvokeBagSlotUpdated(BagSlot br)
        {
            int bagIndex = br.BagIndex;
            int slotIndex = br.SlotIndex;
            ActiveBag brBag = Bags[bagIndex];
            OnBagSlotUpdated?.Invoke(bagIndex, slotIndex, brBag.Slots[slotIndex]);
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
        /// Updates inventory resources using a recipe.
        /// This removes required resources while adding created.
        /// </summary>
        private void UpdateResourcesFromRecipe(RecipeData r, bool sendToClient = true)
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