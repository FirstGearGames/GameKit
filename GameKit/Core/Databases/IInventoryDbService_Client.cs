using GameKit.Core.Inventories.Bags;
using System.Collections.Generic;

namespace GameKit.Core.Databases
{

    public interface IInventoryDbService_Client
    {
        List<SerializableActiveBag> GetSortedInventory();
        void SetSortedInventory(List<SerializableActiveBag> sortedInventory);

    }

}