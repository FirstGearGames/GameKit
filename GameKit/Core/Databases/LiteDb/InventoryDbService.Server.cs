using FishNet.Managing;
using FishNet.Managing.Logging;
using FishNet.Object;
using GameKit.Core.Inventories;
using GameKit.Core.Inventories.Bags;
using LiteDB;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace GameKit.Core.Databases.LiteDb
{

    public partial class InventoryDbService : IInventoryDbService_Server
    {
        private LiteDatabase _databaseServer;

        private void InitializeState_Server()
        {
            string path = $"{Path.Combine(Application.persistentDataPath, "Inventory_Server.db")}";
            _databaseServer = new LiteDatabase(path);
        }

        private void ResetState_Server()
        {
            _databaseServer.Dispose();
            _databaseServer = null;
        }

        [Server(Logging = LoggingType.Off)]
        public SerializableInventoryDb GetInventory(uint clientUniqueId, uint categoryId)
        {
            if (!GetCollection<SerializableInventoryDb>(_databaseServer, true, out ILiteCollection<SerializableInventoryDb> collection))
                return default;

            SerializableInventoryDb result = collection.FindById((ulong)clientUniqueId);
            return result;
        }

        [Server(Logging = LoggingType.Off)]
        public void SetInventory(uint clientUniqueId, InventoryBase inventoryBase, SerializableInventoryDb inventory)
        {
            if (!GetCollection<SerializableInventoryDb>(_databaseServer, true, out ILiteCollection<SerializableInventoryDb> collection))
                return;

            collection.Upsert((ulong)clientUniqueId, inventory);
        }

        [Server(Logging = LoggingType.Off)]
        public List<SerializableActiveBag> GetSortedInventory(uint clientUniqueId, uint categoryId)
        {
            if (!GetCollection<SerializableActiveBagContainer>(_databaseServer, true, out ILiteCollection<SerializableActiveBagContainer> collection))
                return default;

            SerializableActiveBagContainer container = collection.FindById((ulong)clientUniqueId);
            return container.Item;
        }

        [Server(Logging = LoggingType.Off)]
        public void SetSortedInventory(uint clientUniqueId, InventoryBase inventoryBase, List<SerializableActiveBag> sortedInventory)
        {
            SerializableActiveBagContainer container = new SerializableActiveBagContainer(sortedInventory);
            if (!GetCollection<SerializableActiveBagContainer>(_databaseServer, true, out ILiteCollection<SerializableActiveBagContainer> collection))
                return;

            collection.Upsert((ulong)clientUniqueId, container);
        }
    }


}