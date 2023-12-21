using GameKit.Dependencies.Utilities.Types;
using System;
using System.Collections.Generic;

namespace GameKit.Dependencies.Utilities
{
    /// <summary>
    /// Basic implementation of randomly picking items by weight.
    /// </summary>
    public static class WeightedRandom
    {
        public interface IWeightedRandom
        {
            /// <summary>
            /// Weight of the entry between 0f and 1f.
            /// </summary>
            /// <returns></returns>
            float GetWeight();
            /// <summary>
            /// True if entry can be picked more than once when selecting multiple entries.
            /// </summary>
            /// <returns></returns>
            bool IsRepeatable();
        }
        /// <summary>
        /// Returns a number of entries randomly picked based on their weight.
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public static void GetEntries<T>(IReadOnlyCollection<T> source, IntRange countRange, List<T> results, bool allowRepeatables = true) where T : IWeightedRandom
        {
            {
                if (source == null)
                {
                    UnityEngine.Debug.LogError($"Source cannot be null.");
                    return;
                }
                if (countRange.Minimum > source.Count)
                    UnityEngine.Debug.LogError($"Source does not have enough entries to accomodate minimum count range. Results will not meet minimum count.");

                List<IWeightedRandom> copy = CollectionCaches<IWeightedRandom>.RetrieveList();
                //Copy is pulled from to not disrupt the original collection.
                foreach (IWeightedRandom item in source)
                    copy.Add(item);

                List<T> firstPass = CollectionCaches<T>.RetrieveList();
                int count = Ints.RandomInclusiveRange(countRange.Minimum, countRange.Maximum);

                do
                {
                    for (int i = 0; i < copy.Count; i++)
                    {
                        IWeightedRandom iwr = copy[i];
                        float chance = UnityEngine.Random.Range(0f, 1f);
                        if (chance <= iwr.GetWeight())
                        {
                            firstPass.Add((T)iwr);
                            if (!allowRepeatables || !iwr.IsRepeatable())
                            {
                                copy.RemoveAt(i);
                                i--;
                            }
                        }
                    }

                    //Pick a random entry if copy has results.
                    if (firstPass.Count > 0)
                    {
                        int index = Ints.RandomExclusiveRange(0, firstPass.Count);
                        results.Add(firstPass[index]);
                        firstPass.Clear();
                    }
                    //Nothing left in copy, cannot continue.
                    if (copy.Count == 0)
                        break;
                } while (results.Count < count);

                CollectionCaches<T>.Store(firstPass);
                CollectionCaches<IWeightedRandom>.Store(copy);
            }
        }
    }
}