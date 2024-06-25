using FishNet.Managing;
using GameKit.Core.Inventories.Bags;
using LiteDB;
using System.Collections.Generic;

namespace GameKit.Core.Databases.LiteDb
{

    public partial class InventoryDbService : IInventoryDbService_Server
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

        public static InventoryDbService Instance { get; private set; } = new InventoryDbService();

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