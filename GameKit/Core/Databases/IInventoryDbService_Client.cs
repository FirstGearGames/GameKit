using GameKit.Core.Inventories;
using GameKit.Core.Inventories.Bags;
using System.Collections.Generic;

namespace GameKit.Core.Databases
{

    public interface IInventoryDbService_Client
    {
        List<SerializableActiveBag> GetSortedInventory(uint categoryId);
        void SetSortedInventory(InventoryBase inventoryBase, List<SerializableActiveBag> sortedInventory);

    }

}