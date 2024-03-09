using FishNet.Object;
using GameKit.Core.Providers;
using GameKit.Core.Resources.Droppables;
using GameKit.Dependencies.Utilities;
using GameKit.Dependencies.Utilities.Types;
using System.Collections.Generic;

namespace GameKit.Core.Quests
{

    public class Quester : NetworkBehaviour
    {
        #region Types.
        /// <summary>
        /// Droppables for a certain quest.
        /// </summary>
        private struct QuestDroppableData
        {
            /// <summary>
            /// Quest givers UniqueId.
            /// </summary>
            public uint GiverId;
            /// <summary>
            /// Quest the droppables are for.
            /// </summary>
            public QuestData Quest;
            /// <summary>
            /// Droppables for the quest.
            /// This should not be put into CollectionCaches since it's being set by reference.
            /// </summary>
            public List<DroppableData> Droppables;

            public QuestDroppableData(uint giverId, QuestData quest, List<DroppableData> droppables)
            {
                GiverId = giverId;
                Quest = quest;
                Droppables = droppables;
            }
        }
        #endregion

        /// <summary>
        /// Resources which can be dropped.
        /// Key: object which can drop the resource, such as a NPC.
        /// Value: droppables for the NPC.
        /// </summary>
        private Dictionary<uint, List<QuestDroppableData>> _droppableResources;

        private void Awake()
        {
            _droppableResources = CollectionCaches<uint, List<QuestDroppableData>>.RetrieveDictionary();
        }

        private void OnDestroy()
        {
            foreach (List<QuestDroppableData> item in _droppableResources.Values)
                CollectionCaches<QuestDroppableData>.Store(item);
            CollectionCaches<uint, List<QuestDroppableData>>.Store(_droppableResources);
        }

        /// <summary>
        /// Adds a quest.
        /// </summary>
        /// <returns>True if quest was added.</returns>
        public void AddQuest(ProviderData provider, QuestData quest)
        {
            uint providerId = provider.UniqueId;
            List<QuestDroppableData> droppables;
            //If no quest are set for the giver yet.
            if (!_droppableResources.TryGetValueIL2CPP(providerId, out droppables))
            {
                droppables = CollectionCaches<QuestDroppableData>.RetrieveList();
                _droppableResources[providerId] = droppables;
            }
            /* //TODO This is pretty much all wrong.
             *
             * Droppables need to be stored based on provider which can drop
             * them. EG: a new dictionary (ProviderData, List<DroppableData> Droppables) where
             * the key is the provider that drops the object, and value are droppables
             * the provider is currently capable of due to quests.
             * 
             * There needs to be a reverse lookup as well where RemoveQuest can know
             * which drops to remove. This could be done by reading the provider of
             * each QuestDroppable, using that Id to find droppables, and removing the supplied
             * DroppableData for the QuestDroppable. */

            //Make sure quest was not already added.
            foreach (QuestDroppableData item in droppables)
            {
                if (item.Quest == quest)
                    return;
            }

            //Add all droppables for quest.
            QuestDroppableData qdd = new QuestDroppableData(providerId, quest, quest.Droppables);
            droppables.Add(qdd);
        }

        /// <summary>
        /// Removes a quest.
        /// </summary>
        /// <returns>True if quest was found and removed.</returns>
        public bool RemoveQuest(ProviderData provider, QuestData quest)
        {
            uint providerId = provider.UniqueId;
            List<QuestDroppableData> droppables;
            //Provider has not given any quests.
            if (!_droppableResources.TryGetValueIL2CPP(providerId, out droppables))
                return false;

            //Find the quest in droppables.
            for (int i = 0; i < droppables.Count; i++)
            {
                if (droppables[i].Quest == quest)
                {
                    droppables.RemoveAt(i);
                    return true;
                }

            }

            //If here then quest was not found.
            return false;
        }

        /// <summary>
        /// Tries to spawn any droppables for quests under a provider.
        /// </summary>
        /// <param name="maxDrops">Maximum drop results to gather. Item quantities can exceed this value.</param>
        /// <param name="allowRepeatingDrops">True to allow the same drop to be added to results more than once. Item quantities can exceed this value.</param>
        /// <returns>True if droppables were found. True does not indicate a resource dropped.</returns>
        public bool GetRandomDroppables(ProviderData provider, ref Dictionary<DroppableData, uint> results, int maxDrops = 3, bool allowRepeatingDrops = false)
        {
            if (!_droppableResources.TryGetValueIL2CPP(provider.UniqueId, out List<QuestDroppableData> droppables))
                return false;

            if (maxDrops < 1)
                maxDrops = 1;
            IntRange quantity = new IntRange(0, maxDrops);

            /* Add all droppables to a single collection.
             * This could be optimized by building the collection
             * every time a quest with droppables is added, but
             * at the same time the odds of there being more than 1
             * droppable collections is very unlikely.
             * 
             * We will save some perf though by simple using the single
             * droppables collection reference if it's the only collection. */

            List<DroppableData> allDroppables;
            bool combineDroppables = (droppables.Count > 1);
            if (combineDroppables)
            {
                allDroppables = CollectionCaches<DroppableData>.RetrieveList();
                foreach (QuestDroppableData item in droppables)
                    allDroppables.AddRange(item.Droppables);
            }
            else
            {
                allDroppables = droppables[0].Droppables;
            }

            WeightedRandom.GetEntries(allDroppables, quantity, ref results, allowRepeatingDrops);

            /* If allDroppables was combined by multiple collections then store it
             * as it's no longer needed. */
            if (combineDroppables)
                CollectionCaches<DroppableData>.Store(allDroppables);

            return true;
        }
    }

}