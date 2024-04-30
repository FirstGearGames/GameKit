namespace GameKit.Core.Inventories
{
    /// <summary>
    /// A resource which is in an ActiveBag.
    /// </summary>
    public struct BagSlot
    {
        #region Public.
        /// <summary>
        /// UniqueId of the bag which holds this item.
        /// </summary>
        public uint BagUniqueId;
        /// <summary>
        /// Slot in the bag where this item resides.
        /// </summary>
        public int SlotIndex;

        public BagSlot(uint bagUniqueId, int slotIndex)
        {
            BagUniqueId = bagUniqueId;
            SlotIndex = slotIndex;
        }

        /// <summary>
        /// Returns if this object matches other.
        /// </summary>
        public bool Equals(BagSlot other) => (BagUniqueId == other.BagUniqueId && SlotIndex == other.SlotIndex);
        #endregion
    }


}