using System.Collections.Generic;
using UnityEngine;

namespace FirstGearGames.Utilities.Objects
{



    public static class Debugs
    {
        /// <summary>
        /// Dictionary used to store LimitPrint calls.
        /// </summary>
        private static Dictionary<string, float> _limitPrints = new Dictionary<string, float>();

        /// <summary>
        /// Returns if enough time has passed to print another debug on the specified key. Used to debug print without spamming console.
        /// </summary>
        /// <param name="key">Required delay in seconds.</param>
        /// <param name="requiredDelay">Amount of time required to pass before log can be printed again.</param>
        /// <returns></returns>
        public static bool LimitLog(string key, float requiredDelay = 1f)
        {
            //It's too dangerous to allow this code in builds due to memory leak potential.
#if UNITY_EDITOR
            float lastPrint = 0f;
            //If key exist.
            if (_limitPrints.TryGetValue(key, out lastPrint))
            {
                //Not enough time has passed to perform another log.
                if (Time.unscaledTime < (lastPrint + requiredDelay))
                    return false;

            }
            //Key not found.
            else
            {
                _limitPrints.Add(key, UnityEngine.Time.unscaledTime);
            }

            //Reset time.
            _limitPrints[key] = Time.unscaledTime;
#endif
            return true;
        }

    }


}