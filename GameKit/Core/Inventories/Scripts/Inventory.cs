using FishNet.Object;
using System.Collections.Generic;
using UnityEngine;
using GameKit.Dependencies.Utilities;
using GameKit.Core.Crafting;
using GameKit.Core.Resources;
using GameKit.Core.Inventories.Bags;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace GameKit.Core.Inventories
{

    public partial class Inventory : NetworkBehaviour
    {
        #region Types.
        /// <summary>
        /// Inventory without any sorting.
        /// </summary>
        private struct SerializableUnsortedInventory
        {
            /// <summary>
            /// Bags the client has.
            /// </summary>
            public List<SerializableBagData> Bags;
            /// <summary>
            /// Resources across all bags the client has.
            /// </summary>
            public List<SerializableResourceQuantity> BaggedResourceQuantities;
            /// <summary>
            /// Resources the client has which are hidden.
            /// </summary>
            public List<SerializableResourceQuantity> HiddenResourceQuantities;

            public SerializableUnsortedInventory(List<SerializableBagData> bags, List<SerializableResourceQuantity> resourceQuantities, List<SerializableResourceQuantity> hiddenResourceQuantities)
            {
                Bags = bags;
                BaggedResourceQuantities = resourceQuantities;
                HiddenResourceQuantities = hiddenResourceQuantities;
            }
        }
        #endregion

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
                for (int i = 0; i < ActiveBags.Count; i++)
                    total += ActiveBags[i].MaximumSlots;

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
                for (int i = 0; i < ActiveBags.Count; i++)
                    total += ActiveBags[i].UsedSlots;

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
                for (int i = 0; i < ActiveBags.Count; i++)
                    total += ActiveBags[i].AvailableSlots;

                return total;
            }
        }
        /// <summary>
        /// All active bags for this inventory.
        /// </summary>
        public List<ActiveBag> ActiveBags { get; private set; } = new List<ActiveBag>();
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



        private const string BAGGED_INVENTORY_FILENAME = "inventory_bagged.json";
        private const string HIDDEN_INVENTORY_FILENAME = "inventory_bagged.json";
        private const string SORTED_INVENTORY_FILENAME = "inventory_sorted.json";


        public override void OnStartNetwork()
        {
            _resourceManager = base.NetworkManager.GetInstance<ResourceManager>();
            _bagManager = base.NetworkManager.GetInstance<BagManager>();
        }

        /// <summary>
        /// Called after receiving a crafting result.
        /// </summary>
        /// <param name="r">Recipe the result is for.</param>
        /// <param name="result">The crafting result.</param>
        /// <param name="asServer">True if callback is for server.</param>
        private void Crafter_OnCraftingResult(RecipeData r, CraftingResult result, bool asServer)
        {
            if (result == CraftingResult.Completed)
                UpdateResourcesFromRecipe(r, asServer);
        }


        /// <summary>
        /// Uses serializable data to set inventory.
        /// </summary>
        /// <returns>True if sorted inventory was changed due to errors.</returns>
        private bool ApplyInventory(SerializableUnsortedInventory unsortedInv, List<SerializableActiveBag> sortedInv, bool sendToClient)
        {
            //TODO: For server save types in a database rather than JSON.
            ActiveBags.Clear();
            HiddenResources.Clear();

            /* ResourceQuantities which are handled inside the users saved inventory
            * are removed from unsortedInventory. Any ResourceQuantities remaining in unsorted
            * inventory are added to whichever slots are available in the users inventory.
            * 
            * If a user doesn't have the bag entirely which is in their saved inventory
            * then it's skipped over. This will result in any skipped entries filling slots
            * as described above. */

            //TODO: convert linq lookups to for loops for quicker iteration.

            //Make resources into dictionary for quicker lookups.
            //Resource UniqueIds and quantity of each.
            Dictionary<uint, int> rqsDict = CollectionCaches<uint, int>.RetrieveDictionary();
            foreach (SerializableResourceQuantity item in unsortedInv.ResourceQuantities)
                rqsDict[item.UniqueId] = item.Quantity;

            /* First check if unsortedInv contains all the bags used
             * in sortedInv. If sortedInv says a bag is used that the client
             * does not have then the bag is unset from sorted which will
             * cause the resources to be placed wherever available. */
            for (int i = 0; i < sortedInv.Count; i++)
            {
                int bagIndex = unsortedInv.Bags.FindIndex(x => x.UniqueId == sortedInv[i].BagUniqueId);
                //Bag not found, remove bag from sortedInventory.
                if (bagIndex == -1)
                {
                    sortedInv.RemoveAt(i);
                    i--;
                }
                //Bag found, remove from unsorted so its not used twice.
                else
                {
                    unsortedInv.Bags.RemoveAt(bagIndex);
                }
            }

            /* Check if unsortedInv contains the same resources as
             * sortedinv. This uses the same approach as above where
             * inventory items which do not exist in unsorted are removed
             * from sorted. */
            for (int i = 0; i < sortedInv.Count; i++)
            {
                for (int z = 0; z < sortedInv[i].FilledSlots.Count; z++)
                {
                    FilledSlot fs = sortedInv[i].FilledSlots[z];
                    rqsDict.TryGetValue(fs.ResourceQuantity.UniqueId, out int unsortedCount);
                    /* Subtract sortedCount from unsortedCount. If the value is negative
                     * then the result must be removed from unsortedCount. Additionally,
                     * remove the resourceId from rqsDict since it no longer has value. */
                    int quantityDifference = (unsortedCount - fs.ResourceQuantity.Quantity);
                    if (quantityDifference < 0)
                    {
                        fs.ResourceQuantity.Quantity += quantityDifference;
                        sortedInv[i].FilledSlots[z] = fs;
                    }

                    //If there is no more quantity left then remove from unsorted.
                    if (quantityDifference <= 0)
                        rqsDict.Remove(fs.ResourceQuantity.UniqueId);
                    //Still some quantity left, update unsorted.
                    else
                        rqsDict[fs.ResourceQuantity.UniqueId] = quantityDifference;
                }
            }

            //Add starting with sorted bags.
            foreach (SerializableActiveBag sab in sortedInv)
            {
                ActiveBag ab = sab.ToNative(_bagManager);
                AddBag(ab);
            }

            //Add remaining bags from unsorted.
            foreach (SerializableBagData sb in unsortedInv.Bags)
            {
                BagData b = _bagManager.GetBagData(sb.UniqueId);
                AddBag(b);
            }

            /* This builds a cache of resources currently in the inventory.
             * Since ActiveBags were set without allowing rebuild to save perf
             * it's called here after all bags are added. */
            RebuildBaggedResources();
            //Add remaining resources to wherever they fit.
            foreach (KeyValuePair<uint, int> item in rqsDict)
                ModifiyResourceQuantity(item.Key, item.Value, false);


            if (sendToClient)
                TgtApplyInventory(base.Owner, unsortedInv, sortedInv);

            int rqsDictCount = rqsDict.Count;
            CollectionCaches<uint, int>.Store(rqsDict);
            /* If there were unsorted added then save clients new
            * layout after everything was added. */
            return (unsortedInv.Bags.Count > 0 || rqsDictCount > 0);
        }

        /// <summary>
        /// Converts this inventories ActiveBags to Json.
        /// </summary>
        /// <returns></returns>
        private string ActiveBagsToJson()
        {
            string result = JsonConvert.SerializeObject(ActiveBags.ToSerializable(), Formatting.Indented);
            return result;
        }

        /// <summary>
        /// Adds a Bag using an ActiveBag.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddBag(SerializableActiveBag sab, bool sendToClient = true)
        {
            BagData bd = _bagManager.GetBagData(sab.BagUniqueId);
            ActiveBag ab = new(bd, sab.Index, sab.FilledSlots.GetResourceQuantity(bd.Space));
            AddBag(ab, sendToClient);
        }

        /// <summary>
        /// Adds a Bag to Inventory.
        /// </summary>
        /// <param name="bag">Adds an ActiveBag for bag with no entries.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddBag(BagData bag, bool sendToClient = true)
        {
            ActiveBag ab = new ActiveBag(bag);
            ab.SetIndex(ActiveBags.Count);
            AddBag(ab, sendToClient);
        }

        /// <summary>
        /// Adds a Bag to Inventory.
        /// </summary>
        /// <param name="activeBag">ActiveBag information to add.</param>
        public void AddBag(ActiveBag activeBag, bool sendToClient = true)
        {
            ActiveBags.Insert(activeBag.Index, activeBag);
            OnBagsChanged?.Invoke(true, activeBag);

            if (base.IsServerInitialized && sendToClient)
                TgtAddBag(base.Owner, activeBag.ToSerializable());
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ModifiyResourceQuantity(uint uniqueId, int quantity, bool sendToClient = true)
        {
            if (quantity == 0)
                return 0;

            int result;
            if (quantity > 0)
                result = AddResourceQuantity(uniqueId, (uint)quantity, sendToClient);
            else
                result = RemoveResourceQuantity(uniqueId, (uint)(quantity * -1), sendToClient);

            //If something was added or removed then save unsorted.
            if (result != Mathf.Abs(quantity) && base.IsServerInitialized)
                SaveInventoryUnsorted_Server();

            return result;
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
                    ActiveBag bag = ActiveBags[bagSlot.BagIndex];
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
                    for (int bagIndex = 0; bagIndex < ActiveBags.Count; bagIndex++)
                    {
                        ActiveBag bag = ActiveBags[bagIndex];

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

                    ActiveBag bag = ActiveBags[bagSlot.BagIndex];
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
            ActiveBag brBag = ActiveBags[bagIndex];
            OnBagSlotUpdated?.Invoke(bagIndex, slotIndex, brBag.Slots[slotIndex]);
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

            for (int i = 0; i < ActiveBags.Count; i++)
            {
                ActiveBag bag = ActiveBags[i];
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
        /// Returns if a slot exists.
        /// </summary>
        /// <param name="bagIndex">Bag index to check.</param>
        /// <param name="slotIndex">Slot index to check.</param>
        /// <returns></returns>
        private bool IsValidBagSlot(int bagIndex, int slotIndex)
        {
            if (bagIndex < 0 || bagIndex >= ActiveBags.Count)
                return false;
            if (slotIndex < 0 || slotIndex >= ActiveBags[bagIndex].Slots.Length)
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
                rq = ActiveBags[bagIndex].Slots[slotIndex];
                return true;
            }
        }
    }

}