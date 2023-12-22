using GameKit.Dependencies.Utilities.Types;
using System.Collections.Generic;
using UnityEngine;

namespace GameKit.Dependencies.Utilities
{
    public interface IWeighted
    {
        float GetWeight();
        bool IsRepeatable();
    }

    public static class WeightedRandom
    {

        public static void GetEntries<T>(List<T> source, IntRange countRange, List<T> results, bool allowRepeatable = true) where T : IWeighted
        {
            if (source == null || source.Count == 0)
            {
                Debug.Log($"Source list of type {typeof(T).Name} cannot be null or empty.");
                return;
            }

            int count = Ints.RandomInclusiveRange(countRange.Minimum, countRange.Maximum);
            //If to not return any then exit early.
            if (count == 0)
                return;
             
            //Get the total weight.
            float totalWeight = 0f;
            for (int i = 0; i < source.Count; i++)
                totalWeight += source[i].GetWeight();

            //Make a copy of source to not modify source.
            List<T> sourceCopy = CollectionCaches<T>.RetrieveList();
            foreach (T item in source)
                sourceCopy.Add(item);

            while (results.Count < count)
            {
                int startCount = results.Count;
                /* Reset copy to totalWeight.
                 * totalWeight will be modified if
                 * a non-repeatable item is pulled. */
                float tWeightCopy = totalWeight;
                float rnd = UnityEngine.Random.Range(0f, totalWeight);

                for (int i = 0; i < sourceCopy.Count; i++)
                {
                    T item = sourceCopy[i];
                    float weight = item.GetWeight();
                    if (rnd <= weight)
                    {
                        results.Add(item);
                        /* If cannot stay in collection then remove it
                         * from copy and remove its weight
                         * from total. */
                        if (!allowRepeatable || !item.IsRepeatable())
                        {
                            sourceCopy.RemoveAt(i);
                            totalWeight -= weight;
                        }
                        break;
                    }
                    else
                    {
                        tWeightCopy -= weight;
                    }
                }

                /* If nothing was added to results then
                 * something went wrong. */
                if (results.Count == startCount)
                    return;
            }

        }
    }

}
