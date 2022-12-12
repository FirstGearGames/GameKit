using GameKit.Resources;

namespace GameKit.Inventories
{

    public class Bag
    {
        #region Public.
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
        public Bag(int space)
        {
            Initialize(space);
        }

        /// <summary> 
        /// Initializes this bag with available space.
        /// </summary>
        /// <param name="maxSpace"></param>
        public void Initialize(int maxSpace)
        {
            Slots = new ResourceQuantity[maxSpace];
            for (int i = 0; i < maxSpace; i++)
            {
                Slots[i] = new ResourceQuantity(-1, 0);
                Slots[i].MakeUnset();
            }
        }

    }


}