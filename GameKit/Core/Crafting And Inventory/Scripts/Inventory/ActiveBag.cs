using FishNet.Serializing;
using GameKit.Core.Resources;

namespace GameKit.Core.Inventories.Bags
{
    /// <summary>
    /// A bag which exist. This can be in the world, inventory, etc.
    /// </summary>
    public class ActiveBag
    {
        #region Public.
        /// <summary>
        /// Information about the bag used.
        /// </summary>
        public Bag Bag { get; private set; }
        /// <summary>
        /// Index of this bag within it's placement, such as an inventory.
        /// </summary>
        public int Index { get; private set; }
        /// <summary>
        /// Maximum space in this bag.
        /// </summary>
        public int MaximumSlots => Bag.Space;
        /// <summary>
        /// Used space in this bag.
        /// </summary>
        public int UsedSlots
        {
            get
            {
                int setCount = 0;
                int slotCount = Slots.Length;
                for (int i = 0; i < slotCount; i++)
                {
                    if (!Slots[i].IsUnset)
                        setCount++;
                }

                return setCount;
            }
        }
        /// <summary>
        /// Space available for use within the inventory.
        /// </summary>
        public int AvailableSlots => (MaximumSlots - UsedSlots);
        /// <summary>
        /// All slots in this bag.
        /// </summary>
        public ResourceQuantity[] Slots { get; private set; } = new ResourceQuantity[0];
        #endregion

        public ActiveBag(Bag b)
        {
            Bag = b;
            Slots = new ResourceQuantity[b.Space];
            for (int i = 0; i < b.Space; i++)
            {
                Slots[i] = new ResourceQuantity(-1, 0);
                Slots[i].MakeUnset();
            }
            Index = -1;
        }

        public ActiveBag(Bag b, int index, ResourceQuantity[] slots)
        {
            Bag = b;
            Index = index;
            Slots = slots;
        }

        /// <summary>
        /// Sets Index for this bag.
        /// </summary>
        public void SetIndex(int value) => Index = value;
        /// <summary>
        /// Sets resource quantities for each slot in this bag.
        /// </summary>
        /// <param name="rq"></param>
        public void SetSlots(ResourceQuantity[] rq) => Slots = rq;
    }


    internal static class ActiveBagExtensions
    {
        public static void WriteActiveBag(this Writer w, ActiveBag value)
        {
            w.WriteUInt16((ushort)value.Bag.UniqueId);
            w.WriteUInt16((ushort)value.Index);
            w.WriteArray<ResourceQuantity>(value.Slots);
        }
        public static ActiveBag ReadActiveBag(this Reader r)
        {
            int uniqueId = r.ReadUInt16();
            int index = r.ReadUInt16();
            ResourceQuantity[] slots = r.ReadArrayAllocated<ResourceQuantity>();

            BagManager manager = r.NetworkManager.GetInstance<BagManager>();
            Bag bag;
            if (manager == null)
                bag = new Bag();
            else
                bag = manager.GetBag(uniqueId);

            ActiveBag ab = new ActiveBag(bag, index, slots);
            return ab;
        }
    }

}