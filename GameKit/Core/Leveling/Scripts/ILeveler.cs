
using log4net.Core;
using System;
using UnityEngine;

namespace GameKit.Core.Leveling
{

    public class LevelBase
    {
        /// <summary>
        /// Called when experience changes.
        /// </summary>
        public event ExperienceChangeDel OnExperienceChange;
        public delegate void ExperienceChangeDel(uint change, bool added);
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
                OnExperienceChange?.Invoke(value, true);
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
                OnExperienceChange?.Invoke(value, false);
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
        public virtual bool ModifyLevel(int value, bool resetExperience = false)
        {
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

            Level++;
            OnLevelChange?.Invoke(Level, Experience);
            //Do not allow level change to prevent endless loop.
            if (resetExperience)
                ModifyExperience(-Experience, false);

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

            Level--;
            OnLevelChange?.Invoke(Level, Experience);
            //Do not allow level change to prevent endless loop.
            if (resetExperience)
                ModifyExperience(-Experience, false);

            return true;
        }
    }


}