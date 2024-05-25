
namespace GameKit.Core.Inventories
{
    /// <summary>
    /// Constants related to Inventory.
    /// </summary>
    public class InventoryConsts
    {
        /// <summary>
        /// Value to use when a bag Id is unset.
        /// This is used for database Id as well runtime UniqueId.
        /// </summary>
        public const uint UNSET_BAG_ID = 0;
        /// <summary>
        /// Value to use when a category Id is unset.
        /// </summary>
        public const ushort UNSET_CATEGORY_ID = 0;
        /// <summary>
        /// Value to use when bag space is not specified.
        /// </summary>
        public const int UNSET_BAG_SPACE = 0;
        /// <summary>
        /// Value to use when a layout index is not specified.
        /// </summary>
        public const int UNSET_LAYOUT_INDEX = -1;
    }

}