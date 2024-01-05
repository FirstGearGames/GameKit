namespace GameKit.Core.Inventories
{
    /// <summary>
    /// A resource which is in an ActiveBag.
    /// </summary>
    public struct BagSlot
    {
        #region Public.
        /// <summary>
        /// Index of the bag which holds this item.
        /// </summary>
        public int BagIndex;
        /// <summary>
        /// Slot in the bag where this item resides.
        /// </summary>
        public int SlotIndex;

        public BagSlot(int bagIndex, int slotIndex)
        {
            BagIndex = bagIndex;
            SlotIndex = slotIndex;
        }

        /// <summary>
        /// Returns if this object matches other.
        /// </summary>
        public bool Equals(BagSlot other) => (BagIndex == other.BagIndex && SlotIndex == other.SlotIndex);
        #endregion
    }


}