namespace GameKit.Core.Inventories
{
    /// <summary>
    /// Data structure for items in bags.
    /// </summary>
    public struct BaggedResource
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

        public BaggedResource(int bagIndex, int slotIndex)
        {
            BagIndex = bagIndex;
            SlotIndex = slotIndex;
        }
        #endregion
    }


}