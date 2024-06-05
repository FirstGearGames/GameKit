using GameKit.Core.Inventories;

namespace GameKit.Core.Databases
{

    public interface IInventoryDbService
    {
        SerializableInventoryDb GetInventory(uint clientUniqueId);
        void SetInventory(uint clientUniqueId, SerializableInventoryDb inventory);
    }

}