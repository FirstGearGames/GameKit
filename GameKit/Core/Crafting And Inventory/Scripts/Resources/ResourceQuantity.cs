using UnityEngine;

namespace GameKit.Core.Resources
{
    public struct SerializableResourceQuantity
    {
        /// <summary>
        /// Type of resource.
        /// </summary>
        public int ResourceId;
        /// <summary>
        /// Quantity of resource.
        /// </summary>
        public int Quantity;

        public SerializableResourceQuantity(int resourceId, int quantity)
        {
            ResourceId = resourceId;
            Quantity = quantity;
        }
    }

    public struct ResourceQuantity
    {
        /// <summary>
        /// Returns if this entry is considered unset.
        /// </summary>
        public bool IsUnset => (ResourceId == ResourceConsts.UNSET_RESOURCE_ID || Quantity == 0);
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

        public SerializableResourceQuantity ToSerializable()
        {
            return new SerializableResourceQuantity(ResourceId, Quantity);
        }

        /// <summary>
        /// Gives this ResourceQuantity unset values.
        /// </summary>
        public void MakeUnset()
        {
            ResourceId = ResourceConsts.UNSET_RESOURCE_ID;
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