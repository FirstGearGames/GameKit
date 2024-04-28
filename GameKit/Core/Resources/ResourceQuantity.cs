using UnityEngine;

namespace GameKit.Core.Resources
{
    public struct SerializableResourceQuantity
    {
        /// <summary>
        /// Type of resource.
        /// </summary>
        public uint UniqueId;
        /// <summary>
        /// Quantity of resource.
        /// </summary>
        public int Quantity;

        public SerializableResourceQuantity(uint uniqueId, int quantity)
        {
            UniqueId = uniqueId;
            Quantity = quantity;
        }

        public ResourceQuantity ToNative()
        {
            return new ResourceQuantity(UniqueId, Quantity);
        }
    }

    [System.Serializable]
    public struct ResourceQuantity
    {
        /// <summary>
        /// Returns if this entry is considered unset.
        /// </summary>
        public bool IsUnset => (UniqueId == ResourceConsts.UNSET_RESOURCE_ID || Quantity == 0);
        /// <summary>
        /// Type of resource.
        /// </summary>
        public uint UniqueId;
        /// <summary>
        /// Quantity of resource.
        /// </summary>
        [Range(0, ushort.MaxValue)]
        public int Quantity;

        public ResourceQuantity(uint uniqueId, int quantity)
        {
            UniqueId = uniqueId;
            Quantity = quantity;
        }

        public SerializableResourceQuantity ToSerializable()
        {
            return new SerializableResourceQuantity(UniqueId, Quantity);
        }

        /// <summary>
        /// Gives this ResourceQuantity unset values.
        /// </summary>
        public void MakeUnset()
        {
            UniqueId = ResourceConsts.UNSET_RESOURCE_ID;
            Quantity = 0;
        }

        /// <summary>
        /// Updates values.
        /// </summary>
        public void Update(uint uniqueId, int quantity)
        {
            UniqueId = uniqueId;
            Quantity = quantity;
        }
        /// <summary>
        /// Updates the ResourceId.
        /// </summary>
        public void UpdateResourceId(uint uniqueId)
        {
            UniqueId = uniqueId;
        }
        /// <summary>
        /// Updates the Quantity.
        /// </summary>
        public void UpdateQuantity(int quantity)
        {
            Quantity = quantity;
        }
    }



}