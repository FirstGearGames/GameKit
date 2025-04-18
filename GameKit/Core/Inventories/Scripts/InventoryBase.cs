using FishNet.Object;
using System.Collections.Generic;
using UnityEngine;
using GameKit.Dependencies.Utilities;
using GameKit.Core.Crafting;
using GameKit.Core.Resources;
using GameKit.Core.Inventories.Bags;
using System.Runtime.CompilerServices;

namespace GameKit.Core.Inventories
{
    /// <summary>
    /// Handles inventories of all categories for the client.
    /// </summary>
    public partial class InventoryBase : NetworkBehaviour
    {
        public delegate void RebuildBaggedResourcesDel();

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

        public delegate void ResourceUpdatedDel(SerializableResourceQuantity resourceQuantity);

        /// <summary>
        /// Called when inventory slots change with new, removed, or additionally stacked items.
        /// </summary>
        public event BagSlotUpdatedDel OnBagSlotUpdated;

        public delegate void BagSlotUpdatedDel(ActiveBag activeBag, int slotIndex, SerializableResourceQuantity resource);

        /// <summary>
        /// CategoryId for this inventory.
        /// </summary>
        public virtual ushort CategoryId { get; private set; } = InventoryConsts.UNSET_CATEGORY_ID;
        /// <summary>
        /// Quantities of each resource.
        /// Key: the resource UniqueId.
        /// Value: quantity of the resource.
        /// </summary>
        [HideInInspector]
        public Dictionary<uint, int> ResourceQuantities = new();
        /// <summary>
        /// Maximum space of all bags.
        /// </summary>        
        public int MaximumSlots
        {
            get
            {
                int total = 0;
                foreach (ActiveBag item in ActiveBags.Values)
                    total += item.MaximumSlots;

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
                foreach (ActiveBag item in ActiveBags.Values)
                    total += item.UsedSlots;

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
                foreach (ActiveBag item in ActiveBags.Values)
                    total += item.AvailableSlots;

                return total;
            }
        }
        /// <summary>
        /// All active bags for this inventory.
        /// Key: UniqueId of the bag. This is an Id given to the bag by the server for client's session.
        /// Value: ActiveBag value.
        /// </summary>
        public Dictionary<uint, ActiveBag> ActiveBags { get; private set; } = new();
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
        //TODO this should probably be under the BagManager.
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
        /// BagManager to use.
        /// </summary>
        private BagManager _bagManager;
        #endregion

        protected virtual void Awake()
        {
            Inventory inv = GetComponentInParent<Inventory>();
            inv.RegisterInventoryBase(this);
        }

        public override void OnStartNetwork()
        {
            _resourceManager = base.NetworkManager.GetInstance<ResourceManager>();
            _bagManager = base.NetworkManager.GetInstance<BagManager>();
        }

        /// <summary>
        /// Converts this inventories ActiveBags to Json.
        /// </summary>
        /// <returns></returns>
        protected List<SerializableActiveBag> ActiveBagsToSerializable()
        {
            List<SerializableActiveBag> sab = CollectionCaches<SerializableActiveBag>.RetrieveList();

            List<ActiveBag> ab = CollectionCaches<ActiveBag>.RetrieveList();
            ActiveBags.ValuesToList(ref ab);

            ab.ToSerializable(ref sab);
            CollectionCaches<ActiveBag>.Store(ab);

            return sab;
        }

        /// <summary>
        /// Sets ResourceQuantities using resources.
        /// </summary>
        private void ApplyResourceQuantities(List<SerializableActiveBag> baggedResources, List<SerializableResourceQuantity> hiddenResources)
        {
            ResourceQuantities.Clear();

            foreach (SerializableActiveBag item in baggedResources)
            {
                foreach (SerializableFilledSlot sfs in item.FilledSlots)
                {
                    ResourceQuantities.TryGetValueIL2CPP(sfs.ResourceQuantity.UniqueId, out int currentValue);
                    ResourceQuantities[sfs.ResourceQuantity.UniqueId] = (currentValue + sfs.ResourceQuantity.Quantity);
                }
            }

            foreach (SerializableResourceQuantity item in hiddenResources)
            {
                ResourceQuantities.TryGetValueIL2CPP(item.UniqueId, out int currentValue);
                ResourceQuantities[item.UniqueId] = (currentValue + item.Quantity);
            }
        }

        /// <summary>
        /// Adds a Bag using an ActiveBag.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddBag(SerializableActiveBag sab, bool sendToClient)
        {
            ActiveBag ab = new ActiveBag(sab, this, _bagManager);
            AddBag(ab, sendToClient);
        }

        /// <summary>
        /// Adds a Bag to Inventory.
        /// </summary>
        /// <param name="bag">Adds an ActiveBag for bag with no entries.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddBag(BagData bag, uint activeBagUniqueId, bool sendToClient)
        {
            int currentActiveBagsCount = ActiveBags.Count;
            if (activeBagUniqueId == InventoryConsts.UNSET_BAG_ID)
                activeBagUniqueId = ((uint)currentActiveBagsCount + 1);
            ActiveBag ab = new ActiveBag(activeBagUniqueId, this, bag, currentActiveBagsCount);
            AddBag(ab, sendToClient);
        }

        /// <summary>
        /// Adds a Bag to Inventory.
        /// </summary>
        /// <param name="activeBag">ActiveBag information to add.</param>
        public void AddBag(ActiveBag activeBag, bool sendToClient)
        {
            ActiveBags[activeBag.UniqueId] = activeBag;
            OnBagsChanged?.Invoke(true, activeBag);

            if (base.IsServerInitialized && sendToClient)
                TgtAddBag(base.Owner, activeBag.ToSerializable());
        }

        /// <summary>
        /// Adds or removes a resource quantity. Values can be negative to subtract quantity.
        /// </summary>
        /// <param name="uniqueId">Resource being modified.</param>
        /// <param name="quantity">Number of items to remove or add.</param>
        /// <param name="sendToClient">True to send the changes to the client.</param>
        /// <returns>Quantity which could not be added or removed due to space limitations or missing resources.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ModifyResourceQuantity(uint uniqueId, int quantity, bool sendToClient)
        {
            if (quantity == 0)
                return 0;

            int result;
            if (quantity > 0)
                result = AddResourceQuantity(uniqueId, (uint)quantity, sendToClient);
            else
                result = RemoveResourceQuantity(uniqueId, (uint)(quantity * -1), sendToClient);

            //If something was added or removed then save servers inventory.
            if (result != Mathf.Abs(quantity) && base.IsServerInitialized)
            {
                SerializableInventoryDb db = SaveAllInventory_Server();
                db.ResetState();
            }
            return result;
        }

        /// <summary>
        /// Adds a resource quantity to the first available existing stacks, or slots if no stacks are available.
        /// </summary>
        /// <param name="uniqueId">Resource to add.</param>
        /// <param name="qPositive">Quantity of resources to add.</param>
        /// <returns>Quantity which could not be added due to no available space.</returns>
        public int AddResourceQuantity(uint uniqueId, uint qPositive, bool sendToClient)
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

                if (sendToClient && base.IsServerInitialized)
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
                    ActiveBag bag = bagSlot.ActiveBag;
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
                    foreach (ActiveBag activeBag in ActiveBags.Values)
                    {
                        int slotsCount = activeBag.MaximumSlots;
                        for (int slotIndex = 0; slotIndex < slotsCount; slotIndex++)
                        {
                            //Already has an item.
                            if (!activeBag.Slots[slotIndex].IsUnset)
                                continue;

                            int addCount = Mathf.Min(stackLimit, quantityRemaining);
                            thisAdded += addCount;
                            quantityRemaining -= addCount;
                            activeBag.Slots[slotIndex].Update(uniqueId, addCount);
                            //Since filling an empty slot add it to bagged resources.
                            BagSlot bs = new BagSlot(activeBag, slotIndex);
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

                if (sendToClient && base.IsServerInitialized)
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

                    ActiveBag bag = bagSlot.ActiveBag;
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
        private void InvokeBagSlotUpdated(BagSlot bs)
        {
            OnBagSlotUpdated?.Invoke(bs.ActiveBag, bs.SlotIndex, bs.ActiveBag.Slots[bs.SlotIndex]);
        }

        /// <summary>
        /// Completes the process of a resource quantity being updated.
        /// </summary>
        /// <param name="uniqueId">Resource changed.</param>
        /// <param name="currentQuantity">Current quantity of the resource after the change.</param>
        private void CompleteResourceQuantityChange(uint uniqueId, int currentQuantity)
        {
            SerializableResourceQuantity rq = new SerializableResourceQuantity(uniqueId, (int)currentQuantity);
            OnResourceUpdated?.Invoke(rq);
        }

        /// <summary>
        /// Updates inventory resources using a recipe.
        /// This removes required resources while adding created.
        /// </summary>
        public void UpdateResourcesFromRecipe(RecipeData r, bool sendToClient)
        {
            //Remove needed resources first so space used is removed.
            foreach (SerializableResourceQuantity rq in r.GetRequiredResources())
                ModifyResourceQuantity(rq.UniqueId, -rq.Quantity, sendToClient);

            SerializableResourceQuantity recipeResult = r.GetResult();
            ModifyResourceQuantity(recipeResult.UniqueId, recipeResult.Quantity, sendToClient);
            OnBulkResourcesUpdated?.Invoke();
        }

        /// <summary>
        /// Iterates all bags and rebuilds bagged resources.
        /// </summary>
        protected void RebuildBaggedResources()
        {
            BaggedResources.Clear();

            foreach (ActiveBag activeBag in ActiveBags.Values)
            {
                for (int z = 0; z < activeBag.Slots.Length; z++)
                {
                    SerializableResourceQuantity rq = activeBag.Slots[z];
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

                    resources.Add(new BagSlot(activeBag, z));
                }
            }
        }

        /// <summary>
        /// Outputs resource quantities of a BagSlot.
        /// </summary>
        /// <returns>True if the return was successful.</returns>
        public bool GetResourceQuantity(BagSlot bs, out SerializableResourceQuantity rq)
        {
            return GetResourceQuantity(bs.ActiveBag.UniqueId, bs.SlotIndex, out rq);
        }

        /// <summary>
        /// Gets a ResourceQuantity using an ActiveBag.UniqueId, and SlotIndex.
        /// </summary>
        /// <returns>True if the bag and slot index was valid.</returns>
        public bool GetResourceQuantity(uint bagUniqueId, int slotIndex, out SerializableResourceQuantity rq)
        {
            //Invalid information.
            if (!IsValidBagSlot(bagUniqueId, slotIndex))
            {
                rq = default;
                return false;
            }
            //Valid.
            else
            {
                rq = ActiveBags[bagUniqueId].Slots[slotIndex];
                return true;
            }
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
        /// Returns if a slot exists.
        /// </summary>
        /// <param name="activeBagUniqueId">Bag index to check.</param>
        /// <param name="slotIndex">Slot index to check.</param>
        /// <returns></returns>
        private bool IsValidBagSlot(uint activeBagUniqueId, int slotIndex)
        {
            if (!ActiveBags.TryGetValue(activeBagUniqueId, out ActiveBag ab))
                return false;
            if (slotIndex < 0 || slotIndex >= ab.Slots.Length)
                return false;

            //All conditions pass.
            return true;
        }
    }
}