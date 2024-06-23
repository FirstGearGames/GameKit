
using FishNet.Managing;
using GameKit.Core.Inventories;
using LiteDB;
using System.IO;
using UnityEngine;

namespace GameKit.Core.Databases.LiteDb
{

    public class InventoryDbService : IInventoryDbService
    {
        public static InventoryDbService Instance { get; private set; } = new InventoryDbService();
        private LiteDatabase _database;

        public InventoryDbService()
        {
            Initialize();
        }

        ~InventoryDbService()
        {
            _database = null;
            Instance = null;
            _database.Dispose();
        }

        private void Initialize()
        {
            if (DatabaseExist(false))
                return;

            BsonMapper mapper = BsonMapper.Global;
            mapper.IncludeFields = true;
            mapper.TrimWhitespace = false;
            mapper.EmptyStringToNull = false;

            string path = $"{Path.Combine(Application.persistentDataPath, "GameKit.db")}";
            _database = new LiteDatabase(path);
        }

        private bool DatabaseExist(bool error)
        {
            if (_database == null)
            {
                if (error)
                    NetworkManagerExtensions.LogError($"Database does not exist.");
                return false;
            }

            return true;
        }

        public SerializableInventoryDb GetInventory(uint clientUniqueId)
        {
            if (!DatabaseExist(true))
                return default;

            ILiteCollection<SerializableInventoryDb> collection = _database.GetCollection<SerializableInventoryDb>();
            SerializableInventoryDb result = collection.FindById((ulong)clientUniqueId);
            //It's okay if inventory is default.
            return result;
        }

        public void SetInventory(uint clientUniqueId, SerializableInventoryDb inventory)
        {
            if (!DatabaseExist(true))
                return;

            ILiteCollection<SerializableInventoryDb> collection = _database.GetCollection<SerializableInventoryDb>();
            if (collection == null)
                NetworkManagerExtensions.LogError($"{nameof(SerializableInventoryDb)} collection could not be found.");
            else
                collection.Upsert((ulong)clientUniqueId, inventory);
        }
    }


}