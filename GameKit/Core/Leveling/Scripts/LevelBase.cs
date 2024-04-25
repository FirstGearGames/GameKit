using UnityEngine;

namespace GameKit.Core.Leveling
{
    public enum ExperienceChangeType
    {
        Positive = 1,
        Negative = 2,
        Zero = 3,
    }

    /// <summary>
    /// Adds levels as experience is gained. Levels may also be removed with removed experience.
    /// This can be used as a base for character levels, or even tiers of skills.
    /// </summary>
    public class LevelBase
    {
        /// <summary>
        /// Called when experience changes.
        /// </summary>
        public event ExperienceChangeDel OnExperienceChange;
        public delegate void ExperienceChangeDel(uint value, ExperienceChangeType changeType);
        /// <summary>
        /// Called when max experience changes.
        /// </summary>
        public event ExperienceChangeDel OnMaxExperienceChange;
        public delegate void MaxExperienceChangeDel(uint newValue);
        /// <summary>
        /// Called when level changes.
        /// </summary>
        public event LevelChangeDel OnLevelChange;
        public delegate void LevelChangeDel(uint newLevel, uint experience);
        /// <summary>
        /// Called when the max level is changed.
        /// </summary>
        public event MaxLevelDel OnMaxLevelChange;
        public delegate void MaxLevelDel(uint newLevel);

        public uint Level { get; private set; }
        /// <summary>
        /// Current MaxLevel.
        /// </summary>
        public uint MaxLevel { get; private set; }
        /// <summary>
        /// Experience gained for the current level.
        /// </summary>
        public uint Experience { get; private set; }
        /// <summary>
        /// Max experience for the level.
        /// </summary>
        public uint MaxExperience { get; private set; }
        /// <summary>
        /// Returns percentage of how much the level has been completed.
        /// </summary>
        public float LevelCompletion => (Experience / MaxExperience);

        /// <summary>
        /// Sets a new MaxLevel.
        /// </summary>
        public virtual void SetMaxLevel(uint value)
        {
            if (MaxLevel == value)
                return;

            MaxLevel = value;
            OnMaxLevelChange?.Invoke(value);
        }

        /// <summary>
        /// Sets Level and MaxLevel.
        /// </summary>
        public void SetLevel(uint level, uint maxLevel, bool resetExperience)
        {
            SetMaxLevel(maxLevel);
            long difference = (long)Mathf.Clamp((level - Level), int.MinValue, int.MaxValue);
            ModifyLevel(difference, resetExperience);
        }

        /// <summary>
        /// Sets a new level clamped between 0 and MaxLevel.
        /// </summary>
        public void SetLevel(uint level, bool resetExperience)
        {
            long difference = (long)Mathf.Clamp((level - Level), int.MinValue, int.MaxValue);
            ModifyLevel(difference, resetExperience);
        }


        /// <summary>
        /// Sets the MaxExperience for the level.
        /// </summary>
        public virtual void SetMaxExperience(uint value)
        {
            MaxExperience = value;
            OnMaxLevelChange?.Invoke(value);
        }

        /// <summary>
        /// Modifies experience, increasing or decreasing it. Optionally allowing to gain or lose a level.
        /// </summary>
        /// <returns>True if a level change happened.</returns>
        public virtual bool ModifyExperience(long value, bool allowLevelChange = true)
        {
            //Clamp value to uint range.
            value = (long)Mathf.Clamp(value, -uint.MaxValue, uint.MaxValue);

            if (value > 0)
                return AddExperience((uint)value, allowLevelChange);
            else if (value < 0)
                return RemoveExperience((uint)(value * -1), allowLevelChange);

            //If here experience is 0. Simple invoke experience change but do nothing else.
            OnExperienceChange?.Invoke(0, ExperienceChangeType.Zero);
            return false;
        }
        /// <summary>
        /// Adds experience while optionally allowing level change.
        /// </summary>
        /// <returns>True if a level change happened.</returns>
        protected virtual bool AddExperience(uint value, bool allowLevelChange)
        {
            ulong next = (value + Experience);
            //Enough to level up.
            if (next >= MaxExperience)
            {
                uint overage = (uint)(next - MaxExperience);
                //Set experience to max to invoke event with value.
                Experience = MaxExperience;
                OnExperienceChange?.Invoke(value, ExperienceChangeType.Positive);
                //If can also level up...
                if (allowLevelChange && (Level < MaxLevel))
                {
                    //Update experience and level then invoke.
                    Experience = overage;
                    ModifyLevel(1);
                }
            }
            else
            {
                Experience = (uint)next;
            }

            //Fall through for no level change.
            return false;
        }
        /// <summary>
        /// Removes experience while optionally allowing level change.
        /// </summary>
        /// <returns>True if a level change happened.</returns>
        protected virtual bool RemoveExperience(uint value, bool allowLevelChange)
        {
            long next = (Experience - value);

            //Enough to level down.
            if (next < 0)
            {
                uint remainder = (uint)Mathf.Abs(next);
                //Set experience to 0 and invoke event with value.
                Experience = 0;
                OnExperienceChange?.Invoke(value, ExperienceChangeType.Negative);
                //If can also level down...
                if (allowLevelChange && Level > 0)
                {
                    Experience = remainder;
                    ModifyLevel(-1);
                    /* MaxExperience is probably too high at this point but
                     * the events and return provide the opportunity to
                     * adjust MaxExperience or other values after .*/
                    return true;
                }
            }
            else
            {
                Experience = (uint)next;
            }

            //Fall through for no level change.
            return false;
        }

        /// <summary>
        /// Modifies level, increasing or decreating it.
        /// </summary>
        /// <returns>True if level change was successful.</returns>
        public virtual bool ModifyLevel(long value, bool resetExperience = false)
        {
            value = (long)Mathf.Clamp(Level, -uint.MaxValue, uint.MaxValue);

            if (value > 0)
                return AddLevel((uint)value, resetExperience);
            else if (value < 0)
                return RemoveLevel((uint)value, resetExperience);

            return false;
        }
        /// <summary>
        /// Adds levels.
        /// </summary>
        protected virtual bool AddLevel(uint value, bool resetExperience)
        {
            long next = (Level + value);
            //This would caus ean overflow or beyond max level.
            if ((next >= MaxLevel) || (next > uint.MaxValue))
                return false;

            if (resetExperience)
                ModifyExperience(-Experience, false);
            Level++;
            OnLevelChange?.Invoke(Level, Experience);

            return true;
        }
        /// <summary>
        /// Removes levels
        /// </summary>
        protected virtual bool RemoveLevel(uint value, bool resetExperience)
        {
            long next = (Level - value);
            //Underflow.
            if (next < 0)
                return false;

            if (resetExperience)
                ModifyExperience(-Experience, false);
            Level--;
            OnLevelChange?.Invoke(Level, Experience);

            return true;
        }
    }


}