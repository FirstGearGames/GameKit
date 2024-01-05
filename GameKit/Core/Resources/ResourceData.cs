using UnityEngine;

namespace GameKit.Core.Resources
{

    [CreateAssetMenu(fileName = "ResourceData", menuName = "Game/New ResourceData", order = 1)]
    public class ResourceData : ScriptableObject
    {
        /// <summary>
        /// UniqueId of the resource.   
        /// </summary>
        public uint UniqueId;
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
        [Range(0, ushort.MaxValue)]
        public int StackLimit = ResourceConsts.UNSET_STACK_LIMIT;
        /// <summary>
        /// Maximum number of items which may exist with an inventory.
        /// </summary>
        [Range(0, ushort.MaxValue)]
        public int QuantityLimit = ResourceConsts.UNSET_QUANTITY_LIMIT;
    }
}