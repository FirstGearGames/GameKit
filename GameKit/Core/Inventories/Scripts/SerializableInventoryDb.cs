using GameKit.Core.Inventories.Bags;
using GameKit.Core.Resources;
using GameKit.Dependencies.Utilities;
using System.Collections.Generic;

namespace GameKit.Core.Inventories
{
    /// <summary>
    /// Constants related to Inventory.
    /// </summary>
    [System.Serializable]
    public struct SerializableInventoryDb
    {
        public List<SerializableActiveBag> ActiveBags;
        public List<SerializableResourceQuantity> HiddenResources;

        public SerializableInventoryDb(List<SerializableActiveBag> activeBags, List<SerializableResourceQuantity> hiddenResources)
        {
            ActiveBags = activeBags;
            HiddenResources = hiddenResources;
        }

        public void ResetState()
        {
            CollectionCaches<SerializableActiveBag>.Store(ActiveBags);
            CollectionCaches<SerializableResourceQuantity>.Store(HiddenResources);

            ActiveBags = null;
            HiddenResources = null;
        }
    }

    public static class InventoryDbExtensions
    {
        public static bool IsDefault(this SerializableInventoryDb inventoryDb)
        {
            return (inventoryDb.ActiveBags == null && inventoryDb.HiddenResources == null);
        }
    }

}