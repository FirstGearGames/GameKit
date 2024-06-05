using GameKit.Core.Resources;
using System.Collections.Generic;

namespace GameKit.Core.Inventories.Bags
{
    /// <summary>
    /// Information about a slot and it's resources.
    /// </summary>
    public struct SerializableFilledSlot
    {
        /// <summary>
        /// Slot containing resources.
        /// </summary>
        public int Slot { get; set; }
        /// <summary>
        /// Resources within slot.
        /// </summary>
        public SerializableResourceQuantity ResourceQuantity { get; set; }

        public SerializableFilledSlot(int slot, SerializableResourceQuantity resourceQuantity)
        {
            Slot = slot;
            ResourceQuantity = resourceQuantity;
        }
    }


    public static class FilledSlotsExtensions
    {
        public static ResourceQuantity[] GetResourceQuantity(this List<SerializableFilledSlot> filledSlots, int slots)
        {
            ResourceQuantity[] rq = new ResourceQuantity[slots];
            filledSlots.GetResourceQuantity(ref rq);
            return rq;
        }

        /// <summary>
        /// Populates ResourceQuantity using FilledSlots.
        /// </summary>
        /// <param name="result">Collection to put data into. The collection is expected to be the correct size.</param>
        /// <returns></returns>
        public static void GetResourceQuantity(this List<SerializableFilledSlot> filledSlots, ref ResourceQuantity[] result)
        {
            foreach (SerializableFilledSlot item in filledSlots)
                result[item.Slot] = item.ResourceQuantity.ToNative();
        }
    }
}