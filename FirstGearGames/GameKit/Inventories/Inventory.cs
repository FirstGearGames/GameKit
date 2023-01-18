using GameKit.Crafting;
using GameKit.Resources;
using GameKit.Resources.Managers;
using FishNet.Object;
using System;
using System.Collections.Generic;
using UnityEngine;
using TriInspector;
using FishNet.Managing.Server;
using FishNet.Managing.Logging;

namespace GameKit.Inventories
{

    public partial class Inventory : NetworkBehaviour
    {
        #region Public.
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
        /// All bags for this inventory.
        /// </summary>
        public List<Bag> Bags { get; private set; } = new List<Bag>();
        /// <summary>
        /// ResourceIds and bag slots they occupy.
        /// </summary>
        public Dictionary<int, List<BaggedResource>> BaggedResources { get; private set; } = new();
        #endregion

        #region Serialized.
        /// <summary>
        /// Default bags to add.
        /// </summary>
        [PropertyTooltip("Default bags to add.")]
        [Range(1, 30)]
        [SerializeField]
        private int[] _defaultBagSizes = new int[] { 5 };
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
            foreach (int item in _defaultBagSizes)
            {
                Bag b = new Bag(item);
                Bags.Add(b);
            }

            Crafter crafter = GetComponent<Crafter>();
            crafter.OnCraftingResult += Crafter_OnCraftingResult;
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            _resourceManager = base.NetworkManager.GetInstance<ResourceManager > ();
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
                UpdateResourcesFromRecipe(r);
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
        /// <returns>Quantity which could not be added or removed due to space limitations or missing resources.</returns>
        public int ModifiyResourceQuantity(int resourceId, int quantity)
        {
            if (quantity == 0)
                return 0;

            if (quantity < 0)
                return RemoveResourceQuantity(resourceId, (uint)(quantity * -1));
            else
                return AddResourceQuantity(resourceId, (uint)quantity);
        }

        /// <summary>
        /// Removes a resource from the first available stacks regardless of the stack quantity.
        /// </summary>
        /// <param name="resourceId">Resource to remove.</param>
        /// <param name="quantity">Amount to remove. Value must be positive.</param>
        /// <returns>Quantity which could not be removed due to missing resources.</returns>
        private int RemoveResourceQuantity(int resourceId, uint qPositive)
        {
            int quantity = (int)qPositive;

            int current;
            //Does not exist and removing, nothing further to be done.
            if (!ResourceQuantities.TryGetValue(resourceId, out current))
                return 0;

            List<BaggedResource> baggedResources;
            //If none in bags then return full quantity as what was unable to be removed.
            if (!BaggedResources.TryGetValue(resourceId, out baggedResources))
                return quantity;

            int removed = 0;
            for (int bagResourceIndex = 0; bagResourceIndex < baggedResources.Count; bagResourceIndex++)
            {
                BaggedResource br = baggedResources[bagResourceIndex];

                Bag bag = Bags[br.BagIndex];
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
                void InvokeBagSlotUpdated(BaggedResource br)
                {
                    int bagIndex = br.BagIndex;
                    int slotIndex = br.SlotIndex;
                    Bag brBag = Bags[bagIndex];
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

            CompleteResourceQuantityChange(resourceId, resourceCount);
            return (quantity - removed);
        }

        /// <summary>
        /// Adds a resource quantity to the first available existing stacks, or slots if no stacks are available.
        /// </summary>
        /// <param name="resourceId">Resource to add.</param>
        /// <param name="qPositive">Quantity of resources to add.</param>
        /// <returns>Quantity which could not be added due to no available space.</returns>
        private int AddResourceQuantity(int resourceId, uint qPositive)
        {
            int quantity = (int)qPositive;

            IResourceData rd = _resourceManager.GetIResourceData(resourceId);
            int stackLimit = rd.GetStackLimit();

            List<BaggedResource> baggedResources;
            //If none are bagged yet.
            if (!BaggedResources.TryGetValue(resourceId, out baggedResources))
            {
                /* If there's no available slots then
                 * none can be bagged. Return full quantity
                 * as not added. */
                if (AvailableSlots == 0)
                    return quantity;

                //Otherwise add new bagged resources because at least one will be added.
                baggedResources = new List<BaggedResource>();
                BaggedResources.Add(resourceId, baggedResources);
            }

            //Check if can be added to existing stacks.
            for (int i = 0; i < baggedResources.Count; i++)
            {
                BaggedResource br = baggedResources[i];

                Bag bag = Bags[br.BagIndex];
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
                    Bag bag = Bags[bagIndex];

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
                        baggedResources.Add(new BaggedResource(bagIndex, slotIndex));
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
            return quantity;
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
        private int GetResourceCount(List<BaggedResource> resources, int resourceId = -1)
        {
            //Loop through bagged resources again to get remaining.
            int count = 0;
            foreach (BaggedResource br in resources)
            {
                Bag bag = Bags[br.BagIndex];
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
        private void UpdateResourcesFromRecipe(IRecipe r)
        {
            //Remove needed resources first so space used is removed.
            foreach (ResourceQuantity rq in r.GetRequiredResources())
                ModifiyResourceQuantity(rq.ResourceId, -rq.Quantity);

            ResourceQuantity recipeResult = r.GetResult();
            ModifiyResourceQuantity(recipeResult.ResourceId, recipeResult.Quantity);
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
                Bag bag = Bags[i];
                for (int z = 0; z < bag.Slots.Length; z++)
                {
                    ResourceQuantity rq = bag.Slots[z];
                    //Do not need to add if unset.
                    if (rq.IsUnset)
                        continue;

                    //Debug.Log($"Bag {i}, Slot {z}, ResourceId {rq.ResourceId}");
                    List<BaggedResource> resources;
                    if (!BaggedResources.TryGetValue(rq.ResourceId, out resources))
                    {
                        resources = new();
                        BaggedResources.Add(rq.ResourceId, resources);
                    }

                    resources.Add(new BaggedResource(i, z));
                }
            }
        }

        /// <summary>
        /// Moves a resource from one bag slot to another.
        /// </summary>
        /// <param name="from">Information on where the resource is coming from.</param>
        /// <param name="to">Information on where the resource is going.</param>
        public void MoveResource(BaggedResource from, BaggedResource to)
        {
            //Did not move...
            if (from.BagIndex == to.BagIndex && from.SlotIndex == to.SlotIndex)
                return;

            //From or to information is invalid.
            if (!IsValidBagSlot(from.BagIndex, from.SlotIndex) || !IsValidBagSlot(to.BagIndex, to.SlotIndex))
            {
                //Exploit attempt kick.
                if (base.IsServer)
                    base.Owner.Kick(KickReason.ExploitAttempt, LoggingType.Common, $"Connection Id {base.Owner.ClientId} tried to move a bag item to an invalid slot.");
            }

            Debug.Log(from.SlotIndex + ", " + to.SlotIndex);

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
    }


}