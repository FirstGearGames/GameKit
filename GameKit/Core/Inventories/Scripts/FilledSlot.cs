using GameKit.Core.Resources;
using System.Collections.Generic;

namespace GameKit.Core.Inventories.Bags
{
    /// <summary>
    /// Information about a slot and it's resources.
    /// </summary>
    public struct FilledSlot
    {
        /// <summary>
        /// Slot containing resources.
        /// </summary>
        public int Slot;
        /// <summary>
        /// Resources within slot.
        /// </summary>
        public SerializableResourceQuantity ResourceQuantity;

        public FilledSlot(int slot, SerializableResourceQuantity resourceQuantity)
        {
            Slot = slot;
            ResourceQuantity = resourceQuantity;
        }
    }


    public static class FilledSlotsExtensions
    {
        public static ResourceQuantity[] GetResourceQuantity(this List<FilledSlot> filledSlots, int slots)
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
        public static void GetResourceQuantity(this List<FilledSlot> filledSlots, ref ResourceQuantity[] result)
        {
            foreach (FilledSlot item in filledSlots)
                result[item.Slot] = item.ResourceQuantity.ToNative();
        }
    }
}