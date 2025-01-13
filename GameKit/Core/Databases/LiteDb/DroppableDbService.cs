using FishNet.Managing;
using LiteDB;

namespace GameKit.Core.Databases.LiteDb
{

    public partial class DroppableDbService : IDroppableDbService_Server
    {
        
        public static DroppableDbService Instance { get; private set; } = new DroppableDbService();

        public DroppableDbService()
        {
            InitializeState();
        }

        ~DroppableDbService()
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