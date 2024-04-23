using GameKit.Core.Dependencies;
using GameKit.Core.Inventories;
using GameKit.Core.Providers;
using GameKit.Core.Resources;
using System.Collections.Generic;

namespace GameKit.Core.Quests
{
    public class ActiveQuest
    {
        /// <summary>
        /// Called when any ActiveQuest state changes.
        /// </summary>
        public static event QuestStateDel OnQuestState_Static;
        /// <summary>
        /// Called when this ActiveQuest state changes.
        /// </summary>
        public static event QuestStateDel OnQuestState;
        public delegate void QuestStateDel(ActiveQuest activeQuest, QuestState state);
        /// <summary>
        /// Called when any ActiveQuest has a condition met change for a quest condition.
        /// </summary>
        public static event QuestObjectiveState OnQuestObjectiveState_Static;
        /// <summary>
        /// Called when this ActiveQuest has a condition met change for a quest condition.
        /// </summary>
        public static event QuestObjectiveState OnQuestObjectiveStateChange;

        public delegate void QuestObjectiveState(ActiveQuest activeQuest, QuestObjectiveState state);
        /// <summary>
        /// ClientInstance this quest is for.
        /// </summary>
        public ClientInstance ClientInstance { get; private set; }
        /// <summary>
        /// Quest which is active.
        /// </summary>
        public QuestData Quest { get; private set; }
        /// <summary>
        /// Object which provided the quest.
        /// </summary>
        public Provider Provider { get; private set; }
        /// <summary>
        /// Conditions of Quest.
        /// </summary>
        private HashSet<uint> _gatherableResourceIds = new HashSet<uint>();
        /// <summary>
        /// Cached value of if conditions are met.
        /// Value will be null if this has not yet been checked.
        /// </summary>
        private bool? _isConditionsMet;

        //TODO add quest manager to player.
        /* initialize with quest manager as well.
         * If a condition becomes met then QuestManager sends
         * a rpc to the server asking server to check. */
        public ActiveQuest(QuestData quest, Provider provider, ClientInstance clientInstance)
        {
            Quest = quest;
            Provider = provider;
            ClientInstance = clientInstance;

            foreach (QuestConditionBase item in quest.Conditions)
            {
                if (item.QuestType == ConditionType.Gather)
                {
                    GatherCondition go = (GatherCondition)item;
                    foreach (GatherableResource gr in go.Resources)
                        _gatherableResourceIds.Add(gr.ResourceData.UniqueId);
                }
            }
        }

        /// <summary>
        /// Returns if all conditions are met.
        /// This can be expensive the first time called.
        /// </summary>
        public bool IsConditionsMet()
        {
            if (_isConditionsMet.HasValue)
                return _isConditionsMet.Value;

            //Most if not all conditions will require access to the inventory.
            Inventory inv = ClientInstance.Inventory;
            /* //TODO: Make another 'bagged resources' exclusively for hidden objects.
             * hidden objects wont be aquired nearly as often but could be in the thousands
             * such as area tokens. We dont want to put that load into the more commonly iterated
             * bagged resources. Also exclude common resource actions like swapping from hidden objects,
             * as well ignore stack limits and what not from hidden items. */
            foreach (QuestConditionBase item in Quest.Conditions)
            {
                //Check gather condition.
                if (item is GatherCondition gc)
                {
                    foreach (GatherableResource gr in gc.Resources)
                    {
                        List<BagSlot> abr;                        
                        inv.BaggedResources.TryGetValue(gr.ResourceData.UniqueId, out abr);
                        //If resource doesnt exist or count is less than required then condition is not met.
                        if (abr == null || abr.Count < gr.Quantity)
                        {
                            _isConditionsMet = false;
                            return false;
                        }
                    }
                }

                //Check travel conditions.
                if (item is TravelCondition tc)
                {
                    foreach (ResourceData rd in tc.Objects)
                    {
                        List<BagSlot> abr;
                        inv.BaggedResources.TryGetValue(rd.UniqueId, out abr);
                        //Travel conditions only require one of the item.
                        if (abr == null || abr.Count <= 0)
                        {
                            _isConditionsMet = false;
                            return false;
                        }
                    }
                }
            }

            //Fall through, all are met.
            _isConditionsMet = true;
            return true;
        }

        /// <summary>
        /// Called when an item is added to the players inventory.
        /// </summary>
        /// <param name="rd"></param>
        public void ItemAdded(ResourceData rd)
        {
            CheckGatherConditionMet(rd);
        }

        /// <summary>
        /// Called when an item is removed from the players inventory.
        /// </summary>
        public void ItemRemoved(ResourceData rd)
        {
            CheckGatherConditionMet(rd);
        }

        /// <summary>
        /// Checks if a gather condition has been met.
        /// </summary>
        /// <param name="rd"></param>
        private void CheckGatherConditionMet(ResourceData rd)
        {
            //Not a resource for this quest.
            if (_gatherableResourceIds.Contains(rd.UniqueId))
                return;

            uint resourceUniqueId = rd.UniqueId;

            foreach (QuestConditionBase item in Quest.Conditions)
            {
                if (item.QuestType != ConditionType.Gather)
                    continue;

                GatherCondition go = (GatherCondition)item;
                //If gatherables contains the resource data see if it's completed.
                foreach  (GatherableResource gr in go.Resources)
                {
                    if (gr.ResourceData.UniqueId != resourceUniqueId)
                        continue;

                    //If here then compare if condition is met.

                }
            }

        }
    }

}