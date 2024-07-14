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
        public delegate void BagsChangedDel(InventoryBase inventoryBase, bool added, ActiveBag bag);
        /// <summary>
        /// Called when resources cannot be added due to a full inventory.
        /// </summary>
        public event InventoryFullDel OnInventoryFull;
        public delegate void InventoryFullDel(InventoryBase inventoryBase, IEnumerable<ResourceData> resourcesNotAdded);
        /// <summary>
        /// Called when multiple resources have updated.
        /// </summary>
        public event BulkResourcesUpdatedDel OnBulkResourcesUpdated;
        public delegate void BulkResourcesUpdatedDel(InventoryBase inventoryBase);
        /// <summary>
        /// Called when a single resource is updated.
        /// </summary>
        public event ResourceUpdatedDel OnResourceUpdated;
        public delegate void ResourceUpdatedDel(InventoryBase inventoryBase, SerializableResourceQuantity resourceQuantity);
        /// <summary>
        /// Called when inventory slots change with new, removed, or additionally stacked items.
        /// </summary>
        public event BagSlotUpdatedDel OnBagSlotUpdated;
        public delegate void BagSlotUpdatedDel(InventoryBase inventoryBase, ActiveBag activeBag, int slotIndex, SerializableResourceQuantity resource);
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
        /// Converts this inventories ActiveBags to Json.
        /// </summary>
        /// <returns></returns>
        private List<SerializableActiveBag> ActiveBagsToSerializable()
        {
            List<SerializableActiveBag> sab = CollectionCaches<SerializableActiveBag>.RetrieveList();

            List<ActiveBag> ab = CollectionCaches<ActiveBag>.RetrieveList();
            ActiveBags.ValuesToList(ref ab);

            ab.ToSerializable(ref sab);
            CollectionCaches<ActiveBag>.Store(ab);

            return sab;
        }

        /// <summary>
        /// Adds a Bag using an ActiveBag.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddBag(SerializableActiveBag sab, bool sendToClient = true)
        {
            ActiveBag ab = new ActiveBag(sab, _bagManager);
            AddBag(ab, sendToClient);
        }

        /// <summary>
        /// Adds a Bag to Inventory.
        /// </summary>
        /// <param name="bag">Adds an ActiveBag for bag with no entries.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddBag(BagData bag, uint activeBagUniqueId = InventoryConsts.UNSET_BAG_ID, bool sendToClient = true)
        {
            int currentActiveBagsCount = ActiveBags.Count;
            if (activeBagUniqueId == InventoryConsts.UNSET_BAG_ID)
                activeBagUniqueId = ((uint)currentActiveBagsCount + 1);
            ActiveBag ab = new ActiveBag(activeBagUniqueId, bag, currentActiveBagsCount);
            AddBag(ab, sendToClient);
        }

        /// <summary>
        /// Adds a Bag to Inventory.
        /// </summary>
        /// <param name="activeBag">ActiveBag information to add.</param>
        public void AddBag(ActiveBag activeBag, bool sendToClient = true)
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
        public int ModifyResourceQuantity(InventoryBase inventoryBase, uint uniqueId, int quantity, bool sendToClient = true)
            => inventoryBase.ModifyResourceQuantity(uniqueId, quantity, sendToClient);

        /// <summary>
        /// Invokes that a bag slot was updated for the supplied bagSlot.
        /// </summary>
        private void InvokeBagSlotUpdated(InventoryBase inventoryBase, BagSlot bs)
        {
            OnBagSlotUpdated?.Invoke(inventoryBase, bs.ActiveBag, bs.SlotIndex, bs.ActiveBag.Slots[bs.SlotIndex]);
        }

        /// <summary>
        /// Completes the process of a resource quantity being updated.
        /// </summary>
        /// <param name="uniqueId">Resource changed.</param>
        /// <param name="currentQuantity">Current quantity of the resource after the change.</param>
        private void CompleteResourceQuantityChange(InventoryBase inventoryBase, uint uniqueId, int currentQuantity)
        {
            SerializableResourceQuantity rq = new SerializableResourceQuantity(uniqueId, (int)currentQuantity);
            OnResourceUpdated?.Invoke(inventoryBase, rq);
        }

        /// <summary>
        /// Updates inventory resources using a recipe.
        /// This removes required resources while adding created.
        /// </summary>
        private void UpdateResourcesFromRecipe(InventoryBase inventoryBase, RecipeData r, bool sendToClient = true)
        {
            inventoryBase.UpdateResourcesFromRecipe(r, sendToClient);
            OnBulkResourcesUpdated?.Invoke(inventoryBase);
        }

        /// <summary>
        /// Outputs resource quantities of a BagSlot.
        /// </summary>
        /// <returns>True if the return was successful.</returns>
        public bool GetResourceQuantity(InventoryBase inventoryBase, BagSlot bs, out SerializableResourceQuantity rq)
            => inventoryBase.GetResourceQuantity(bs, out rq);

        /// <summary>
        /// Gets a ResourceQuantity using an ActiveBag.UniqueId, and SlotIndex.
        /// </summary>
        /// <returns>True if the bag and slot index was valid.</returns>
        public bool GetResourceQuantity(InventoryBase inventoryBase, uint bagUniqueId, int slotIndex, out SerializableResourceQuantity rq)
            => inventoryBase.GetResourceQuantity(bagUniqueId, slotIndex, out rq);

        /// <summary>
        /// Returns the held quantity of a resource.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public int GetResourceQuantity(InventoryBase inventoryBase, uint uniqueId)
            => inventoryBase.GetResourceQuantity(uniqueId);


        /// <summary>
        /// Returns if a slot exists.
        /// </summary>
        /// <param name="activeBagUniqueId">Bag index to check.</param>
        /// <param name="slotIndex">Slot index to check.</param>
        /// <returns></returns>
        private bool IsValidBagSlot(InventoryBase inventoryBase, uint activeBagUniqueId, int slotIndex)
        {
            if (!inventoryBase.ActiveBags.TryGetValue(activeBagUniqueId, out ActiveBag ab))
                return false;
            if (slotIndex < 0 || slotIndex >= ab.Slots.Length)
                return false;

            //All conditions pass.
            return true;
        }

    }

}