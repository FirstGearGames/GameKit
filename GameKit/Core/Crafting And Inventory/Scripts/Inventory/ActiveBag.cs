using FishNet.Serializing;
using GameKit.Core.Resources;
using System.Collections.Generic;
using Unity.Plastic.Newtonsoft.Json;

namespace GameKit.Core.Inventories.Bags
{
    public struct SerializableActiveBag
    {
        /// <summary>
        /// Information about a slot and it's resources.
        /// </summary>
        public struct FilledSlot
        {
            /// <summary>
            /// Slot containing resources.
            /// </summary>
            public int Slot;
            /// <summary>
            /// Resources within slot.
            /// </summary>
            public SerializableResourceQuantity ResourceQuantity;

            public FilledSlot(int slot, SerializableResourceQuantity resourceQuantity)
            {
                Slot = slot;
                ResourceQuantity = resourceQuantity;
            }
        }

        /// <summary>
        /// UniqueId for the Bag used.
        /// </summary>
        public int BagUniqueId;
        /// <summary>
        /// Index of this bag within it's placement, such as an inventory.
        /// </summary>
        public int Index;
        /// <summary>
        /// All slots which have resources within them.
        /// </summary>
        public List<FilledSlot> FilledSlots;
    }
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
        /// Returns a serializable type containing this active bags information.
        /// </summary>
        /// <returns></returns>
        public SerializableActiveBag ToSerializable()
        {
            SerializableActiveBag result = new SerializableActiveBag();
            result.BagUniqueId = Bag.UniqueId;
            result.Index = Index;

            result.FilledSlots = new List<SerializableActiveBag.FilledSlot>();
            for (int i = 0; i < Slots.Length; i++)
            {
                ResourceQuantity rq = Slots[i];
                if (rq.IsUnset)
                    continue;

                result.FilledSlots.Add(new SerializableActiveBag.FilledSlot(i, rq.ToSerializable()));
            }

            return result;
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


    public static class ActiveBagExtensions
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

        public static List<SerializableActiveBag> ToSerializable(this List<ActiveBag> activeBags)
        {
            List<SerializableActiveBag> result = new List<SerializableActiveBag>();
            foreach (ActiveBag item in activeBags)
                result.Add(item.ToSerializable());

            return result;
        }
    }

}