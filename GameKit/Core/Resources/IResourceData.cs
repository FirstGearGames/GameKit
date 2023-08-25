using UnityEngine;

namespace GameKit.Core.Resources
{

    public interface IResourceData
    {
        /// <summary>
        /// Id for the resource.
        /// </summary>
        /// <returns></returns>
        public int GetResourceId();
        /// <summary>
        /// Category for the resource.
        /// </summary>
        /// <returns></returns>
        public int GetResourceCategory();
        /// <summary>
        /// How many times this resource can be stacked into a single inventory slot.
        /// </summary>
        public int GetStackLimit();
        /// <summary>
        /// Maximum number of times this resource may exist in the inventory.
        /// </summary>
        /// <returns></returns>
        public int GetMaximumLimit();
    }


}