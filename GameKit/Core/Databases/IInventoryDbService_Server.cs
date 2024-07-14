using GameKit.Core.Inventories;
using GameKit.Core.Inventories.Bags;
using System.Collections.Generic;

namespace GameKit.Core.Databases
{

    public interface IInventoryDbService_Server
    {
        SerializableInventoryDb GetInventory(uint clientUniqueId, uint categoryId);
        void SetInventory(uint clientUniqueId, InventoryBase inventoryBase, SerializableInventoryDb inventory);
        List<SerializableActiveBag> GetSortedInventory(uint clientUniqueId, uint categoryId);
        void SetSortedInventory(uint clientUniqueId, InventoryBase inventoryBase, List<SerializableActiveBag> sortedInventory);

    }

}