using FishNet.Object;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace GameKit.Core.Resources
{
    /// <summary>
    /// Holds information about resources.
    /// </summary>
    public partial class ResourceManager : NetworkBehaviour
    {
        #region Public.
        /// <summary>
        /// Resource datas.
        /// </summary>
        [System.NonSerialized, HideInInspector]
        public List<ResourceData> ResourceDatas = new List<ResourceData>();
        /// <summary>
        /// Resource category datas.
        /// </summary>
        [System.NonSerialized, HideInInspector]
        public List<ResourceCategoryData> ResourceCategoryDatas = new List<ResourceCategoryData>();
        #endregion

        #region Private.
        /// <summary>
        /// ResourceDatas lookup.
        /// Key: the resource UniqueId.
        /// Value: ResourceData reference.
        /// </summary>
        private Dictionary<uint, ResourceData> _resourceDatasCache = new Dictionary<uint, ResourceData>();
        /// <summary>
        /// ResourceCategoryDatas lookup.
        /// Key: the resource category UniqueId.
        /// Value: ResourceCategoryData reference.
        /// </summary>
        private Dictionary<uint, ResourceCategoryData> _resourceCategoryDatasCache = new Dictionary<uint, ResourceCategoryData>();
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
        public void AddResourceData(ResourceData data, bool applyUniqueId)
        {
            if (!data.Enabled)
                return;

            if (applyUniqueId)
                data.UniqueId = ((uint)ResourceDatas.Count + ResourceConsts.UNSET_RESOURCE_ID + 1);
            ResourceDatas.Add(data);
            _resourceDatasCache.Add(data.UniqueId, data);
        }
        /// <summary>
        /// Adds datas to ResourceDatas.
        /// </summary>
        /// <param name="datas"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddResourceData(IEnumerable<ResourceData> datas, bool applyUniqueId)
        {
            foreach (ResourceData rd in datas)
                AddResourceData(rd, applyUniqueId);
        }

        /// <summary>
        /// Adds data to ResourceCategoryDatas.
        /// </summary>
        /// <param name="data"></param>
        public void AddResourceCategoryData(ResourceCategoryData data)
        {
            ResourceCategoryDatas.Add(data);
            _resourceCategoryDatasCache.Add((uint)data.Category, data);
        }
        /// <summary>
        /// Adds datas to ResourceCategoryDatas.
        /// </summary>
        /// <param name="datas"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddResourceCategoryData(IEnumerable<ResourceCategoryData> datas)
        {
            foreach (ResourceCategoryData rcd in datas)
                AddResourceCategoryData(rcd);
        }


        /// <summary>
        /// Gets a ResourceData for a resource type.
        /// </summary>
        public ResourceData GetResourceData(uint uniqueId)
        {
            if (uniqueId == ResourceConsts.UNSET_RESOURCE_ID)
                return null;

            ResourceData result;
            if (!_resourceDatasCache.TryGetValue(uniqueId, out result))
                Debug.LogError($"ResourceData not found for {uniqueId}.");

            return result;
        }

        /// <summary>
        /// Gets ResourceCategory for a resource type.
        /// </summary>
        public uint GetResourceCategory(uint uniqueId)
        {
            if (uniqueId == ResourceConsts.UNSET_RESOURCE_ID)
                return ResourceConsts.UNSET_RESOURCE_CATEGORY;

            ResourceData rd = GetResourceData(uniqueId);
            if (rd != null)
                return (uint)rd.Category;
            else
                return ResourceConsts.UNSET_RESOURCE_CATEGORY;
        }

        /// <summary>
        /// Gets a ResourceCategoryData for a resource type.
        /// </summary>
        public ResourceCategoryData GetResourceCategoryData(uint uniqueId)
        {
            if (uniqueId == ResourceConsts.UNSET_RESOURCE_ID)
                return null;

            ResourceCategoryData result;
            if (!_resourceCategoryDatasCache.TryGetValue(uniqueId, out result))
                Debug.LogError($"ResourceCategoryData not found for {uniqueId}.");

            return result;
        }

    }

}


