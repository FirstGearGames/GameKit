using FishNet.Managing;
using FishNet.Object;
using GameKit.Core.Resources;
using System.Collections.Generic;
using UnityEngine;

namespace GameKit.Core.Inventories.Bags
{
    public class BagManager : NetworkBehaviour
    {
        #region Public.
        /// <summary>
        /// All bags for the game.
        /// </summary>
        [HideInInspector]
        public List<BagData> BagDatas = new List<BagData>();
        #endregion

        #region Private.
        /// <summary>
        /// NetworkManager on or a parent of this object.
        /// </summary>
        private NetworkManager _networkManager;
        /// <summary>
        /// Cache of BagDatas.
        /// Key: UniqueId of BagDAta.
        /// Value: BagData.
        /// </summary>
        private Dictionary<uint, BagData> _bagDatasCache = new();
        #endregion

        public override void OnStartNetwork()
        {
            base.NetworkManager.RegisterInstance(this);
        }

        /// <summary>
        /// Adds bag datas.
        /// </summary>
        /// <param name="bags">Datas to add.</param>
        /// <param name="applyUniqueId">True to assign uniqueIds to the bags.</param>
        public void AddBagData(List<BagData> bags, bool applyUniqueId)
        {
            foreach (BagData item in bags)
                AddBagData(item, applyUniqueId);
        }

        /// <summary>
        /// Adds data to ResourceDatas.
        /// </summary>
        /// <param name="data"></param>
        public void AddBagData(BagData data, bool applyUniqueId)
        {
            //Set minimum quantity to 1.
            if (data.Space == InventoryConsts.UNSET_BAG_SPACE)
            {
                base.NetworkManager.LogError($"BagData {data.Name} does not have Space set.");
                return;
            }

            if (applyUniqueId)
                data.UniqueId = ((uint)BagDatas.Count + InventoryConsts.UNSET_BAG_ID + 1);

            BagDatas.Add(data);
            _bagDatasCache.Add(data.UniqueId, data);
        }

        /// <summary>
        /// Gets a bag.
        /// </summary>
        public BagData GetBagData(uint uniqueId)
        {
            if (!_bagDatasCache.TryGetValue(uniqueId, out BagData value))
            {
                _networkManager.LogError($"BagData could not be found for UniqueId {uniqueId}.");
                return null;
            }
            else
            {
                return value;
            }
        }
    }
}