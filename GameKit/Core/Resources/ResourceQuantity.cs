using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace GameKit.Core.Resources
{

    public struct SerializableResourceQuantity
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

        public SerializableResourceQuantity(uint uniqueId, int quantity)
        {
            UniqueId = uniqueId;
            Quantity = quantity;
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
    }

    public static class ResourceQuantityExtensions
    {
        /// <summary>
        /// Makes this object network serializable.
        /// </summary>
        /// <returns></returns>
        public static SerializableResourceQuantity ToSerializable(this SerializableResourceQuantity rq)
        {
            return new SerializableResourceQuantity(rq.UniqueId, rq.Quantity);
        }

        /// <summary>
        /// Makes this object network serializable.
        /// </summary>
        /// <returns></returns>
        public static List<SerializableResourceQuantity> ToSerializable(this List<SerializableResourceQuantity> rqs)
        {
            List<SerializableResourceQuantity> result = new();
            foreach (SerializableResourceQuantity rq in rqs)
                result.Add(rq.ToSerializable());

            return result;
        }


        /// <summary>
        /// Makes this object native.
        /// </summary>
        /// <returns></returns>
        public static SerializableResourceQuantity ToNative(this SerializableResourceQuantity srq)
        {
            return new SerializableResourceQuantity(srq.UniqueId, srq.Quantity);
        }

        /// <summary>
        /// Makes this object native.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<SerializableResourceQuantity> ToNative(this List<SerializableResourceQuantity> srqs)
        {
            List<SerializableResourceQuantity> result = new();
            foreach (SerializableResourceQuantity srq in srqs)
                result.Add(srq.ToNative());

            return result;
        }

        /// <summary>
        /// Makes this object native to a supplied dictionary replacing any existing keys.
        /// </summary>
        public static void ToNativeReplace(this SerializableResourceQuantity srq, Dictionary<uint, int> dict)
        {
            dict[srq.UniqueId] = srq.Quantity;
        }

        /// <summary>
        /// Makes this object native to a supplied dictionary using Add.
        /// </summary>
        public static void ToNativeAdd(this SerializableResourceQuantity srq, Dictionary<uint, int> dict)
        {
            dict.Add(srq.UniqueId, srq.Quantity);
        }

        /// <summary>
        /// Makes this object native to a supplied dictionary replacing any existing keys.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ToNativeReplace(this List<SerializableResourceQuantity> srqs, Dictionary<uint, int> dict)
        {
            foreach (SerializableResourceQuantity item in srqs)
                item.ToNativeReplace(dict);
        }

        /// <summary>
        /// Makes this object native to a supplied dictionary replacing any existing keys.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ToNativeAdd(this List<SerializableResourceQuantity> srqs, Dictionary<uint, int> dict)
        {
            foreach (SerializableResourceQuantity item in srqs)
                item.ToNativeAdd(dict);
        }
    }


}