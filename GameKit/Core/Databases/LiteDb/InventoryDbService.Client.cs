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

    public partial class InventoryDbService : IInventoryDbService_Client
    {
        /// <summary>
        /// Database local client uses.
        /// </summary>
        private LiteDatabase _databaseClient;
        /// <summary>
        /// Database BsonId used by the local client.
        /// </summary>
        private const ulong LOCAL_CLIENT_BSONID = 0;

        private void InitializeState_Client()
        {
            string path = $"{Path.Combine(Application.persistentDataPath, "Inventory_Client.db")}";
            _databaseClient = new LiteDatabase(path);
        }

        private void ResetState_Client()
        {
            _databaseClient.Dispose();
            _databaseClient = null;
        }


        [Client(Logging = LoggingType.Off)]
        public List<SerializableActiveBag> GetSortedInventory(uint categoryId)
        {
            if (!GetCollection<SerializableActiveBagContainer>(_databaseClient, true, out ILiteCollection<SerializableActiveBagContainer> collection))
                return default;

            SerializableActiveBagContainer container = collection.FindById(LOCAL_CLIENT_BSONID);
            return container.Item;
        }

        [Client(Logging = LoggingType.Off)]
        public void SetSortedInventory(InventoryBase inventoryBase, List<SerializableActiveBag> sortedInventory)
        {
            SerializableActiveBagContainer container = new SerializableActiveBagContainer(sortedInventory);
            if (!GetCollection<SerializableActiveBagContainer>(_databaseClient, true, out ILiteCollection<SerializableActiveBagContainer> collection))
                return;

            collection.Upsert(LOCAL_CLIENT_BSONID, container);
        }

}


}