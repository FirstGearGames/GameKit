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
        #endregion

        public override void OnStartNetwork()
        {
            _resourceManager = base.NetworkManager.GetInstance<ResourceManager>();
        }

        /// <summary>
        /// Adds a Bag to Inventory.
        /// </summary>
        /// <param name="bag">Adds an ActiveBag for bag with no entries.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddBag(InventoryBase inventoryBase, BagData bag, uint activeBagUniqueId = InventoryConsts.UNSET_BAG_ID, bool sendToClient = true)
            => inventoryBase.AddBag(bag, activeBagUniqueId, sendToClient);

        /// <summary>
        /// Adds a Bag to Inventory.
        /// </summary>
        /// <param name="activeBag">ActiveBag information to add.</param>
        public void AddBag(InventoryBase inventoryBase, ActiveBag activeBag, bool sendToClient = true)
            =>  inventoryBase.AddBag(activeBag, sendToClient);

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

    }

}