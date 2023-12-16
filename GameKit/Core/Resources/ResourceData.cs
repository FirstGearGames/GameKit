using GameKit.Core.Resources;
using UnityEngine;

namespace GameKit.Core.Resources
{

    [CreateAssetMenu(fileName = "ResourceData", menuName = "Game/New ResourceData", order = 1)]
    public class ResourceData : ScriptableObject, IResourceData
    {
        /// <summary>
        /// Type of resource this data is for.
        /// </summary>
        public ResourceType ResourceType;
        /// <summary>
        /// Type of categories this resource fits into.
        /// </summary>
        public ResourceCategory Category;
        /// <summary>
        /// Display name of the resource.
        /// </summary>
        public string DisplayName;
        /// <summary>
        /// Description for the resource.
        /// </summary>
        [Multiline]
        public string Description;
        /// <summary>
        /// Icon for the resource.
        /// </summary>
        public Sprite Icon;
        /// <summary>
        /// Maximum number of times this item can be stacked.
        /// </summary>
        [Range(-1, ushort.MaxValue)]
        public int Stacks;
        /// <summary>
        /// Maximum number of items which may exist with an inventory.
        /// </summary>
        [Range(-1, ushort.MaxValue)]
        public int ItemLimit = -1;

        public int GetResourceId() => (int)ResourceType;
        public int GetResourceCategory() => (int)Category;
        public string GetDisplayName()
        {
            if (string.IsNullOrEmpty(DisplayName))
                return ResourceType.ToString();
            else
                return DisplayName;
        }
        public string GetDescription() => Description;
        public Sprite GetIcon() => Icon;
        public int GetStackLimit() => Stacks;
        public int GetMaximumLimit() => ItemLimit;


    }
}