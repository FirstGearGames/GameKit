using LiteDB;
using System.IO;
using UnityEngine;

namespace GameKit.Core.Databases.LiteDb
{

    public partial class DroppableDbService : IDroppableDbService_Server
    {
        private LiteDatabase _databaseServer;

        private void InitializeState_Server()
        {
            string path = $"{Path.Combine(Application.persistentDataPath, "Droppable_Server.db")}";
            _databaseServer = new LiteDatabase(path);
        }

        private void ResetState_Server()
        {
            _databaseServer.Dispose();
            _databaseServer = null;
        }

    }


}