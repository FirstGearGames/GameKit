namespace GameKit.Core.Inventories.Bags
{
    public struct SerializableBag
    {
        /// <summary>
        /// Unique Id for this bag. This is generally a database Id for the bag.
        /// </summary>
        public uint UniqueId;

        public SerializableBag(uint uniqueId)
        {
            UniqueId = uniqueId;
        }
    }

}