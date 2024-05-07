using FishNet.Managing;
using FishNet;
using GameKit.Core.Inventories.Bags;
using GameKit.Core.Resources;

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
        public ActiveBag ActiveBag;
        /// <summary>
        /// Slot in the bag where this item resides.
        /// </summary>
        public int SlotIndex;

        public BagSlot(ActiveBag activeBag, int slotIndex)
        {
            ActiveBag = activeBag;
            SlotIndex = slotIndex;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inv">Inventory of the player this is for.</param>
        public BagSlot(SerializableBagSlot sbs, Inventory inv) : this()
        {
            if (!inv.ActiveBags.TryGetValue(sbs.ActiveBagUniqueId, out ActiveBag))
                SlotIndex = sbs.SlotIndex;
            else
                inv.NetworkManager.LogError($"UniqueId {sbs.ActiveBagUniqueId} could not be found in Inventory for client {inv.Owner.ToString()}");
        }

        /// <summary>
        /// Returns if this object matches other.
        /// </summary>
        public bool Equals(BagSlot other) => (SlotIndex == other.SlotIndex && ActiveBag == other.ActiveBag);
        #endregion
    }

    /// <summary>
    /// A resource which is in an ActiveBag.
    /// </summary>
    public struct SerializableBagSlot
    {
        #region Public.
        /// <summary>
        /// UniqueId of the bag which holds this item.
        /// </summary>
        public uint ActiveBagUniqueId;
        /// <summary>
        /// Slot in the bag where this item resides.
        /// </summary>
        public int SlotIndex;

        public SerializableBagSlot(uint activeBagUniqueId, int slotIndex)
        {
            ActiveBagUniqueId = activeBagUniqueId;
            SlotIndex = slotIndex;
        }
        #endregion
    }

    public static class BagSlotExtensions
    {
        /// <summary>
        /// Returns a serializable type.
        /// </summary>
        /// <returns></returns>
        public static SerializableBagSlot ToSerializable(this BagSlot bs)
        {
            uint activeBagUniqueId;
            if (bs.ActiveBag == null)
                activeBagUniqueId = InventoryConsts.UNSET_BAG_ID;
            else
                activeBagUniqueId = bs.ActiveBag.UniqueId;

            return new SerializableBagSlot(activeBagUniqueId, bs.SlotIndex);
        }

        /// <summary>
        /// Returns a native type.
        /// </summary>
        /// <returns></returns>
        /// <param name="inv">Inventory of the client this BagSlot is for.</param>
        public static BagSlot ToNative(this SerializableBagSlot sbs, Inventory inv)
        {
            if (inv.ActiveBags.TryGetValue(sbs.ActiveBagUniqueId, out ActiveBag ab))
                return new BagSlot(ab, sbs.SlotIndex);

            inv.NetworkManager.LogError($"UniqueId {sbs.ActiveBagUniqueId} could not be found in Inventory for client {inv.Owner.ToString()}");
            return default;
        }


    }
}