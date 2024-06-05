namespace GameKit.Core.Inventories.Bags
{
    public struct SerializableBagData
    {
        /// <summary>
        /// Unique Id for this bag. This is generally a database Id for the bag.
        /// </summary>
        public uint UniqueId { get; set; }

        public SerializableBagData(uint uniqueId)
        {
            UniqueId = uniqueId;
        }
    }

}