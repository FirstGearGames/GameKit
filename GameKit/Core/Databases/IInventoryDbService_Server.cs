using GameKit.Core.Inventories;
using GameKit.Core.Inventories.Bags;
using System.Collections.Generic;

namespace GameKit.Core.Databases
{

    public interface IInventoryDbService_Server
    {
        SerializableInventoryDb GetInventory(uint clientUniqueId);
        void SetInventory(uint clientUniqueId, SerializableInventoryDb inventory);
        List<SerializableActiveBag> GetSortedInventory(uint clientUniqueId);
        void SetSortedInventory(uint clientUniqueId, List<SerializableActiveBag> sortedInventory);

    }

}