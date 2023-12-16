using GameKit.Core.Resources;
using UnityEngine;

namespace GameKit.Core.Resources
{

    [CreateAssetMenu(fileName = "ResourceCategoryData", menuName = "Game/New ResourceCategoryData", order = 1)]
    public class ResourceCategoryData : ScriptableObject, IResourceCategoryData
    {
        /// <summary>
        /// Category which this resource belongs.
        /// </summary>
        public ResourceCategory Category;
        /// <summary>
        /// Display name of the resource category.
        /// </summary>
        public string DisplayName;
        /// <summary>
        /// Icon for this resource category.
        /// </summary>
        public Sprite Icon;

        public int GetResourceCategoryId() => (int)Category;
        public string GetDisplayName() => DisplayName;
        public Sprite GetIcon() => Icon;
    }


}