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
            /// A HashSet is used so drops cannot be duplicated due to multiple quest having the same drop.
            /// </summary>
            public HashSet<DroppableData> Droppables;

            public QuestDroppableData(uint giverId, QuestData quest, HashSet<DroppableData> droppables)
            {
                GiverId = giverId;
                Quest = quest;
                Droppables = droppables;
            }
        }
        #endregion

        /// <summary>
        /// Droppables for providers.
        /// </summary>
        private Dictionary<ProviderData, List<DroppableData>> _providerDroppables;
        /// <summary>
        /// Current quests.
        /// </summary>
        private Dictionary<QuestData, ProviderData> _quests;

        private void Awake()
        {
            _providerDroppables = CollectionCaches<ProviderData, List<DroppableData>>.RetrieveDictionary();
            _quests = CollectionCaches<QuestData, ProviderData>.RetrieveDictionary();
        }

        private void OnDestroy()
        {
            CollectionCaches<QuestData, ProviderData>.StoreAndDefault(ref _quests);

            foreach (List<DroppableData> item in _providerDroppables.Values)
                CollectionCaches<DroppableData>.Store(item);
            CollectionCaches<ProviderData, List<DroppableData>>.StoreAndDefault(ref _providerDroppables);
        }

        /// <summary>
        /// Adds a quest.
        /// </summary>
        /// <returns>True if quest was added.</returns>
        public bool AddQuest(ProviderData provider, QuestData quest)
        {
            /* Quest is already added.
             * This is a small limitation allowing
             * the same quest to not be added twice
             * even if by different providers. */
            if (!_quests.TryAdd(quest, provider))
                return false;

            /* Add each droppable to providerDroppables. This allows providers to
             * use GetRandomDroppables which will return possible quest drops. */
            foreach (QuestData.QuestDroppableData item in quest.QuestDroppables)
            {
                foreach (ProviderData pd in item.Providers)
                {
                    List<DroppableData> currentDroppables;
                    if (!_providerDroppables.TryGetValue(pd, out currentDroppables))
                    {
                        currentDroppables = CollectionCaches<DroppableData>.RetrieveList();
                        _providerDroppables[pd] = currentDroppables;
                    }
                    currentDroppables.Add(item.Droppable);
                }

            }

            return true;
        }

        /// <summary>
        /// Removes a quest.
        /// </summary>
        /// <returns>True if quest was found and removed.</returns>
        public bool RemoveQuest(ProviderData provider, QuestData quest)
        {
            //Quest does not exist.
            if (!_quests.Remove(quest))
                return false;

            //uint providerId = provider.UniqueId;
            //List<QuestDroppableData> droppables;
            ////Provider has not given any quests.
            //if (!_droppableResources.TryGetValueIL2CPP(providerId, out droppables))
            //    return false;

            ////Find the quest in droppables.
            //for (int i = 0; i < droppables.Count; i++)
            //{
            //    if (droppables[i].Quest == quest)
            //    {
            //        droppables.RemoveAt(i);
            //        return true;
            //    }

            //}


            /* //TODO to remove droppables in RemoveQuest simply take the same QuestData
            * and look up Providers, and remove the first droppable entry. Since DroppableData
            * is a class the removal will be by reference. */

            //If here then quest was not found.
            return false;
        }

        /// <summary>
        /// Tries to spawn any droppables for quests under a provider.
        /// </summary>
        /// <param name="results">Droppables to drop, and how many of each.</param>
        /// <param name="maxDrops">Maximum drop results to gather. Item quantities can exceed this value.</param>
        /// <param name="allowRepeatingDrops">True to allow the same drop to be added to results more than once. Item quantities can exceed this value.</param>
        /// <returns>True if droppables were found. True does not indicate a resource dropped.</returns>
        public bool GetRandomDroppables(ProviderData provider, ref Dictionary<DroppableData, uint> results, int maxDrops = 3, bool allowRepeatingDrops = false)
        {
            if (!_providerDroppables.TryGetValueIL2CPP(provider, out List<DroppableData> droppables))
                return false;

            if (maxDrops < 1)
                maxDrops = 1;
            IntRange quantity = new IntRange(0, maxDrops);
            WeightedRandom.GetEntries(droppables, quantity, ref results, allowRepeatingDrops);

            return true;
        }
    }

}