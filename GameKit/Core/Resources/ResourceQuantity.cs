using UnityEngine;

namespace GameKit.Core.Resources
{

    [System.Serializable]
    public struct ResourceQuantity
    {
        /// <summary>
        /// Returns if this entry is considered unset.
        /// </summary>
        public bool IsUnset => (ResourceId == -1 || Quantity == 0);
        /// <summary>
        /// Type of resource.
        /// </summary>
        public int ResourceId;
        /// <summary>
        /// Quantity of resource.
        /// </summary>
        [Range(0, ushort.MaxValue)]
        public int Quantity;

        public ResourceQuantity(int resourceId, int quantity)
        {
            ResourceId = resourceId;
            Quantity = quantity;
        }

        /// <summary>
        /// Gives this ResourceQuantity unset values.
        /// </summary>
        public void MakeUnset()
        {
            ResourceId = -1;
            Quantity = 0;
        }

        /// <summary>
        /// Updates values.
        /// </summary>
        public void Update(int resourceId, int quantity)
        {
            ResourceId = resourceId;
            Quantity = quantity;
        }
    }



}