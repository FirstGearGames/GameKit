
using GameKit.Core.Inventories.Bags;
using GameKit.Core.Resources;
using GameKit.Dependencies.Utilities;
using System.Collections.Generic;

namespace GameKit.Core.Inventories
{
    /// <summary>
    /// Constants related to Inventory.
    /// </summary>
    public struct InventoryDb
    {
        public List<SerializableActiveBag> ActiveBags;
        public List<SerializableResourceQuantity> HiddenResources;

        public InventoryDb(List<SerializableActiveBag> activeBags, List<SerializableResourceQuantity> hiddenResources)
        {
            ActiveBags = activeBags;
            HiddenResources = hiddenResources;
        }

        public void ResetState()
        {
            CollectionCaches<SerializableActiveBag>.StoreAndDefault(ref ActiveBags);
            CollectionCaches<SerializableResourceQuantity>.StoreAndDefault(ref HiddenResources);
        }
    }

    public static class InventoryDbExtensions
    {
        public static bool IsDefault(this InventoryDb inventoryDb)
        {
            return (inventoryDb.ActiveBags == null && inventoryDb.HiddenResources == null);
        }
    }

}