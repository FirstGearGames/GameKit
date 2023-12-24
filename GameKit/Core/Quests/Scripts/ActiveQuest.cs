using GameKit.Core.Resources;
using System.Collections.Generic;
using UnityEngine;

namespace GameKit.Core.Quests
{
    [CreateAssetMenu(fileName = "New Quest", menuName = "GameKit/Quests/Quest", order = int.MinValue)]
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
        public static event QuestObjectiveState OnQuestObjectiveState;

        public delegate void QuestObjectiveState(ActiveQuest activeQuest, QuestObjectiveState state);
        /// <summary>
        /// Quest this is for.
        /// </summary>
        public Quest Quest { get; private set; }
        /// <summary>
        /// Object which provided the quest.
        /// </summary>
        public QuestProvider Provider { get; private set; }
        /// <summary>
        /// Conditions of Quest.
        /// </summary>
        private HashSet<int> _gatherableResourceIds = new HashSet<int>();
        //TODO add quest manager to player.
        /* initialize with quest manager as well.
         * If a condition becomes met then QuestManager sends
         * a rpc to the server asking server to check. */
        public ActiveQuest(Quest quest, QuestProvider provider)
        {
            Quest = quest;
            Provider = provider;

            foreach (QuestObjectiveBase item in quest.Objectives)
            {
                if (item.QuestType == ConditionType.Gather)
                {
                    GatherObjective go = (GatherObjective)item;
                    foreach (GatherableResource gr in go.Resources)
                        _gatherableResourceIds.Add(gr.ResourceData.GetResourceId());
                }
            }
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
            if (_gatherableResourceIds.Contains(rd.GetResourceId()))
                return;

            int resourceId = rd.GetResourceId();

            foreach (QuestObjectiveBase item in Quest.Objectives)
            {
                if (item.QuestType != ConditionType.Gather)
                    continue;

                GatherObjective go = (GatherObjective)item;
                //If gatherables contains the resource data see if it's completed.
                foreach  (GatherableResource gr in go.Resources)
                {
                    if (gr.ResourceData.GetResourceId() != resourceId)
                        continue;

                    //If here then compare if condition is met.

                }
            }

        }
    }

}