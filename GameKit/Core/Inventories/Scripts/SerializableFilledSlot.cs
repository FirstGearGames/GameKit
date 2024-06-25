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
        public int Slot;
        /// <summary>
        /// Resources within slot.
        /// </summary>
        public SerializableResourceQuantity ResourceQuantity;

        public SerializableFilledSlot(int slot, SerializableResourceQuantity resourceQuantity)
        {
            Slot = slot;
            ResourceQuantity = resourceQuantity;
        }
    }


    public static class FilledSlotsExtensions
    {
        public static SerializableResourceQuantity[] GetResourceQuantity(this List<SerializableFilledSlot> filledSlots, int slots)
        {
            SerializableResourceQuantity[] rq = new SerializableResourceQuantity[slots];
            filledSlots.GetResourceQuantity(ref rq);
            return rq;
        }

        /// <summary>
        /// Populates ResourceQuantity using FilledSlots.
        /// </summary>
        /// <param name="result">Collection to put data into. The collection is expected to be the correct size.</param>
        /// <returns></returns>
        public static void GetResourceQuantity(this List<SerializableFilledSlot> filledSlots, ref SerializableResourceQuantity[] result)
        {
            if (filledSlots == null)
                return;

            foreach (SerializableFilledSlot item in filledSlots)
                result[item.Slot] = item.ResourceQuantity.ToNative();
        }
    }
}