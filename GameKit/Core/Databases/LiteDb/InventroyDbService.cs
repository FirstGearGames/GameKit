
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
        }

        private void Initialize()
        {
            if (DatabaseExist(false))
                return;

            string path = Application.persistentDataPath;
            _database = new LiteDatabase($"{Path.Combine(path, "GameKit.db")}");

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

        public InventoryDb GetInventory(uint clientUniqueId)
        {
            if (!DatabaseExist(true))
                return default;

            ILiteCollection<InventoryDb> collection = _database.GetCollection<InventoryDb>();
            InventoryDb result = collection.FindById((ulong)clientUniqueId);
            //It's okay if inventory is default.
            return result;
        }

        public void SetInventory(uint clientUniqueId, InventoryDb inventory)
        {
            if (!DatabaseExist(true))
                return;

            ILiteCollection<InventoryDb> collection = _database.GetCollection<InventoryDb>();
            collection.Upsert((ulong)clientUniqueId, inventory);
        }
    }


}