namespace GameKit.Core.Inventories
{
    /// <summary>
    /// A resource which is in an ActiveBag.
    /// </summary>
    public struct ActiveBagResource
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

        public ActiveBagResource(int bagIndex, int slotIndex)
        {
            BagIndex = bagIndex;
            SlotIndex = slotIndex;
        }
        #endregion
    }


}