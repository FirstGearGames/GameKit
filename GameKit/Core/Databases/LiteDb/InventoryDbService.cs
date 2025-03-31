using FishNet.Managing;
using GameKit.Core.Inventories.Bags;
using LiteDB;
using System.Collections.Generic;
using System.IO;
using GameKit.Core.Inventories;
using UnityEngine;

namespace GameKit.Core.Databases.LiteDb
{
    public partial class InventoryDbService
    {
        #region Types.
        private struct SerializableActiveBagContainer
        {
            public List<SerializableActiveBag> Item;

            public SerializableActiveBagContainer(List<SerializableActiveBag> item)
            {
                Item = item;
            }
        }
        #endregion
        
        public static InventoryDbService Instance { get; private set; } = new();

        public InventoryDbService()
        {
            InitializeState();
        }

        ~InventoryDbService()
        {
            ResetState();
        }
        
        private void InitializeState()
        {
            BsonMapper mapper = BsonMapper.Global;
            mapper.IncludeFields = true;
            mapper.TrimWhitespace = false;
            mapper.EmptyStringToNull = false;

            InitializeState_Server();
            InitializeState_Client();

          //  DeleteInventories_Testing();
        }

        private void DeleteInventories_Testing()
        {
            ClearCollections(_databaseServer);
            ClearCollections(_databaseClient);

            void ClearCollections(LiteDatabase db)
            {
                if (db == null)
                    return;

                ILiteCollection<SerializableInventoryDb> c0 = db.GetCollection<SerializableInventoryDb>();
                if (c0 != null)
                    c0.DeleteAll();

                ILiteCollection<SerializableActiveBagContainer> c1 = db.GetCollection<SerializableActiveBagContainer>();
                if (c1 != null)
                    c1.DeleteAll();
            }
        }

        private void ResetState()
        {
            ResetState_Server();
            ResetState_Client();
            Instance = null;
        }

        private bool DatabaseExists(LiteDatabase db, bool error)
        {
            bool exist = (db != null);
            if (!exist && error)
                NetworkManagerExtensions.LogError($"Database does not exist.");

            return exist;
        }

        private bool GetCollection<T>(LiteDatabase db, bool error, out ILiteCollection<T> result)
        {
            result = default;

            if (!DatabaseExists(db, error))
                return false;

            result = db.GetCollection<T>();
            bool hasValue = (result != null);

            if (!hasValue && error)
                NetworkManagerExtensions.LogError($"Collection {typeof(T).FullName} could not be found.");

            return hasValue;
        }
    }
}