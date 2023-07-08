using FishNet.Serializing;
using GameKit.Resources;

namespace GameKit.Inventories
{

    public class Bag
    {
        #region Public.
        /// <summary>
        /// Index of this bag within Inventory.
        /// </summary>
        public int Index { get; private set; }
        /// <summary>
        /// Maximum space in this bag.
        /// </summary>
        public int MaximumSlots => Slots.Length;
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
        public ResourceQuantity[] Slots { get; protected set; } = new ResourceQuantity[0];
        #endregion
        public Bag() { }

        public Bag(int space, int index)
        {
            Initialize(space, index);
        }

        /// <summary> 
        /// Initializes this bag with available space.
        /// </summary>
        /// <param name="maxSpace"></param>
        public void Initialize(int maxSpace, int index)
        {
            Slots = new ResourceQuantity[maxSpace];
            for (int i = 0; i < maxSpace; i++)
            {
                Slots[i] = new ResourceQuantity(-1, 0);
                Slots[i].MakeUnset();
            }

            Index = index;
        }

        /// <summary>
        /// Sets a new value to Slots.
        /// </summary>
        /// <param name="slots">New value.</param>
        public void SetSlots(ResourceQuantity[] slots) => Slots = slots;
    }


    internal static class BagSerializers
    {
        public static void WriteBag(this Writer w, Bag value)
        {
            w.WriteInt32(value.Index);
            w.WriteArray<ResourceQuantity>(value.Slots);
        }
        public static Bag ReadBag(this Reader r)
        {
            int index = r.ReadInt32();
            ResourceQuantity[] rgs = r.ReadArrayAllocated<ResourceQuantity>();

            Bag bag = new Bag(rgs.Length, index);
            bag.SetSlots(rgs);
            return bag;
        }
    }

}