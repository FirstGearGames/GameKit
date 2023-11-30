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
        /// Resource information.
        /// </summary>
        [System.NonSerialized, HideInInspector]
        public List<IResourceData> ResourceDatas = new List<IResourceData>();
        /// <summary>
        /// Resource category information.
        /// </summary>
        public List<IResourceCategoryData> ResourceCategoryDatas = new List<IResourceCategoryData>();
        #endregion

        #region Private.
        /// <summary>
        /// ResourceDatas lookup.
        /// Key: the resource Id.
        /// Value: IResourceData reference.
        /// </summary>
        private Dictionary<int, IResourceData> _resourceDatasCache = new Dictionary<int, IResourceData>();
        /// <summary>
        /// ResourceCategoryDatas lookup.
        /// Key: the resource category Id.
        /// Value: IResourceCategoryData reference.
        /// </summary>
        private Dictionary<int, IResourceCategoryData> _resourceCategoryDatasCache = new Dictionary<int, IResourceCategoryData>();
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
        public void AddIResourceData(IResourceData data)
        {
            ResourceDatas.Add(data);
            _resourceDatasCache.Add(data.GetResourceId(), data);
        }
        /// <summary>
        /// Adds datas to ResourceDatas.
        /// </summary>
        /// <param name="datas"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddIResourceData(IEnumerable<IResourceData> datas)
        {
            int index = 0;
            foreach (IResourceData ird in datas)
            {
                AddIResourceData(ird);
                index++;
            }
        }

        /// <summary>
        /// Adds data to ResourceCategoryDatas.
        /// </summary>
        /// <param name="data"></param>
        public void AddIResourceCategoryData(IResourceCategoryData data)
        {
            ResourceCategoryDatas.Add(data);
            _resourceCategoryDatasCache.Add(data.GetResourceCategoryId(), data);
        }
        /// <summary>
        /// Adds datas to ResourceCategoryDatas.
        /// </summary>
        /// <param name="datas"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddIResourceCategoryData(IEnumerable<IResourceCategoryData> datas)
        {
            foreach (IResourceCategoryData ircd in datas)
                AddIResourceCategoryData(ircd);
        }


        /// <summary>
        /// Gets a ResourceData for a resource type.
        /// </summary>
        public IResourceData GetIResourceData(int resourceId)
        {
            if (resourceId == -1)
                return null;

            IResourceData result;
            if (!_resourceDatasCache.TryGetValue(resourceId, out result))
                Debug.LogError($"ResourceData not found for {resourceId}.");

            return result;
        }

        /// <summary>
        /// Gets ResourceCategory for a resource type.
        /// </summary>
        public int GetResourceCategory(int resourceId)
        {
            if (resourceId == -1)
                return -1;

            IResourceData rd = GetIResourceData(resourceId);
            if (rd != null)
                return rd.GetResourceCategory();
            else
                return -1;
        }

        /// <summary>
        /// Gets a ResourceCategoryData for a resource type.
        /// </summary>
        public IResourceCategoryData GetResourceCategoryData(int resourceId)
        {
            if (resourceId == -1)
                return null;

            IResourceCategoryData result;
            if (!_resourceCategoryDatasCache.TryGetValue(resourceId, out result))
                Debug.LogError($"ResourceCategoryData not found for {resourceId}.");

            return result;
        }

    }

}


