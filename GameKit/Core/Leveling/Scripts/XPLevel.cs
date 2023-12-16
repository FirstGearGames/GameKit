using GameKit.Dependencies.Utilities.Types;
using UnityEngine;

namespace GameKit.Core.Leveling
{
    /// <summary>
    /// A simple level system where the XP requirement per level is always the same but level requirement increases by a set amount.
    /// </summary>
    public class XPLevel : LevelBase
    {

        private const uint STARTING_LEVEL = 1;
        private const uint MAXIMUM_LEVEL = 50;
        private const float INCREASE_PER_LEVEL = 0.05f;
        private const uint STARTING_LEVEL_EXPERIENCE = 1000;

        public XPLevel()
        {
            base.SetLevel(STARTING_LEVEL, MAXIMUM_LEVEL, true);
        }

        /// <summary>
        /// Changes the max experience requirement when level is changed.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="resetExperience"></param>
        /// <returns></returns>
        public override bool ModifyLevel(long value, bool resetExperience)
        {
            bool result = base.ModifyLevel(value, resetExperience);
            //If level was changed recalc XP needed.
            if (result)
            {
                float baseIncrease = (1f + INCREASE_PER_LEVEL);
                long power = (base.Level - STARTING_LEVEL);
                //Make sure power is a minimum of 1.
                power = (long)Mathf.Max(power, 1);
                float multiplier = Mathf.Pow(baseIncrease, power);
                long xpRequirement = (long)Mathf.Clamp(multiplier * STARTING_LEVEL_EXPERIENCE, STARTING_LEVEL_EXPERIENCE, uint.MaxValue);
                SetMaxExperience((uint)xpRequirement);
            }

            return result;
        }

        /// <summary>
        /// Modifies experience using a multiplier.
        /// </summary>
        public void ModifyExperience(long value, float multiplier, bool allowLevelChange = true)
        {
            value = (long)Mathf.Clamp(value * multiplier, -long.MaxValue, long.MaxValue);
            base.ModifyExperience(value, allowLevelChange);
        }

        /// <summary>
        /// Modifies experience by generating a multiplier based on level range. Multiplier ranges between 0.25f and 1.75f.
        /// </summary>
        public void ModifyExperience(long value, uint actorLevel, IntRange levelRange, bool allowLevelChange = true)
        {
            //Too low to give XP.
            if (actorLevel < levelRange.Minimum)
            {
                base.ModifyExperience(0, false);
            }
            //Get a multiplier by using level ranges and actor level.
            else
            {
                float alpha = Mathf.InverseLerp(actorLevel, levelRange.Minimum, levelRange.Maximum);
                float percent = Mathf.Lerp(alpha, 0.25f, 1.75f);
                this.ModifyExperience(value, percent, allowLevelChange);
            }
        }

    }


}