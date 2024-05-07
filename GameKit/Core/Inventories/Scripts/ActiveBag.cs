using FishNet;
using FishNet.Managing;
using GameKit.Core.Resources;
using System.Collections.Generic;

namespace GameKit.Core.Inventories.Bags
{
    public struct SerializableActiveBag
    {
        /// <summary>
        /// An Id issued at runtime to reference this bag between server and client.
        /// </summary>
        public uint UniqueId;
        /// <summary>
        /// UniqueId for the BagData used.
        /// </summary>
        public uint BagDataUniqueId;
        /// <summary>
        /// Category or section of the game which this bag belongs to.
        /// This value can be used however liked, such as an Id of 0 would be inventory, 1 could be bank.
        /// </summary>
        public ushort CategoryId;
        /// <summary>
        /// Index of this bag within the client's UI placement.
        /// This value is only used by the client.
        /// </summary>
        public int LayoutIndex;
        /// <summary>
        /// All slots which have resources within them.
        /// </summary>
        public List<FilledSlot> FilledSlots;

        public SerializableActiveBag(ActiveBag ab)
        {
            UniqueId = ab.UniqueId;
            BagDataUniqueId = ab.BagData.UniqueId;
            CategoryId = ab.CategoryId;
            LayoutIndex = ab.LayoutIndex;
            FilledSlots = new();
        }
        public SerializableActiveBag(uint uniqueId, uint bagUniqueId, ushort categoryId, int layoutIndex) : this()
        {
            UniqueId = uniqueId;
            BagDataUniqueId = bagUniqueId;
            CategoryId = categoryId;
            LayoutIndex = layoutIndex;
            FilledSlots = new();
        }

    }
    /// <summary>
    /// A bag which exist. This can be in the world, inventory, etc.
    /// </summary>
    public class ActiveBag
    {
        #region Public.
        /// <summary>
        /// An Id issued at runtime to reference this bag between server and client.
        /// </summary>
        public uint UniqueId;
        /// <summary>
        /// Information about the bag used.
        /// </summary>
        public BagData BagData { get; private set; }
        /// <summary>
        /// Category or section of the game which this bag belongs to.
        /// This value can be used however liked, such as an Id of 0 would be inventory, 1 could be bank.
        /// </summary>
        public ushort CategoryId;
        /// <summary>
        /// Index of this bag within the client's UI placement.
        /// This value is only used by the client.
        /// </summary>
        public int LayoutIndex = InventoryConsts.UNSET_LAYOUT_INDEX;
        /// <summary>
        /// Maximum space in this bag.
        /// </summary>
        public int MaximumSlots => BagData.Space;
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
        public ResourceQuantity[] Slots = new ResourceQuantity[0];
        #endregion

        public ActiveBag(uint uniqueId, BagData b)
        {
            UniqueId = uniqueId;
            BagData = b;           
            Slots = new ResourceQuantity[b.Space];
            for (int i = 0; i < b.Space; i++)
            {
                Slots[i] = new ResourceQuantity(ResourceConsts.UNSET_RESOURCE_ID, 0);
                Slots[i].MakeUnset();
            }
        }

        public ActiveBag(uint uniqueId, BagData b, int layoutIndex, ResourceQuantity[] slots)
        {
            UniqueId = uniqueId;
            BagData = b;
            LayoutIndex = layoutIndex;
            Slots = slots;
        }

        public ActiveBag(SerializableActiveBag sab, BagManager bagManager = null)
        {
            if (bagManager == null)
            {
                if (!InstanceFinder.TryGetInstance<BagManager>(out bagManager))
                {
                    NetworkManagerExtensions.LogError($"BagManager could not be found.");
                    return;
                }
            }

            UniqueId = sab.UniqueId;
            BagData = bagManager.GetBagData(sab.BagDataUniqueId);
            if (BagData == null)
                return;

            LayoutIndex = sab.LayoutIndex;
            Slots = sab.FilledSlots.GetResourceQuantity(BagData.Space);
        }
    }


    public static class ActiveBagExtensions
    {
        /// <summary>
        /// Returns a serializable type.
        /// </summary>
        /// <returns></returns>
        public static SerializableActiveBag ToSerializable(this ActiveBag ab)
        {
            SerializableActiveBag result = new SerializableActiveBag(ab);
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
        /// Returns a native type.
        /// </summary>
        /// <returns></returns>
        /// <param name="bagManager">BagManager to use. If left null InstanceFinder will be used.</param>
        public static ActiveBag ToNative(this SerializableActiveBag sab, BagManager bagManager = null)
        {
            ActiveBag result = new(sab, bagManager);
            return result;
        }

        /// <summary>
        /// Returns a serializable type.
        /// </summary>
        /// <returns></returns>
        public static List<SerializableActiveBag> ToSerializable(this List<ActiveBag> activeBags)
        {
            List<SerializableActiveBag> result = new List<SerializableActiveBag>();
            foreach (ActiveBag item in activeBags)
                result.Add(item.ToSerializable());

            return result;
        }
    }

}