using UnityEngine;

namespace GameKit.Core.Resources
{

    public class ResourceDataBase : ScriptableObject
    {
        /// <summary>
        /// UniqueId of the resource.   
        /// </summary>
        [HideInInspector, System.NonSerialized]
        public uint UniqueId = ResourceConsts.UNSET_RESOURCE_ID;
        /// <summary>
        /// Id of something this object is associated with.
        /// </summary>
        [HideInInspector, System.NonSerialized]
        public uint RuntimeId = ResourceConsts.UNSET_RESOURCE_ID;
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