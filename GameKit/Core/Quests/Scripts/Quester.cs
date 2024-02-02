using FishNet.Object;
using GameKit.Core.Resources;
using GameKit.Dependencies.Utilities;
using System.Collections.Generic;

namespace GameKit.Core.Quests
{

    public class Quester : NetworkBehaviour
    {
        #region Types.

        #endregion

        /// <summary>
        /// Resources which can be dropped.
        /// Key: object which can drop the resource, such as a NPC.
        /// Value: droppables for the NPC.
        /// </summary>
        private Dictionary<uint, List<ResourceQuantity>> _droppableResources;

        private void Awake()
        {
            _droppableResources = CollectionCaches<uint, List<ResourceQuantity>>.RetrieveDictionary();
        }

        private void OnDestroy()
        {
            CollectionCaches<uint, List<ResourceQuantity>>.Store(_droppableResources);
        }
    }

}