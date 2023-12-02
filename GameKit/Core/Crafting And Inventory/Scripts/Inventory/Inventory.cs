using FishNet.Object;
using System;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Managing.Server;
using FishNet.Managing.Logging;
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
        public delegate void InventoryFullDel(IEnumerable<IResourceData> resourcesNotAdded);
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
        /// Key: the resourceId.
        /// Value: quantity of the resource.
        /// </summary>
        [HideInInspector]
        public Dictionary<int, int> ResourceQuantities = new Dictionary<int, int>();
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
        /// ResourceIds and bag slots they occupy.
        /// </summary>
        public Dictionary<int, List<ActiveBagResource>> BaggedResources { get; private set; } = new();
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
        #endregion

        private void Awake()
        {
            InitializeOnce();
        }

        private void InitializeOnce()
        {
            //Add default bags.
            foreach (Bag item in _defaultBags)
                AddBag(item);

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
        /// <param name="space">Amount of space to give the bag.</param>
        public void AddBag(Bag bag)
        {
            ActiveBag ab = new ActiveBag(bag);
            ab.SetIndex(Bags.Count);
            Bags.Add(ab);
            OnBagsChannged?.Invoke(true, ab);
        }

        /// <summary>
        /// Returns the held quantity of a resource.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public int GetResourceQuantity(int resourceId)
        {
            int result;
            ResourceQuantities.TryGetValue(resourceId, out result);
            return result;
        }

        /// <summary>
        /// Adds or removes a resource quantity. Values can be negative to subtract quantity.
        /// </summary>
        /// <param name="resourceId">Resource being modified.</param>
        /// <param name="quantity">Number of items to remove or add.</param>
        /// <param name="sendToClient">True to send the changes to the client.</param>
        /// <returns>Quantity which could not be added or removed due to space limitations or missing resources.</returns>
        public int ModifiyResourceQuantity(int resourceId, int quantity, bool sendToClient = true)
        {
            if (quantity == 0)
                return 0;

            if (quantity < 0)
                return RemoveResourceQuantity(resourceId, (uint)(quantity * -1), sendToClient);
            else
                return AddResourceQuantity(resourceId, (uint)quantity, sendToClient);
        }

        /// <summary>
        /// Removes a resource from the first available stacks regardless of the stack quantity.
        /// </summary>
        /// <param name="resourceId">Resource to remove.</param>
        /// <param name="quantity">Amount to remove. Value must be positive.</param>
        /// <returns>Quantity which could not be removed due to missing resources.</returns>
        private int RemoveResourceQuantity(int resourceId, uint qPositive, bool sendToClient)
        {
            int quantity = (int)qPositive;

            int current;
            //Does not exist and removing, nothing further to be done.
            if (!ResourceQuantities.TryGetValue(resourceId, out current))
                return 0;

            List<ActiveBagResource> baggedResources;
            //If none in bags then return full quantity as what was unable to be removed.
            if (!BaggedResources.TryGetValue(resourceId, out baggedResources))
                return quantity;

            int removed = 0;
            for (int bagResourceIndex = 0; bagResourceIndex < baggedResources.Count; bagResourceIndex++)
            {
                ActiveBagResource br = baggedResources[bagResourceIndex];

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
                void InvokeBagSlotUpdated(ActiveBagResource br)
                {
                    int bagIndex = br.BagIndex;
                    int slotIndex = br.SlotIndex;
                    ActiveBag brBag = Bags[bagIndex];
                    OnBagSlotUpdated?.Invoke(bagIndex, slotIndex, brBag.Slots[slotIndex]);
                }
            }

            int resourceCount = GetResourceCount(baggedResources, resourceId);
            //If none left then remove.
            if (resourceCount == 0)
                ResourceQuantities.Remove(resourceId);
            //Otherwise update.
            else
                ResourceQuantities[resourceId] = resourceCount;

            if (sendToClient && !base.Owner.IsLocalClient && removed > 0)
                TargetModifyResourceQuantity(base.Owner, resourceId, (int)-removed);

            CompleteResourceQuantityChange(resourceId, resourceCount);
            return (quantity - removed);
        }

        /// <summary>
        /// Adds a resource quantity to the first available existing stacks, or slots if no stacks are available.
        /// </summary>
        /// <param name="resourceId">Resource to add.</param>
        /// <param name="qPositive">Quantity of resources to add.</param>
        /// <returns>Quantity which could not be added due to no available space.</returns>
        private int AddResourceQuantity(int resourceId, uint qPositive, bool sendToClient)
        {
            int quantity = (int)qPositive;

            IResourceData rd = _resourceManager.GetIResourceData(resourceId);
            int stackLimit = rd.GetStackLimit();

            List<ActiveBagResource> baggedResources;
            //If none are bagged yet.
            if (!BaggedResources.TryGetValue(resourceId, out baggedResources))
            {
                /* If there's no available slots then
                 * none can be bagged. Return full quantity
                 * as not added. */
                if (AvailableSlots == 0)
                    return quantity;

                //Otherwise add new bagged resources because at least one will be added.
                baggedResources = new List<ActiveBagResource>();
                BaggedResources.Add(resourceId, baggedResources);
            }

            //Check if can be added to existing stacks.
            for (int i = 0; i < baggedResources.Count; i++)
            {
                ActiveBagResource br = baggedResources[i];

                ActiveBag bag = Bags[br.BagIndex];
                int slotCount = bag.Slots[br.SlotIndex].Quantity;
                int availableStacks = (stackLimit - slotCount);

                int addCount = Mathf.Min(availableStacks, quantity);
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
                        bag.Slots[slotIndex].Update(resourceId, addCount);
                        quantity -= addCount;
                        //Since filling an empty slot add it to bagged resources.
                        baggedResources.Add(new ActiveBagResource(bagIndex, slotIndex));
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

            int resourceCount = GetResourceCount(baggedResources, resourceId);
            ResourceQuantities[resourceId] = resourceCount;
            CompleteResourceQuantityChange(resourceId, resourceCount);

            /* Only send to update inventory
             * if the owner of this is not the clientHost.
             * IsLocalClient would return false if server
             * only because client is obviously not
             * running as server only. */
            uint added = (qPositive - (uint)quantity);
            if (sendToClient && !base.Owner.IsLocalClient && added > 0)
                TargetModifyResourceQuantity(base.Owner, resourceId, (int)added);

            return quantity;
        }

        /// <summary>
        /// Sends a resource change to the client.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="resourceId">Resource being modified.</param>
        /// <param name="quantity">Quantity being added or removed.</param>
        [TargetRpc]
        private void TargetModifyResourceQuantity(NetworkConnection c, int resourceId, int quantity)
        {
            ModifiyResourceQuantity(resourceId, quantity);
        }

        /// <summary>
        /// Completes the process of a resource quantity being updated.
        /// </summary>
        /// <param name="resourceId">Resource changed.</param>
        /// <param name="currentQuantity">Current quantity of the resource after the change.</param>
        private void CompleteResourceQuantityChange(int resourceId, int currentQuantity)
        {
            ResourceQuantity rq = new ResourceQuantity(resourceId, (int)currentQuantity);
            OnResourceUpdated?.Invoke(rq);
        }

        /// <summary>
        /// Gets the number of resources using BaggedResource data.
        /// </summary>
        private int GetResourceCount(List<ActiveBagResource> resources, int resourceId = -1)
        {
            //Loop through bagged resources again to get remaining.
            int count = 0;
            foreach (ActiveBagResource br in resources)
            {
                ActiveBag bag = Bags[br.BagIndex];
                int slotCount = bag.Slots[br.SlotIndex].Quantity;

                int slotId = bag.Slots[br.SlotIndex].ResourceId;
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
                ModifiyResourceQuantity(rq.ResourceId, -rq.Quantity);

            ResourceQuantity recipeResult = r.GetResult();
            ModifiyResourceQuantity(recipeResult.ResourceId, recipeResult.Quantity, sendToClient);
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
                    List<ActiveBagResource> resources;
                    if (!BaggedResources.TryGetValue(rq.ResourceId, out resources))
                    {
                        resources = CollectionCaches<ActiveBagResource>.RetrieveList();
                        BaggedResources.Add(rq.ResourceId, resources);
                    }

                    resources.Add(new ActiveBagResource(i, z));
                }
            }
        }

        /// <summary>
        /// Moves a resource from one bag slot to another.
        /// </summary>
        /// <param name="from">Information on where the resource is coming from.</param>
        /// <param name="to">Information on where the resource is going.</param>
        /// <returns>True if the move was successful.</returns>
        public bool MoveResource(ActiveBagResource from, ActiveBagResource to)
        {
            //Did not move...
            if (from.BagIndex == to.BagIndex && from.SlotIndex == to.SlotIndex)
                return false;

            ResourceQuantity fromRq;
            ResourceQuantity toRq;
            bool fromFound = GetResourceQuantity(from.BagIndex, from.SlotIndex, out fromRq);
            bool toFound = GetResourceQuantity(to.BagIndex, to.SlotIndex, out toRq);
            //A problem occurred when finding bag and slot.
            if (!fromFound || !toFound)
            {
                //Exploit attempt kick.
                if (base.IsServer)
                    base.Owner.Kick(KickReason.ExploitAttempt, LoggingType.Common, $"Connection Id {base.Owner.ClientId} tried to move a bag item to an invalid slot.");
                else
                    return false;
            }

            //If the to is empty just simply move.
            if (toRq.IsUnset)
            {
                SwapEntries();
            }
            //If different items in each slot they cannot be stacked, so swap.
            else if (fromRq.ResourceId != toRq.ResourceId)
            {
                SwapEntries();
            }
            //Same resource if here. Try to stack.
            else
            {
                //Since the smae resource stack limit can be from either from or to.
                IResourceData rd = _resourceManager.GetIResourceData(fromRq.ResourceId);
                int stackLimit = rd.GetStackLimit();
                //If the to or from resourcequantity is at limit already then just swap.
                if (toRq.Quantity >= stackLimit || fromRq.Quantity >= stackLimit)
                {
                    SwapEntries();
                }
                //Can move stack.
                else
                {
                    //Set the move amount to max possible amount to complete the stack, or from quantity.
                    int moveAmount = Mathf.Min((stackLimit - toRq.Quantity), fromRq.Quantity);
                    /* If move amount is less than to quantity then the full stack could
                     * not be moved. When this occurs fill to stack, and leave remaining. */
                    if (moveAmount < fromRq.Quantity)
                    {
                        Bags[to.BagIndex].Slots[to.SlotIndex] = new ResourceQuantity(toRq.ResourceId, toRq.Quantity + moveAmount);
                        int remaining = (fromRq.Quantity - moveAmount);
                        Bags[from.BagIndex].Slots[from.SlotIndex] = new ResourceQuantity(fromRq.ResourceId, remaining);
                    }
                    //If can fit all then add onto to quantity and unset from bag/slot.
                    else
                    {
                        Bags[to.BagIndex].Slots[to.SlotIndex] = new ResourceQuantity(toRq.ResourceId, toRq.Quantity + fromRq.Quantity);
                        Bags[from.BagIndex].Slots[from.SlotIndex] = new ResourceQuantity();
                    }
                }
            }

            //Invoke changes.
            OnBagSlotUpdated?.Invoke(from.BagIndex, from.SlotIndex, Bags[from.BagIndex].Slots[from.SlotIndex]);
            OnBagSlotUpdated?.Invoke(to.BagIndex, to.SlotIndex, Bags[to.BagIndex].Slots[to.SlotIndex]);

            return true;

            //Swaps the to and from entries.
            void SwapEntries()
            {
                Bags[from.BagIndex].Slots[from.SlotIndex] = toRq;
                Bags[to.BagIndex].Slots[to.SlotIndex] = fromRq;
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