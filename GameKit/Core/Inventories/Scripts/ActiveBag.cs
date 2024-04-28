using FishNet;
using FishNet.Managing;
using FishNet.Serializing;
using GameKit.Core.Resources;
using System.Collections.Generic;

namespace GameKit.Core.Inventories.Bags
{
    public struct SerializableActiveBag
    {
        /// <summary>
        /// UniqueId for the Bag used.
        /// </summary>
        public uint BagUniqueId;
        /// <summary>
        /// Index of this bag within it's placement, such as an inventory.
        /// </summary>
        public int Index;
        /// <summary>
        /// All slots which have resources within them.
        /// </summary>
        public List<FilledSlot> FilledSlots;

        public SerializableActiveBag(uint bagUniqueId, int index) : this()
        {
            BagUniqueId = bagUniqueId;
            Index = index;
            FilledSlots = new List<FilledSlot>();
        }

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
        public BagData Bag { get; private set; }
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

        public ActiveBag(BagData b)
        {
            Bag = b;
            Slots = new ResourceQuantity[b.Space];
            for (int i = 0; i < b.Space; i++)
            {
                Slots[i] = new ResourceQuantity(ResourceConsts.UNSET_RESOURCE_ID, 0);
                Slots[i].MakeUnset();
            }
            Index = -1;
        }

        public ActiveBag(BagData b, int index, ResourceQuantity[] slots)
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


    public static class ActiveBagExtensions
    {
        /// <summary>
        /// Returns a serializable type containing this active bags information.
        /// </summary>
        /// <returns></returns>
        public static SerializableActiveBag ToSerializable(this ActiveBag ab)
        {
            SerializableActiveBag result = new SerializableActiveBag(ab.Bag.UniqueId, ab.Index);
            for (int i = 0; i < ab.Slots.Length; i++)
            {
                ResourceQuantity rq = ab.Slots[i];
                if (rq.IsUnset)
                    continue;

                result.FilledSlots.Add(new FilledSlot(i, rq.ToSerializable()));
            }

            return result;
        }

        /// <summary>
        /// Returns a serializable type containing this active bags information.
        /// </summary>
        /// <returns></returns>
        /// <param name="bagManager">BagManager to use. If left null InstanceFinder will be used.</param>
        public static ActiveBag ToNative(this SerializableActiveBag sab, BagManager bagManager = null)
        {
            if (bagManager == null)
            {
                if (!InstanceFinder.TryGetInstance<BagManager>(out bagManager))
                {
                    NetworkManagerExtensions.LogError($"BagManager could not be found.");
                    return default;
                }
            }

            BagData bd = bagManager.GetBagData(sab.BagUniqueId);
            ActiveBag result = new(bd, sab.Index, sab.FilledSlots.GetResourceQuantity(bd.Space));
            return result;
        }

        /// <summary>
        /// Returns a serializable type containing this active bags collection information.
        /// </summary>
        /// <returns></returns>
        public static List<SerializableActiveBag> ToSerializable(this List<ActiveBag> activeBags)
        {
            List<SerializableActiveBag> result = new List<SerializableActiveBag>();
            foreach (ActiveBag item in activeBags)
                result.Add(item.ToSerializable());

            return result;
        }

        public static void WriteActiveBag(this Writer w, ActiveBag value)
        {
            w.WriteUInt32(value.Bag.UniqueId);
            w.WriteInt32(value.Index);
            w.WriteArray<ResourceQuantity>(value.Slots);
        }
        public static ActiveBag ReadActiveBag(this Reader r)
        {
            uint uniqueId = r.ReadUInt32();
            int index = r.ReadInt32();
            ResourceQuantity[] slots = r.ReadArrayAllocated<ResourceQuantity>();

            BagManager manager = r.NetworkManager.GetInstance<BagManager>();
            BagData bagData = manager.GetBagData(uniqueId);

            ActiveBag ab = new ActiveBag(bagData, index, slots);
            return ab;
        }

    }

}