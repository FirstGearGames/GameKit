using FishNet.Managing;
using FishNet.Object;
using GameKit.Dependencies.Utilities.Types;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace GameKit.Core.Resources.Droppables
{
    /// <summary>
    /// Holds information about resources.
    /// </summary>
    public partial class DroppableManager : NetworkBehaviour
    {
        #region Public.
        /// <summary>
        /// Droppable datas.
        /// </summary>
        [System.NonSerialized, HideInInspector]
        public List<DroppableData> DroppableDatas = new List<DroppableData>();
        #endregion

        #region Private.
        /// <summary>
        /// DroppableDatas lookup.
        /// Key: the droppable UniqueId.
        /// Value: DroppableData reference.
        /// </summary>
        private Dictionary<uint, DroppableData> _droppableDatasCache = new Dictionary<uint, DroppableData>();
        #endregion

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            base.NetworkManager.RegisterInstance(this);
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            base.NetworkManager.UnregisterInstance<ResourceManager>();
        }

        /// <summary>
        /// Adds data to ResourceDatas.
        /// </summary>
        /// <param name="data"></param>
        public void AddDroppableData(DroppableData data, bool applyUniqueId)
        {
            if (applyUniqueId)
                data.UniqueId = ((uint)DroppableDatas.Count + ResourceConsts.UNSET_RESOURCE_ID + 1);
            //Set minimum quantity to 1.
            if (data.Quantity.Minimum < 1)
            {
                ByteRange quantity = new ByteRange(1, data.Quantity.Maximum);
                data.Quantity = quantity;
            }

            DroppableDatas.Add(data);
            _droppableDatasCache.Add(data.UniqueId, data);
        }
        /// <summary>
        /// Adds datas to ResourceDatas.
        /// </summary>
        /// <param name="datas"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddDroppableData(IEnumerable<DroppableData> datas, bool applyUniqueId)
        {
            foreach (DroppableData dd in datas)
                AddDroppableData(dd, applyUniqueId);
        }

        /// <summary>
        /// Gets a DroppableData using a UniqueId.
        /// </summary>
        public DroppableData GetDroppableData(uint uniqueId)
        {
            if (uniqueId == ResourceConsts.UNSET_RESOURCE_ID)
                return null;

            DroppableData result;
            if (!_droppableDatasCache.TryGetValue(uniqueId, out result))
                NetworkManagerExtensions.LogError($"DroppableData not found for {uniqueId}.");

            return result;
        }
  
    }

}


