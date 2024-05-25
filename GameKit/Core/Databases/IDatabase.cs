using GameKit.Core.Inventories;

namespace GameKit.Core.Databases
{

    public interface IInventoryDbService
    {
        InventoryDb GetInventory(uint clientUniqueId);
        void SetInventory(uint clientUniqueId, InventoryDb inventory);
    }

}